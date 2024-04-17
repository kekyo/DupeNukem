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

namespace DupeNukem.Internal;

internal sealed class ByteArrayConverter : JsonConverter
{
    private static readonly Type type = typeof(byte[]);

    public override bool CanConvert(Type objectType) =>
        objectType.Equals(type);

    public override object? ReadJson(
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ConverterContext.AssertValidState();

        var typedValue = serializer.Deserialize<TypedValue>(reader);
        if (typedValue.Type == TypedValueTypes.ByteArray)
        {
            var base64 = typedValue.Body.ToObject<string>(serializer)!;
            return Convert.FromBase64String(base64);
        }
        return null;
    }

    public override void WriteJson(
        JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ConverterContext.AssertValidState();

        if (value is byte[] arr)
        {
            var base64 = Convert.ToBase64String(arr);
            var typedValue = new TypedValue(TypedValueTypes.ByteArray, base64);
            serializer.Serialize(writer, typedValue);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
