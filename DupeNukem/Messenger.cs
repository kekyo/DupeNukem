////////////////////////////////////////////////////////////////////////////
//
// DupeNukem - WebView attachable full-duplex asynchronous interoperable
// messaging library between .NET and JavaScript.
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

    public sealed class Messenger : IDisposable
    {
        private readonly Dictionary<string, MethodDescriptor> methods = new();
        private readonly Dictionary<string, SuspendingDescriptor> suspendings = new();
        private readonly Queue<WeakReference<SuspendingDescriptor>> timeoutQueue = new();
        private readonly Action<string> sendToClientMessage;
        private readonly TimeSpan timeoutDuration;
        private readonly Timer timeoutTimer;
        private volatile int id;

        ///////////////////////////////////////////////////////////////////////////////

        public Messenger(
            Action<string> sendToClientMessage, TimeSpan? timeoutDuration = default)
        {
            this.sendToClientMessage = sendToClientMessage;
            this.timeoutDuration = timeoutDuration ?? TimeSpan.FromSeconds(30);
            this.timeoutTimer = new Timer(this.ReachTimeout);
        }

        public void Dispose()
        {
            this.timeoutTimer.Dispose();
        }

        public event EventHandler? ErrorDetected;

        ///////////////////////////////////////////////////////////////////////////////

        public string GetInjectionScript()
        {
            using var s = this.GetType().Assembly.
                GetManifestResourceStream("DupeNukem.Script.js");
            var tr = new StreamReader(s!, Encoding.UTF8);
            return tr.ReadToEnd();
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

        private void SendToClientMessage(
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
                id, MessageTypes.Invoke, JToken.FromObject(body));
            var requestJsonString =
                JsonConvert.SerializeObject(request);

            this.sendToClientMessage(requestJsonString);
        }

        public Task InvokeClientFunctionAsync(
            CancellationToken ct, string functionName, params object[] args)
        {
            ct.ThrowIfCancellationRequested();

            var descriptor = new VoidSuspendingDescriptor();
            this.SendToClientMessage(descriptor, ct, functionName, args);

            return descriptor.Task;
        }

        public Task<TR> InvokeClientFunctionAsync<TR>(
            CancellationToken ct, string functionName, params object[] args)
        {
            ct.ThrowIfCancellationRequested();

            var descriptor = new SuspendingDescriptor<TR>();
            this.SendToClientMessage(descriptor, ct, functionName, args);

            return descriptor.Task;
        }

        ///////////////////////////////////////////////////////////////////////////////

        public async void ArrivedClientMesssage(string jsonString)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<Message>(jsonString);

                switch (message.MessageType)
                {
                    case MessageTypes.Success:
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
                            var error = message.Body.ToObject<ExceptionBody>();
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
                            var body = message.Body.ToObject<InvokeBody>();

                            if (this.methods.SafeTryGetValue(body.Name, out var method))
                            {
                                var result = await method.InvokeAsync(body.Args).
                                    ConfigureAwait(false);

                                var response = new Message(
                                    message.Id, MessageTypes.Success, JToken.FromObject(result!));
                                var responseJsonString =
                                    JsonConvert.SerializeObject(response);

                                this.sendToClientMessage(responseJsonString);
                            }
                        }
                        catch (Exception ex)
                        {
                            var responseBody = new ExceptionBody(
                                ex.GetType().FullName!, ex.Message, ex.ToString());  // TODO: filter
                            var response = new Message(
                                message.Id, MessageTypes.Failed, JToken.FromObject(responseBody));
                            var responseJsonString =
                                JsonConvert.SerializeObject(response);

                            this.sendToClientMessage(responseJsonString);
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
