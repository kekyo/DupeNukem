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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem;

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
    private readonly Dictionary<string, WeakReference> ctss = new();
    private readonly Queue<WeakReference> timeoutQueue = new();
    private readonly TimeSpan timeoutDuration;
    private readonly Timer timeoutTimer;
    private readonly FinalizationRegistry peerClosureRegistry;
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
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };
        serializer.Converters.Add(new StringEnumConverter(defaultNamingStrategy));
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
        this.timeoutTimer = new(this.ReachTimeout, null, 0, 0);
        this.peerClosureRegistry = new(name =>
        {
            var request = new Message(
                "discard",
                MessageTypes.Metadata,
                JToken.FromObject(name, this.Serializer));

            var tw = new StringWriter();
            this.Serializer.Serialize(tw, request);

            this.SendMessageToPeer(tw.ToString());

            Trace.WriteLine($"DupeNukem: Sent discarded closure delegate: {name}");
        });
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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public Delegate? RegisterPeerClosure(string name, Type delegateType)
    {
        if (!typeof(Delegate).IsAssignableFrom(delegateType))
        {
            Trace.WriteLine($"DupeNukem: {delegateType.FullName} is not a delegate.");
            return null;
        }

        if (ClosureTrampolineFactory.Create(
            delegateType,
            (args, returnType, ct) => this.InvokePeerMethodAsync(ct, returnType, name, args)) is not { } dlg)
        {
            Trace.WriteLine($"DupeNukem: Invalid delegate type: {delegateType.FullName}");
            return null;
        }

        this.peerClosureRegistry.Register(dlg, name);

        return dlg;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public CancellationToken RegisterCancellationToken(string name)
    {
        lock (this.ctss)
        {
            if (this.ctss.TryGetValue(name, out var wr))
            {
                if (wr.Target is not CancellationTokenSource cts)
                {
                    cts = new();
                    wr.Target = cts;
                }
                return cts.Token;
            }
            else
            {
                var cts = new CancellationTokenSource();
                this.ctss.Add(name, new(cts));
                return cts.Token;
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    protected void CancelAllSuspending()
    {
        this.peerClosureRegistry.ForceClear();

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

    private void ValidateArgumentType(object? arg)
    {
        if (arg is Delegate closure)
        {
            var mi = closure.GetMethodInfo()!;
            if (!typeof(Task).IsAssignableFrom(mi.ReturnType))
            {
                throw new JsonSerializationException(
                    $"Could not serialize the delegate. Return type is not Task type.");
            }
        }
    }

    private JToken? SerializeArgument(object? arg)
    {
        switch (arg)
        {
            case null:
                return null;
            case Delegate closure:
                var closureName = "closure_$" + Interlocked.Increment(ref this.id);
                this.RegisterMethod(
                    closureName,
                    new DynamicFunctionDescriptor(closure, this),
                    true);
                return JToken.FromObject(
                    new Message(
                        "closure",
                        MessageTypes.Metadata,
                        JToken.FromObject(closureName, this.Serializer)),
                    this.Serializer);
            case CancellationToken ct:
                var ctName = "cancellationToken_$" + Interlocked.Increment(ref this.id);
                ct.Register(() =>
                {
                    var request = new Message(
                        "cancel",
                        MessageTypes.Metadata,
                        JToken.FromObject(ctName, this.Serializer));

                    var tw = new StringWriter();
                    this.Serializer.Serialize(tw, request);

                    this.SendMessageToPeer(tw.ToString());
                });
                return JToken.FromObject(
                    new Message(
                        "cancellationToken",
                        MessageTypes.Metadata,
                        JToken.FromObject(ctName, this.Serializer)),
                    this.Serializer);
            default:
                return JToken.FromObject(
                    arg,
                    this.Serializer);
        }
    }

    private void SendInvokeMessageToPeer(
        SuspendingDescriptor descriptor, CancellationToken ct,
        string functionName, object?[] args)
    {
        foreach (var arg in args)
        {
            this.ValidateArgumentType(arg);
        }

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
            args.Select(this.SerializeArgument).
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

    public Task<object?> InvokePeerMethodAsync(
        CancellationToken ct, Type returnType, string methodName, params object?[] args)
    {
        ct.ThrowIfCancellationRequested();

        var descriptor = new DynamicSuspendingDescriptor(returnType);
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
                            switch (error.Name)
                            {
                                case "OperationCancelledError":
                                    throw new OperationCanceledException(error.Message);
                                default:
                                    throw new PeerInvocationException(
                                        error.Name, error.Message, error.Detail);
                            }
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
                                string.Empty,
                                new());

                            this.SendExceptionMessageToPeer(message, responseBody);
                        }
                    }
                    catch (Exception ex)
                    {
                        var props = Utilities.ExtractExceptionProperties(
                            ex, this.MemberAccessNamingStrategy);

                        var responseBody = new ExceptionBody(
                            ex.GetType().FullName!, ex.Message,
                            this.SendExceptionWithStackTrace ?
                                (ex.StackTrace ?? string.Empty) :
                                string.Empty,
                            props);

                        this.SendExceptionMessageToPeer(message, responseBody);
                    }
                    break;

                case MessageTypes.Metadata:
                    switch (message.Id)
                    {
                        case "discard" when
                            // Decline invalid name to avoid security attacks.
                            message.Body!.ToObject<string>(this.Serializer) is { } name &&
                            name.StartsWith("closure_$"):
                                this.methods.SafeRemove(name);
                                Trace.WriteLine($"DupeNukem: Deleted peer closure target delegate: {name}");
                                break;
                        case "cancel" when
                            // Decline invalid name to avoid security attacks.
                            message.Body!.ToObject<string>(this.Serializer) is { } name &&
                            name.StartsWith("cancellationToken_$"):
                            lock (this.ctss)
                            {
                                if (this.ctss.TryGetValue(name, out var wr))
                                {
                                    if (wr.Target is CancellationTokenSource cts)
                                    {
                                        cts.Cancel();
                                    }
                                    else
                                    {
                                        lock (this.ctss)
                                        {
                                            this.ctss.Remove(name);
                                        }
                                        Trace.WriteLine($"DupeNukem: Deleted peer cancellation token: {name}");
                                    }
                                }
                                else
                                {
                                    Trace.WriteLine($"DupeNukem: Could not find peer cancellation token: {name}");
                                }
                            }
                            break;
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
