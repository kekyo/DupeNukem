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

using System;
using System.Threading.Tasks;

namespace DupeNukem.Internal
{
    internal abstract class MethodDescriptor
    {
        public abstract Task<object?> InvokeAsync(object?[] args);
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class ActionDescriptor : MethodDescriptor
    {
        private readonly Func<Task> action;

        public ActionDescriptor(Func<Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            await this.action().ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0> : MethodDescriptor
    {
        private readonly Func<T0, Task> action;

        public ActionDescriptor(Func<T0, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            await this.action((T0)args[0]!).ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0, T1> : MethodDescriptor
    {
        private readonly Func<T0, T1, Task> action;

        public ActionDescriptor(Func<T0, T1, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            await this.action((T0)args[0]!, (T1)args[1]!).ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0, T1, T2> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, Task> action;

        public ActionDescriptor(Func<T0, T1, T2, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            await this.action((T0)args[0]!, (T1)args[1]!, (T2)args[2]!).ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class ActionDescriptor<T0, T1, T2, T3> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, T3, Task> action;

        public ActionDescriptor(Func<T0, T1, T2, T3, Task> action) =>
            this.action = action;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            await this.action((T0)args[0]!, (T1)args[1]!, (T2)args[2]!, (T3)args[3]!).ConfigureAwait(false);
            return null;
        }
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class FuncDescriptor<TR> : MethodDescriptor
    {
        private readonly Func<Task<TR>> func;

        public FuncDescriptor(Func<Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(object?[] args) =>
            (await this.func().ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0> : MethodDescriptor
    {
        private readonly Func<T0, Task<TR>> func;

        public FuncDescriptor(Func<T0, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(object?[] args) =>
            (await this.func((T0)args[0]!).ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0, T1> : MethodDescriptor
    {
        private readonly Func<T0, T1, Task<TR>> func;

        public FuncDescriptor(Func<T0, T1, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(object?[] args) =>
            (await this.func((T0)args[0]!, (T1)args[1]!).ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0, T1, T2> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, Task<TR>> func;

        public FuncDescriptor(Func<T0, T1, T2, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(object?[] args) =>
            (await this.func((T0)args[0]!, (T1)args[1]!, (T2)args[2]!).ConfigureAwait(false))!;
    }

    internal sealed class FuncDescriptor<TR, T0, T1, T2, T3> : MethodDescriptor
    {
        private readonly Func<T0, T1, T2, T3, Task<TR>> func;

        public FuncDescriptor(Func<T0, T1, T2, T3, Task<TR>> func) =>
            this.func = func;

        public override async Task<object?> InvokeAsync(object?[] args) =>
            (await this.func((T0)args[0]!, (T1)args[1]!, (T2)args[2]!, (T3)args[3]!).ConfigureAwait(false))!;
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class DynamicMethodDescriptor : MethodDescriptor
    {
        private readonly Delegate method;

        public DynamicMethodDescriptor(Delegate method) =>
            this.method = method;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            await ((Task)this.method.DynamicInvoke(args)!).ConfigureAwait(false);
            return null;
        }
    }

    internal sealed class DynamicMethodDescriptor<TR> : MethodDescriptor
    {
        private readonly Delegate method;

        public DynamicMethodDescriptor(Delegate method) =>
            this.method = method;

        public override async Task<object?> InvokeAsync(object?[] args)
        {
            var result = await ((Task<TR>)this.method.DynamicInvoke(args)!).ConfigureAwait(false);
            return result;
        }
    }
}
