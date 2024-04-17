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
using Newtonsoft.Json;

namespace DupeNukem.Internal;

internal sealed class ClosureConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        typeof(Delegate).IsAssignableFrom(objectType);

    public override object? ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ConverterContext.AssertValidState();

        var typedValue = serializer.Deserialize<TypedValue>(reader);
        if (typedValue.Type == TypedValueTypes.Closure)
        {
            var name = typedValue.Body.ToObject<string>(serializer)!;
            if (name.StartsWith("__peerClosures__.closure_$"))
            {
                return ConverterContext.Current.RegisterPeerClosure(name, objectType);
            }
        }
        return null;
    }

    public override void WriteJson(
        JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ConverterContext.AssertValidState();

        if (value is Delegate dlg)
        {
            var name = ConverterContext.Current.RegisterHostClosure(dlg);
            var typedValue = new TypedValue(TypedValueTypes.Closure, name);
            serializer.Serialize(writer, typedValue);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
