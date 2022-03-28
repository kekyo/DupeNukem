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

using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DupeNukem.Internal
{
    internal abstract class MethodDescriptor
    {
        public abstract Task<object?> InvokeAsync(JToken?[] args);

        protected T ToObject<T>(JToken? arg) =>
            (arg != null) ? arg.ToObject<T>()! : default(T)!;
        protected object? ToObject(JToken? arg, Type type) =>
            (arg != null) ? arg.ToObject(type) : Utilities.GetDefaultValue(type);
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class ActionDescriptor : MethodDescriptor
    {
        private readonly Func<Task> action;

        public ActionDescriptor(Func<Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            Debug.Assert(args.Length == 0);
            await this.action().ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0> : MethodDescriptor
    {
        private readonly Func<T0, Task> action;

        public ActionDescriptor(Func<T0, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            await this.action(
                base.ToObject<T0>(args[0])).
                ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0, T1> : MethodDescriptor
    {
        private readonly Func<T0, T1, Task> action;

        public ActionDescriptor(Func<T0, T1, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            await this.action(
                base.ToObject<T0>(args[0]),
                base.ToObject<T1>(args[1])).
                ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0, T1, T2> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, Task> action;

        public ActionDescriptor(Func<T0, T1, T2, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            await this.action(
                base.ToObject<T0>(args[0]),
                base.ToObject<T1>(args[1]),
                base.ToObject<T2>(args[2])).
                ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0, T1, T2, T3> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, T3, Task> action;

        public ActionDescriptor(Func<T0, T1, T2, T3, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            await this.action(
                base.ToObject<T0>(args[0]),
                base.ToObject<T1>(args[1]),
                base.ToObject<T2>(args[2]),
                base.ToObject<T3>(args[3])).
                ConfigureAwait(false);
            return null;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class FuncDescriptor<TR> : MethodDescriptor
    {
        private readonly Func<Task<TR>> func;

        public FuncDescriptor(Func<Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            Debug.Assert(args.Length == 0);
            return await this.func().ConfigureAwait(false);
        }
    }

    internal sealed class FuncDescriptor<TR, T0> : MethodDescriptor
    {
        private readonly Func<T0, Task<TR>> func;

        public FuncDescriptor(Func<T0, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(JToken?[] args) =>
            (await this.func(
                base.ToObject<T0>(args[0])).
                ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0, T1> : MethodDescriptor
    {
        private readonly Func<T0, T1, Task<TR>> func;

        public FuncDescriptor(Func<T0, T1, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(JToken?[] args) =>
            (await this.func(
                base.ToObject<T0>(args[0]),
                base.ToObject<T1>(args[1])).
                ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0, T1, T2> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, Task<TR>> func;

        public FuncDescriptor(Func<T0, T1, T2, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(JToken?[] args) =>
            (await this.func(
                base.ToObject<T0>(args[0]),
                base.ToObject<T1>(args[1]),
                base.ToObject<T2>(args[2])).
                ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0, T1, T2, T3> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, T3, Task<TR>> func;

        public FuncDescriptor(Func<T0, T1, T2, T3, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(JToken?[] args) =>
            (await this.func(
                base.ToObject<T0>(args[0]),
                base.ToObject<T1>(args[1]),
                base.ToObject<T2>(args[2]),
                base.ToObject<T3>(args[3])).
                ConfigureAwait(false))!;
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class ObjectMethodDescriptor : MethodDescriptor
    {
        private readonly object target;
        private readonly MethodInfo method;
        private readonly Type[] parameterTypes;

        public ObjectMethodDescriptor(object target, MethodInfo method)
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
                Select((arg, index) => this.ToObject(arg, this.parameterTypes[index])).
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

        public DynamicMethodDescriptor(Delegate method)
        {
            this.method = method;
            this.parameterTypes = this.method.Method.
                GetParameters().
                Select(p => p.ParameterType).
                ToArray();
        }

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            var cas = args.
                Select((arg, index) => this.ToObject(arg, this.parameterTypes[index])).
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

        public DynamicMethodDescriptor(Delegate method)
        {
            this.method = method;
            this.parameterTypes = this.method.Method.
                GetParameters().
                Select(p => p.ParameterType).
                ToArray();
        }

        public override async Task<object?> InvokeAsync(JToken?[] args)
        {
            var cas = args.
                Select((arg, index) => this.ToObject(arg, this.parameterTypes[index])).
                ToArray();
            var result = await ((Task<TR>)this.method.DynamicInvoke(cas)!).
                ConfigureAwait(false);
            return result;
        }
    }
}
