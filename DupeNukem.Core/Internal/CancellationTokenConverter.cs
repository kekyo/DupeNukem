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
using Newtonsoft.Json.Linq;

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

        internal void Cancel() =>
            this.cts.Cancel();

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
        ConverterContext.AssertValidState();

        var value = serializer.Deserialize<TypedValue>(reader);
        if (value.Type == TypedValueTypes.AbortSignal &&
            value.Body.ToObject<CancellationTokenBody>(serializer) is { } body &&
            !string.IsNullOrEmpty(body.Scope))
        {
            // Once the CancellationToken argument is found, the CancellationTokenProxy is generated and
            // make this instance visible from the JavaScript side.
            // By being called from the JavaScript side, as in "{Id}.cancel".
            var ctp = new CancellationTokenProxy();
            ConverterContext.Current.RegisterObject(body.Scope, ctp);

            // Already aborted:
            if (body.Aborted)
            {
                // Cancel now.
                ctp.Cancel();
            }

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
        ConverterContext.AssertValidState();

        if (value is CancellationToken c)
        {
            var scope = $"abortSignal_{0}";   // TODO:
            var cancellationTokenBody = new CancellationTokenBody(scope, c.IsCancellationRequested);
            var body = JToken.FromObject(cancellationTokenBody);
            var typedValue = new TypedValue(TypedValueTypes.AbortSignal, body);
            serializer.Serialize(writer, typedValue);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
