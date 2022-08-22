////////////////////////////////////////////////////////////////////////////
//
// DupeNukem - WebView attachable full-duplex asynchronous interoperable
// independent messaging library between .NET and JavaScript.
//
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using DupeNukem.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem
{
    public sealed class SendRequestEventArgs : EventArgs
    {
        public readonly string JsonString;

        public SendRequestEventArgs(string jsonString) =>
            this.JsonString = jsonString;

        public void Deconstruct(out string sendRequest) =>
            sendRequest = this.JsonString;
    }

    public class Messenger : IMessenger, IDisposable
    {
        private static readonly NamingStrategy defaultNamingStrategy =
            new CamelCaseNamingStrategy();

        private readonly Dictionary<string, MethodDescriptor> methods = new();
        private readonly Dictionary<string, SuspendingDescriptor> suspendings = new();
        private readonly Queue<WeakReference> timeoutQueue = new();
        private readonly TimeSpan timeoutDuration;
        private readonly Timer timeoutTimer;
        private volatile int id;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public NamingStrategy MemberAccessNamingStrategy { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public JsonSerializer Serializer { get; }

        protected SynchronizationContext? SynchContext { get; } =
            SynchronizationContext.Current;

        protected KeyValuePair<string, MethodDescriptor>[] GetRegisteredMethodPairs()
        {
            lock (this.methods)
            {
                return this.methods.ToArray();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        public static JsonSerializer GetDefaultJsonSerializer()
        {
            var serializer = new JsonSerializer
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateParseHandling = DateParseHandling.DateTimeOffset,
                DateTimeZoneHandling = DateTimeZoneHandling.Local,
                NullValueHandling = NullValueHandling.Include,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                ContractResolver = new DefaultContractResolver { NamingStrategy = defaultNamingStrategy, },
            };
            serializer.Converters.Add(new StringEnumConverter(defaultNamingStrategy));
            serializer.Converters.Add(new CancellationTokenConverter());
            return serializer;
        }

        public Messenger(TimeSpan? timeoutDuration = default) :
            this(GetDefaultJsonSerializer(), defaultNamingStrategy, timeoutDuration)
        {
        }

        public Messenger(
            JsonSerializer serializer,
            NamingStrategy memberAccessNamingStrategy,
            TimeSpan? timeoutDuration)
        {
            this.Serializer = serializer;
            this.MemberAccessNamingStrategy = memberAccessNamingStrategy;
            this.timeoutDuration = timeoutDuration ??
#if DEBUG
                new TimeSpan(0, 0, 0, 0, -1);
#else
                TimeSpan.FromSeconds(30);
#endif
            this.timeoutTimer = new Timer(this.ReachTimeout, null, 0, 0);
        }

        public virtual void Dispose()
        {
            this.timeoutTimer.Dispose();
            this.CancelAllSuspending();

            this.SendRequest = null;
            this.ErrorDetected = null;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool SendExceptionWithStackTrace { get; set; }

        public event EventHandler<SendRequestEventArgs>? SendRequest;
        public event EventHandler? ErrorDetected;

        ///////////////////////////////////////////////////////////////////////////////

        protected virtual void OnRegisterMethod(
            string name, MethodDescriptor method, bool hasSpecifiedName)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public string RegisterMethod(
            string name, MethodDescriptor method, bool hasSpecifiedName)
        {
            var n = this.MemberAccessNamingStrategy.GetConvertedName(name, hasSpecifiedName);
            this.OnRegisterMethod(n, method, hasSpecifiedName);
            this.methods.SafeAdd(n, method);
            return n;
        }

        protected virtual void OnUnregisterMethod(
            string name, MethodDescriptor method, bool hasSpecifiedName)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void UnregisterMethod(string name, bool hasSpecifiedName)
        {
            var n = this.MemberAccessNamingStrategy.GetConvertedName(name, hasSpecifiedName);
            if (this.methods.TryGetValue(n, out var method))
            {
                this.OnUnregisterMethod(n, method, hasSpecifiedName);
                this.methods.SafeRemove(n);
            }
        }

        public string[] RegisteredMethods
        {
            get
            {
                lock (this.methods)
                {
                    return this.methods.Keys.ToArray();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected void CancelAllSuspending()
        {
            lock (this.timeoutQueue)
            {
                while (this.timeoutQueue.Count >= 1)
                {
                    var wr = this.timeoutQueue.Dequeue();
                    if (wr.Target is SuspendingDescriptor descriptor)
                    {
                        descriptor.Cancel();
                    }
                }
            }
        }

        private void ReachTimeout(object? state)
        {
            var now = DateTime.Now;
            lock (this.timeoutQueue)
            {
                while (this.timeoutQueue.Count >= 1)
                {
                    var wr = this.timeoutQueue.Peek();
                    if (wr.Target is SuspendingDescriptor descriptor)
                    {
                        var past = now - descriptor.Created;
                        var remains = this.timeoutDuration - past;
                        if (remains > TimeSpan.Zero)
                        {
                            this.timeoutTimer.Change(
                                remains, Utilities.InfiniteTimeSpan);
                            break;
                        }

                        descriptor.Cancel();
                    }

                    this.timeoutQueue.Dequeue();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        private async void SendMessageToPeer(string jsonString)
        {
            if (this.SendRequest is { } sendRequest)
            {
                await this.SynchContext.Bind();
                sendRequest(this, new SendRequestEventArgs(jsonString));
            }
            else
            {
                Trace.WriteLine("DupeNukem: SendRequest doesn't hook.");
            }
        }

        private void SendInvokeMessageToPeer(
            SuspendingDescriptor descriptor, CancellationToken ct,
            string functionName, object?[] args)
        {
            lock (this.timeoutQueue)
            {
                this.timeoutQueue.Enqueue(new WeakReference(descriptor));
                if (this.timeoutQueue.Count == 1)
                {
                    this.timeoutTimer.Change(
                        this.timeoutDuration, Utilities.InfiniteTimeSpan);
                }
            }

            var id = "host_" + Interlocked.Increment(ref this.id);

            ct.Register(() =>
            {
                this.suspendings.SafeRemove(id);
                descriptor.Cancel();
            });

            this.suspendings.SafeAdd(id, descriptor);

            var body = new InvokeBody(
                functionName,
                args.Select(arg => arg != null ? JToken.FromObject(arg, this.Serializer) : null).
                ToArray());
            var request = new Message(
                id,
                MessageTypes.Invoke,
                JToken.FromObject(body, this.Serializer));

            var tw = new StringWriter();
            this.Serializer.Serialize(tw, request);

            this.SendMessageToPeer(tw.ToString());
        }

        public Task InvokePeerMethodAsync(
            CancellationToken ct, string methodName, params object?[] args)
        {
            ct.ThrowIfCancellationRequested();

            var descriptor = new VoidSuspendingDescriptor();
            this.SendInvokeMessageToPeer(descriptor, ct, methodName, args);

            return descriptor.Task;
        }

        public Task<TR> InvokePeerMethodAsync<TR>(
            CancellationToken ct, string methodName, params object?[] args)
        {
            ct.ThrowIfCancellationRequested();

            var descriptor = new SuspendingDescriptor<TR>();
            this.SendInvokeMessageToPeer(descriptor, ct, methodName, args);

            return descriptor.Task;
        }

        ///////////////////////////////////////////////////////////////////////////////

        protected void SendControlMessageToPeer(
            string controlId, object? message)
        {
            var request = new Message(
                controlId,
                MessageTypes.Control,
                message != null ?
                    JToken.FromObject(message, this.Serializer) :
                    null);
            var tw = new StringWriter();
            this.Serializer.Serialize(tw, request);
            this.SendMessageToPeer(tw.ToString());
        }

        protected virtual void OnReceivedControlMessage(
            string controlId, JToken? body)
        {
        }

        private void SendExceptionMessageToPeer(
            Message message, ExceptionBody responseBody)
        {
            var response = new Message(
                message.Id,
                MessageTypes.Failed,
                JToken.FromObject(responseBody, this.Serializer));

            var tw = new StringWriter();
            this.Serializer.Serialize(tw, response);

            this.SendMessageToPeer(tw.ToString());
        }

        public async void ReceivedRequest(string jsonString)
        {
            try
            {
                var tr = new StringReader(jsonString);
                var message = (Message)this.Serializer.Deserialize(tr, typeof(Message))!;

                switch (message.Type)
                {
                    case MessageTypes.Control:
                        this.OnReceivedControlMessage(message.Id, message.Body);
                        break;

                    case MessageTypes.Succeeded:
                        if (this.suspendings.SafeTryGetValue(message.Id, out var successorDescriptor))
                        {
                            this.suspendings.SafeRemove(message.Id);
                            successorDescriptor.Resolve(message.Body);
                        }
                        else
                        {
                            this.ErrorDetected?.Invoke(this, new SpriousMessageEventArgs(jsonString));
                        }
                        break;

                    case MessageTypes.Failed:
                        if (this.suspendings.SafeTryGetValue(message.Id, out var failureDescriptor))
                        {
                            this.suspendings.SafeRemove(message.Id);
                            var error = message.Body!.ToObject<ExceptionBody>(this.Serializer);
                            try
                            {
                                throw new PeerInvocationException(error.Name, error.Message, error.Detail);
                            }
                            catch (Exception ex)
                            {
                                failureDescriptor.Reject(ex);
                            }
                        }
                        else
                        {
                            this.ErrorDetected?.Invoke(this, new SpriousMessageEventArgs(jsonString));
                        }
                        break;

                    case MessageTypes.Invoke:
                        try
                        {
                            var body = message.Body!.ToObject<InvokeBody>(this.Serializer);

                            if (this.methods.SafeTryGetValue(body.Name, out var method))
                            {
                                await this.SynchContext.Bind();

                                var result = await method.InvokeAsync(body.Args).
                                    ConfigureAwait(false);

                                var response = new Message(
                                    message.Id, MessageTypes.Succeeded,
                                    (result != null) ?
                                        JToken.FromObject(result, this.Serializer) :
                                        null);

                                var tw = new StringWriter();
                                this.Serializer.Serialize(tw, response);

                                this.SendMessageToPeer(tw.ToString());
                            }
                            else
                            {
                                var responseBody = new ExceptionBody(
                                    "InvalidMethodName",
                                    $"Method '{body.Name}' is not found.",
                                    string.Empty);

                                this.SendExceptionMessageToPeer(message, responseBody);
                            }
                        }
                        catch (Exception ex)
                        {
                            var responseBody = new ExceptionBody(
                                ex.GetType().FullName!, ex.Message,
                                this.SendExceptionWithStackTrace ? (ex.StackTrace ?? string.Empty) : string.Empty);

                            this.SendExceptionMessageToPeer(message, responseBody);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                this.ErrorDetected?.Invoke(this, new InvalidMessageEventArgs(ex));
            }
        }
    }
}
