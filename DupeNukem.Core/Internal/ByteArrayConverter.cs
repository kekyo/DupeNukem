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
        JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) =>
        reader.ReadAsString() is { } body ?
            Convert.FromBase64String(body) :
            null;

    public override void WriteJson(
        JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is byte[] arr)
        {
            var body = Convert.ToBase64String(arr);
            writer.WriteValue(body);
        }
        else
        {
            writer.WriteNull();
        }
    }
}
