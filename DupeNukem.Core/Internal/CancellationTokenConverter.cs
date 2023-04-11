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
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DupeNukem.Internal;

internal sealed class CancellationTokenConverter : JsonConverter
{
    private static readonly Type type = typeof(CancellationToken);

    public override bool CanConvert(Type objectType) =>
        objectType.Equals(type);

    public sealed class CancellationTokenProxy
    {
        private readonly CancellationTokenSource cts = new();

        public CancellationToken Token =>
            this.cts.Token;

        [CallableTarget("cancel")]
        public Task CancelAsync()
        {
            this.cts.Cancel();
            return Utilities.CompletedTask;
        }
    }

    public override object? ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var body = serializer.Deserialize<CancellationTokenBody>(reader);
        if (!string.IsNullOrEmpty(body.Scope))
        {
            // Once the CancellationToken argument is found, the CancellationTokenProxy is generated and
            // make this instance visible from the JavaScript side.
            // (Actually, it will be extracted and registered later from the DeserializingRegisteredObjectRegistry.)
            // By being called from the JavaScript side, as in "{Id}.cancel".
            // CancellationTokenSource.Cancel() is called which is held internally.
            var ctp = new CancellationTokenProxy();
            DeserializingRegisteredObjectRegistry.TryCapture(body.Scope, ctp);
            return ctp.Token;
        }
        else
        {
            return default(CancellationToken);
        }
    }

    public override void WriteJson(
        JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var ct = (CancellationToken)value!;

        // TODO:
        //writer.WriteValue(tag.Name);
    }
}
