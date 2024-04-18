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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DupeNukem.Internal;

internal sealed class JsonTokenConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        typeof(JsonElement).IsAssignableFrom(objectType);

    public override object? ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ConverterContext.AssertValidState();

        if (serializer.Deserialize<JToken>(reader) is { } token)
        {
            return JsonElement.FromJToken(ConverterContext.Current, token);
        }
        return null;
    }

    public override void WriteJson(
        JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ConverterContext.AssertValidState();

        if (value is JsonElement t)
        {
            serializer.Serialize(writer, t.token);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
