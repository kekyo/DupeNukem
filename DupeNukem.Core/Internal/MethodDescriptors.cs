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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace DupeNukem.Internal;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public sealed class MethodMetadata
{
    public readonly bool IsProxyInjecting;
    public readonly ObsoleteAttribute? Obsolete;

    internal MethodMetadata(bool isProxyInjecting, ObsoleteAttribute? obsolete)
    {
        this.IsProxyInjecting = isProxyInjecting;
        this.Obsolete = obsolete;
    }
}

[EditorBrowsable(EditorBrowsableState.Advanced)]
public abstract class MethodDescriptor
{
    private readonly IMessenger messenger;

    private protected MethodDescriptor(IMessenger messenger, MethodMetadata metadata)
    {
        this.messenger = messenger;
        this.Metadata = metadata;
    }

    public MethodMetadata Metadata { get; }

    public abstract Task<object?> InvokeAsync(JToken?[] args);

    protected T ToObject<T>(JToken? arg)
    {
        switch (arg)
        {
            // Null.
            case null:
                return default!;
            case JObject jo when
                    jo.ToObject<Message>(this.messenger.Serializer) is { } m &&
                    m.Type == MessageTypes.Metadata &&
                    m.Body?.ToObject<string>(this.messenger.Serializer) is { } name:
                // Function closure comes from JavaScript.
                if (m.Id == "closure" &&
                    typeof(Delegate).IsAssignableFrom(typeof(T)) &&
                    name.StartsWith("__peerClosures__.closure_$"))
                {
                    return (T)(object)this.messenger.RegisterPeerClosure(name, typeof(T))!;
                }
                // CancellationToken
                else if (m.Id == "cancellationToken" &&
                    typeof(CancellationToken).Equals(typeof(T)) &&
                    name.StartsWith("cancellationToken_$"))
                {
                    return (T)(object)this.messenger.RegisterCancellationToken(name)!;
                }
                else
                {
                    return arg.ToObject<T>(this.messenger.Serializer)!;
                }
            // Other value types.
            default:
                return arg.ToObject<T>(this.messenger.Serializer)!;
        };
    }

    protected object? ToObject(JToken? arg, Type type)
    {
        switch (arg)
        {
            // Null.
            case null:
                return default!;
            case JObject jo when
                    jo.ToObject<Message>(this.messenger.Serializer) is { } m &&
                    m.Type == MessageTypes.Metadata &&
                    m.Body?.ToObject<string>(this.messenger.Serializer) is { } name:
                // Function closure comes from JavaScript.
                if (m.Id == "closure" &&
                    typeof(Delegate).IsAssignableFrom(type) &&
                    name.StartsWith("__peerClosures__.closure_$"))
                {
                    return this.messenger.RegisterPeerClosure(name, type);
                }
                // CancellationToken
                else if (m.Id == "cancellationToken" &&
                    typeof(CancellationToken).Equals(type) &&
                    name.StartsWith("cancellationToken_$"))
                {
                    return this.messenger.RegisterCancellationToken(name);
                }
                else
                {
                    return arg.ToObject(type, this.messenger.Serializer)!;
                }
            // Other value types.
            default:
                return arg.ToObject(type, this.messenger.Serializer)!;
        };
    }
}

///////////////////////////////////////////////////////////////////////////////

internal sealed class ActionDescriptor : MethodDescriptor
{
    private readonly Func<Task> action;

    public ActionDescriptor(
        Func<Task> action, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.action = action;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        Debug.Assert(args.Length == 0);
        await this.action().
            ConfigureAwait(false);
        return null;
    }
}

internal sealed class ActionDescriptor<T0> : MethodDescriptor
{
    private readonly Func<T0, Task> action;

    public ActionDescriptor(
        Func<T0, Task> action, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.action = action;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);

        await this.action(arg0).
            ConfigureAwait(false);
        return null;
    }
}

internal sealed class ActionDescriptor<T0, T1> : MethodDescriptor
{
    private readonly Func<T0, T1, Task> action;

    public ActionDescriptor(
        Func<T0, T1, Task> action, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.action = action;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);
        var arg1 = base.ToObject<T1>(args[1]);

        await this.action(arg0, arg1).
            ConfigureAwait(false);
        return null;
    }
}

internal sealed class ActionDescriptor<T0, T1, T2> : MethodDescriptor
{
    private readonly Func<T0, T1, T2, Task> action;

    public ActionDescriptor(
        Func<T0, T1, T2, Task> action, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.action = action;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);
        var arg1 = base.ToObject<T1>(args[1]);
        var arg2 = base.ToObject<T2>(args[2]);

        await this.action(arg0, arg1, arg2).
            ConfigureAwait(false);
        return null;
    }
}

internal sealed class ActionDescriptor<T0, T1, T2, T3> : MethodDescriptor
{
    private readonly Func<T0, T1, T2, T3, Task> action;

    public ActionDescriptor(
        Func<T0, T1, T2, T3, Task> action, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.action = action;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);
        var arg1 = base.ToObject<T1>(args[1]);
        var arg2 = base.ToObject<T2>(args[2]);
        var arg3 = base.ToObject<T3>(args[3]);

        await this.action(arg0, arg1, arg2, arg3).
            ConfigureAwait(false);
        return null;
    }
}

///////////////////////////////////////////////////////////////////////////////

internal sealed class FuncDescriptor<TR> : MethodDescriptor
{
    private readonly Func<Task<TR>> func;

    public FuncDescriptor(
        Func<Task<TR>> func, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.func = func;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        Debug.Assert(args.Length == 0);
        return await this.func().
            ConfigureAwait(false);
    }
}

internal sealed class FuncDescriptor<TR, T0> : MethodDescriptor
{
    private readonly Func<T0, Task<TR>> func;

    public FuncDescriptor(
        Func<T0, Task<TR>> func, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.func = func;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);

        return await this.func(arg0).
            ConfigureAwait(false);
    }
}

internal sealed class FuncDescriptor<TR, T0, T1> : MethodDescriptor
{
    private readonly Func<T0, T1, Task<TR>> func;

    public FuncDescriptor(
        Func<T0, T1, Task<TR>> func, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.func = func;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);
        var arg1 = base.ToObject<T1>(args[1]);

        return await this.func(arg0, arg1).
            ConfigureAwait(false);
    }
}

internal sealed class FuncDescriptor<TR, T0, T1, T2> : MethodDescriptor
{
    private readonly Func<T0, T1, T2, Task<TR>> func;

    public FuncDescriptor(
        Func<T0, T1, T2, Task<TR>> func, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.func = func;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);
        var arg1 = base.ToObject<T1>(args[1]);
        var arg2 = base.ToObject<T2>(args[2]);

        return await this.func(arg0, arg1, arg2).
            ConfigureAwait(false);
    }
}

internal sealed class FuncDescriptor<TR, T0, T1, T2, T3> : MethodDescriptor
{
    private readonly Func<T0, T1, T2, T3, Task<TR>> func;

    public FuncDescriptor(
        Func<T0, T1, T2, T3, Task<TR>> func, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata) =>
        this.func = func;

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var arg0 = base.ToObject<T0>(args[0]);
        var arg1 = base.ToObject<T1>(args[1]);
        var arg2 = base.ToObject<T2>(args[2]);
        var arg3 = base.ToObject<T3>(args[3]);

        return await this.func(arg0, arg1, arg2, arg3).
            ConfigureAwait(false);
    }
}

///////////////////////////////////////////////////////////////////////////////

internal sealed class ObjectMethodDescriptor : MethodDescriptor
{
    private readonly object target;
    private readonly MethodInfo method;
    private readonly Type[] parameterTypes;

    public ObjectMethodDescriptor(
        object target, MethodInfo method, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata)
    {
        this.target = target;
        this.method = method;
        this.parameterTypes = this.method.
            GetParameters().
            Select(p => p.ParameterType).
            ToArray();
    }

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var cas = args.
            Select((arg, index) => base.ToObject(arg, this.parameterTypes[index])).
            ToArray();

        var task = (Task)this.method.Invoke(this.target, cas)!;
        return await TaskResultGetter.GetResultAsync(task);
    }
}

///////////////////////////////////////////////////////////////////////////////

internal sealed class DynamicMethodDescriptor : MethodDescriptor
{
    private readonly Delegate method;
    private readonly Type[] parameterTypes;

    public DynamicMethodDescriptor(
        Delegate method, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata)
    {
        this.method = method;
        this.parameterTypes =
            this.method.GetMethodInfo()!.
            GetParameters().
            Select(p => p.ParameterType).
            ToArray();
    }

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var cas = args.
            Select((arg, index) => base.ToObject(arg, this.parameterTypes[index])).
            ToArray();

        await ((Task)this.method.DynamicInvoke(cas)!).
            ConfigureAwait(false);
        return null;
    }
}

internal sealed class DynamicMethodDescriptor<TR> : MethodDescriptor
{
    private readonly Delegate method;
    private readonly Type[] parameterTypes;

    public DynamicMethodDescriptor(
        Delegate method, MethodMetadata metadata, IMessenger messenger) :
        base(messenger, metadata)
    {
        this.method = method;
        this.parameterTypes =
            this.method.GetMethodInfo()!.
            GetParameters().
            Select(p => p.ParameterType).
            ToArray();
    }

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var cas = args.
            Select((arg, index) => base.ToObject(arg, this.parameterTypes[index])).
            ToArray();

        var result = await ((Task<TR>)this.method.DynamicInvoke(cas)!).
            ConfigureAwait(false);
        return result;
    }
}

///////////////////////////////////////////////////////////////////////////////

internal sealed class DynamicFunctionDescriptor : MethodDescriptor
{
    private readonly Delegate function;
    private readonly Type[] parameterTypes;

    public DynamicFunctionDescriptor(
        Delegate function, IMessenger messenger) :
        base(messenger, new(false, null))
    {
        this.function = function;
        this.parameterTypes =
            this.function.GetMethodInfo()!.
            GetParameters().
            Select(p => p.ParameterType).
            ToArray();
    }

    public override async Task<object?> InvokeAsync(JToken?[] args)
    {
        var cas = args.
            Select((arg, index) => base.ToObject(arg, this.parameterTypes[index])).
            ToArray();

        var task = (Task)this.function.DynamicInvoke(cas)!;
        return await TaskResultGetter.GetResultAsync(task);
    }
}
