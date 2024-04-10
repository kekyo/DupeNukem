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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem.Internal
{
    internal abstract class ClosureTrampolineFactory
    {
        // The functionality of `ClosureTrampoline` is very confusing:
        // In JavaScript, there are no static types,
        // so the signature of a function to be called back is
        // always considered as likely `object[] -> object`.
        // However in the .NET world, this is unwieldy, so as a method interface
        // we want to make it a delegate like `Func<T0, T1, T2 ... TR>`.
        // `ClosureTrampoline` is the bridge between these delegates.
        // The implementation is very messy, but it gets the type
        // information from the typed delegate by reflection and
        // instantiates `ClosureTrampolineFactory<...>`.
        // To reduce the burden on some reflection APIs,
        // the generated `ClosureTrampolineFactory` instance is cached.
        protected abstract class ClosureTrampoline
        {
            // Delegate for targeted, untyped JavaScript function call.
            private readonly Func<object?[], Type, CancellationToken, Task<object?>> target;

            protected ClosureTrampoline(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
                this.target = target;

            // Bypass invoking JavaScript function call with untyped arguments.
            protected async Task<TR> InvokeAsync<TR>(object?[] args)
            {
                // Uses CT when found CT in the args.
                if (args.FirstOrDefault(arg => arg is CancellationToken) is not CancellationToken ct)
                {
                    ct = default;
                }
                var result = await this.target(args, typeof(TR), ct).
                    ConfigureAwait(false);
                return (TR)result!;
            }
        }

        // The cached factories.
        private static Dictionary<Type, ClosureTrampolineFactory> factories = new();

        protected ClosureTrampolineFactory()
        {
        }

        protected abstract MethodInfo Method { get; }

        protected abstract ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target);

        // Create bypass delegate with matching to a .NET delegate type.
        public static Delegate? Create(
            Type delegateType,
            Func<object?[], Type, CancellationToken, Task<object?>> target)
        {
            ClosureTrampolineFactory factory;

            lock (factories)
            {
                if (!factories.TryGetValue(delegateType, out factory!))
                {
                    // (Compiler generated invoke method)
                    var im = delegateType.GetMethod("Invoke")!;
                    if (im == null)
                    {
                        return null;
                    }

                    // Task<TR>
                    if (!typeof(Task).IsAssignableFrom(im.ReturnType))
                    {
                        return null;
                    }

                    // TR
                    if (im.ReturnType.
                        GetGenericArguments().
                        FirstOrDefault() is not { } returnElementType)
                    {
                        return null;
                    }

                    // Makes `T0, T1, T2, ... TR`
                    var gas = im.GetParameters().
                        Select(p => p.ParameterType).
                        Append(returnElementType).                        
                        ToArray();

                    // Concrete supported delegate argument type counts up to 6 (6 + 1 (return type)).
                    // Reflection.Emit allows for a variable number of arguments to be supported,
                    // but I compromised in consideration of environments where Emitter is not available.
                    Type factoryType;
                    switch (gas.Length)
                    {
                        case 1:
                            factoryType = typeof(ClosureTrampolineFactory<>).MakeGenericType(gas);
                            break;
                        case 2:
                            factoryType = typeof(ClosureTrampolineFactory<,>).MakeGenericType(gas);
                            break;
                        case 3:
                            factoryType = typeof(ClosureTrampolineFactory<,,>).MakeGenericType(gas);
                            break;
                        case 4:
                            factoryType = typeof(ClosureTrampolineFactory<,,,>).MakeGenericType(gas);
                            break;
                        case 5:
                            factoryType = typeof(ClosureTrampolineFactory<,,,,>).MakeGenericType(gas);
                            break;
                        case 6:
                            factoryType = typeof(ClosureTrampolineFactory<,,,,,>).MakeGenericType(gas);
                            break;
                        case 7:
                            factoryType = typeof(ClosureTrampolineFactory<,,,,,,>).MakeGenericType(gas);
                            break;
                        default:
                            return null;
                    };

                    factory = (ClosureTrampolineFactory)Activator.CreateInstance(factoryType)!;
                    factories.Add(delegateType, factory);
                }
            }

            var trampoline = factory.Create(target);
            return factory.Method.CreateDelegate(delegateType, trampoline);
        }
    }

    internal sealed class ClosureTrampolineFactory<TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync() =>
                base.InvokeAsync<TR>(Utilities.Empty<object?>());
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }

    internal sealed class ClosureTrampolineFactory<T0, TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync(T0 arg0) =>
                base.InvokeAsync<TR>(new object?[] { arg0 });
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }

    internal sealed class ClosureTrampolineFactory<T0, T1, TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync(T0 arg0, T1 arg1) =>
                base.InvokeAsync<TR>(new object?[] { arg0, arg1 });
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }

    internal sealed class ClosureTrampolineFactory<T0, T1, T2, TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync(T0 arg0, T1 arg1, T2 arg2) =>
                base.InvokeAsync<TR>(new object?[] { arg0, arg1, arg2 });
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }

    internal sealed class ClosureTrampolineFactory<T0, T1, T2, T3, TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync(T0 arg0, T1 arg1, T2 arg2, T3 arg3) =>
                base.InvokeAsync<TR>(new object?[] { arg0, arg1, arg2, arg3 });
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }

    internal sealed class ClosureTrampolineFactory<T0, T1, T2, T3, T4, TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
                base.InvokeAsync<TR>(new object?[] { arg0, arg1, arg2, arg3, arg4 });
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }

    internal sealed class ClosureTrampolineFactory<T0, T1, T2, T3, T4, T5, TR> : ClosureTrampolineFactory
    {
        protected override MethodInfo Method { get; } =
            typeof(ClosureTrampolineImpl).GetMethod("InvokeAsync")!;

        private sealed class ClosureTrampolineImpl : ClosureTrampoline
        {
            public ClosureTrampolineImpl(Func<object?[], Type, CancellationToken, Task<object?>> target) :
                base(target)
            {
            }

            public Task<TR> InvokeAsync(T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
                base.InvokeAsync<TR>(new object?[] { arg0, arg1, arg2, arg3, arg4, arg5 });
        }

        protected override ClosureTrampoline Create(Func<object?[], Type, CancellationToken, Task<object?>> target) =>
            new ClosureTrampolineImpl(target);
    }
}
