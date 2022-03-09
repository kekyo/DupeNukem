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

using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace DupeNukem.Internal
{
    internal abstract class SuspendingDescriptor
    {
        public readonly DateTime Created = DateTime.Now;

        public abstract void Resolve(JToken? result);
        public abstract void Reject(Exception ex);
        public abstract void Cancel();
    }

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
}
