﻿////////////////////////////////////////////////////////////////////////////
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
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem
{
    public sealed class InvalidMessageEventArgs : EventArgs
    {
        public readonly Exception Exception;

        public InvalidMessageEventArgs(Exception ex) =>
            this.Exception = ex;

        public override string ToString() =>
            this.Exception.Message;
    }

    public sealed class SpriousMessageEventArgs : EventArgs
    {
        public readonly string Json;

        public SpriousMessageEventArgs(string json) =>
            this.Json = json;

        public override string ToString() =>
            $"Sprious message: {this.Json}";
    }

    [Serializable]
    public sealed class JavaScriptException : Exception
    {
        public readonly string Name;
        public readonly string Detail;

        public JavaScriptException(string name, string message, string detail) :
            base(message)
        {
            this.Name = name;
            this.Detail = detail;
        }
    }

    public sealed class SendRequestEventArgs : EventArgs
    {
        public readonly string JsonString;

        public SendRequestEventArgs(string jsonString) =>
            this.JsonString = jsonString;
    }

    public sealed class Messenger : IDisposable
    {
        private readonly Dictionary<string, MethodDescriptor> methods = new();
        private readonly Dictionary<string, SuspendingDescriptor> suspendings = new();
        private readonly Queue<WeakReference<SuspendingDescriptor>> timeoutQueue = new();
        private readonly TimeSpan timeoutDuration;
        private readonly Timer timeoutTimer;
        private readonly JsonSerializer serializer;
        private volatile int id;

        ///////////////////////////////////////////////////////////////////////////////

        public Messenger(TimeSpan? timeoutDuration = default)
        {
            this.serializer = new JsonSerializer
            {
#if DEBUG
                Formatting = Formatting.Indented,
#endif
            };
            this.timeoutDuration = timeoutDuration ?? TimeSpan.FromSeconds(30);
            this.timeoutTimer = new Timer(this.ReachTimeout);

            this.RegisterAction("dupeNukem_Messenger_ready__", () =>
            {
                // Exhausted page content, maybe all suspending tasks are zombies.
                this.CancelAllSuspending();

                this.Ready?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            });
        }

        public void Dispose()
        {
            this.timeoutTimer.Dispose();
            this.CancelAllSuspending();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public bool SendExceptionWithStackTrace { get; set; }

        public event EventHandler<SendRequestEventArgs>? SendRequest;

        public event EventHandler? Ready;
        public event EventHandler? ErrorDetected;

        ///////////////////////////////////////////////////////////////////////////////

        public StringBuilder GetInjectionScript()
        {
            using var s = this.GetType().Assembly.
                GetManifestResourceStream("DupeNukem.Script.js");
            var tr = new StreamReader(s!, Encoding.UTF8);
            return new StringBuilder(tr.ReadToEnd());
        }

        internal void RegisterMethod(string name, MethodDescriptor method) =>
            this.methods.SafeAdd(name, method);

        public void UnregisterMethod(string name) =>
            this.methods.SafeRemove(name);

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

        private void CancelAllSuspending()
        {
            lock (this.timeoutQueue)
            {
                while (this.timeoutQueue.Count >= 1)
                {
                    var wr = this.timeoutQueue.Dequeue();
                    if (wr.TryGetTarget(out var descriptor))
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
                    if (wr.TryGetTarget(out var descriptor))
                    {
                        var past = now - descriptor.Created;
                        var remains = this.timeoutDuration - past;
                        if (remains > TimeSpan.Zero)
                        {
                            this.timeoutTimer.Change(
                                remains, Timeout.InfiniteTimeSpan);
                            break;
                        }

                        descriptor.Cancel();
                    }

                    this.timeoutQueue.Dequeue();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        private void SendMessageToClient(string jsonString)
        {
            if (this.SendRequest is { } sendRequest)
            {
                sendRequest(this, new SendRequestEventArgs(jsonString));
            }
            else
            {
                throw new InvalidOperationException("DupeNukem: SendRequest doesn't hook.");
            }
        }

        private void SendMessageToClient(
            SuspendingDescriptor descriptor, CancellationToken ct,
            string functionName, object[] args)
        {
            lock (this.timeoutQueue)
            {
                this.timeoutQueue.Enqueue(
                    new WeakReference<SuspendingDescriptor>(descriptor));
                if (this.timeoutQueue.Count == 1)
                {
                    this.timeoutTimer.Change(
                        this.timeoutDuration, Timeout.InfiniteTimeSpan);
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
                functionName, args);
            var request = new Message(
                id, MessageTypes.Invoke, JToken.FromObject(body, this.serializer));

            var tw = new StringWriter();
            this.serializer.Serialize(tw, request);

            this.SendMessageToClient(tw.ToString());
        }

        public Task InvokeClientFunctionAsync(
            CancellationToken ct, string functionName, params object[] args)
        {
            ct.ThrowIfCancellationRequested();

            var descriptor = new VoidSuspendingDescriptor();
            this.SendMessageToClient(descriptor, ct, functionName, args);

            return descriptor.Task;
        }

        public Task<TR> InvokeClientFunctionAsync<TR>(
            CancellationToken ct, string functionName, params object[] args)
        {
            ct.ThrowIfCancellationRequested();

            var descriptor = new SuspendingDescriptor<TR>();
            this.SendMessageToClient(descriptor, ct, functionName, args);

            return descriptor.Task;
        }

        ///////////////////////////////////////////////////////////////////////////////

        public async void ReceivedRequest(string jsonString)
        {
            try
            {
                var tr = new StringReader(jsonString);
                var message = (Message)this.serializer.Deserialize(tr, typeof(Message))!;

                switch (message.Type)
                {
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
                            var error = message.Body!.ToObject<ExceptionBody>();
                            try
                            {
                                throw new JavaScriptException(error.Name, error.Message, error.Detail);
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
                            var body = message.Body!.ToObject<InvokeBody>();

                            if (this.methods.SafeTryGetValue(body.Name, out var method))
                            {
                                var result = await method.InvokeAsync(body.Args).
                                    ConfigureAwait(false);

                                var response = new Message(
                                    message.Id, MessageTypes.Succeeded,
                                    (result != null) ? JToken.FromObject(result, this.serializer) : null);

                                var tw = new StringWriter();
                                this.serializer.Serialize(tw, response);

                                this.SendMessageToClient(tw.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            var responseBody = new ExceptionBody(
                                ex.GetType().FullName!, ex.Message,
                                this.SendExceptionWithStackTrace ? (ex.StackTrace ?? string.Empty) : string.Empty);
                            var response = new Message(
                                message.Id, MessageTypes.Failed, JToken.FromObject(responseBody, this.serializer));

                            var tw = new StringWriter();
                            this.serializer.Serialize(tw, response);

                            this.SendMessageToClient(tw.ToString());
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