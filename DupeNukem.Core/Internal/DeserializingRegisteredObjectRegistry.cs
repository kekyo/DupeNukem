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

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DupeNukem.Internal
{
    internal sealed class DeserializingRegisteredObjectRegistry
    {
        // HACK: During deserialization of Newtonsoft.Json, there is no way to pass an arbitrary context
        //   on the deserialization scope. This means that custom instances (e.g., CancellationToken)
        //   found during deserialization cannot be recorded after they are extracted.
        //   Therefore, they are kept in thread-local storage so that custom instances can be extracted
        //   after deserialization is complete.
        //   Note that this is thread-local storage, not asynchronous local storage.
        //   In other words, it is assumed that no asynchronous context switches occur during deserialization.
        private static readonly ThreadLocal<DeserializingRegisteredObjectRegistry> dror =
            new(() => new DeserializingRegisteredObjectRegistry());

        private readonly Dictionary<string, object> objects = new();

        private DeserializingRegisteredObjectRegistry()
        {
        }

        ///////////////////////////////////////////////////

        private void InternalBegin() =>
            this.objects.Clear();

        private bool InternalTryCapture(string name, object value)
        {
            if (!this.objects.ContainsKey(name))
            {
                this.objects.Add(name, value);
                return true;
            }
            else
            {
                return false;
            }
        }

        private KeyValuePair<string, object>[] InternalFinish() =>
            this.objects.ToArray();

        ///////////////////////////////////////////////////

        public static void Begin() =>
            dror.Value!.InternalBegin();

        public static bool TryCapture(string name, object value) =>
            dror.Value!.InternalTryCapture(name, value);

        public static KeyValuePair<string, object>[] Finish() =>
            dror.Value!.InternalFinish();
    }
}
