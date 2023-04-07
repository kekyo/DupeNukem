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
using System.Threading.Tasks;

namespace DupeNukem.Internal
{
    // SuspendingDescriptor indicates an asynchronous task
    // that is waiting to complete after calling a JavaScript function.
    // Since a return value should be returned upon completion,
    // it must be converted to the expected return type,
    // and several derived class variants exist.
    internal abstract class SuspendingDescriptor
    {
        public readonly DateTime Created = DateTime.Now;

        public abstract void Resolve(JToken? result);
        public abstract void Reject(Exception ex);
        public abstract void Cancel();
    }

    // SuspendingDescriptor with no return value.
    internal sealed class VoidSuspendingDescriptor : SuspendingDescriptor
    {
        private readonly TaskCompletionSource<object?> tcs = new();

        public Task Task =>
            this.tcs.Task;

        public override void Resolve(JToken? result) =>
            this.tcs.TrySetResult(null);
        public override void Reject(Exception ex) =>
            this.tcs.TrySetException(ex);
        public override void Cancel() =>
            this.tcs.TrySetCanceled();
    }

    // SuspendingDescriptor with a return value type.
    internal sealed class SuspendingDescriptor<TR> : SuspendingDescriptor
    {
        private readonly TaskCompletionSource<TR> tcs = new();

        public Task<TR> Task =>
            this.tcs.Task;

        public override void Resolve(JToken? result) =>
            this.tcs.TrySetResult(result is { } ? result.ToObject<TR>()! : default!);
        public override void Reject(Exception ex) =>
            this.tcs.TrySetException(ex);
        public override void Cancel() =>
            this.tcs.TrySetCanceled();
    }

    // SuspendingDescriptor with a return value type at runtime.
    internal sealed class DynamicSuspendingDescriptor : SuspendingDescriptor
    {
        private readonly Type returnType;
        private readonly TaskCompletionSource<object?> tcs = new();

        public DynamicSuspendingDescriptor(Type returnType) =>
            this.returnType = returnType;

        public Task<object?> Task =>
            this.tcs.Task;

        public override void Resolve(JToken? result) =>
            this.tcs.TrySetResult(result is { } ? result.ToObject(this.returnType)! : default!);
        public override void Reject(Exception ex) =>
            this.tcs.TrySetException(ex);
        public override void Cancel() =>
            this.tcs.TrySetCanceled();
    }
}
