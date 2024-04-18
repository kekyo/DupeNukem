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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace DupeNukem;

// For loose type Json serializing.
// Using these classes, closures and byte arrays can also be handled correctly.
// Usage is nearly same as JToken and other classes.

/// <summary>
/// Safer represent JToken type.
/// </summary>
public abstract class JsonElement :
    IEquatable<JsonElement>
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal readonly Messenger? messenger;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal readonly JToken token;

    private protected JsonElement(
        Messenger? messenger, JToken token)
    {
        this.messenger = messenger;
        this.token = token;
    }

    /// <summary>
    /// Get related serializer.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [JsonIgnore]
    public JsonSerializer? Serializer =>
        this.messenger?.Serializer;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    [JsonIgnore]
    public JToken Token =>
        this.token;

    [JsonIgnore]
    public JTokenType JsonType =>
        this.token.Type;

    public bool Equals(JsonElement? rhs) =>
        rhs is { } r &&
        JToken.EqualityComparer.Equals(this.token, r.token);

    public override bool Equals(object? obj) =>
        this.Equals(obj as JsonElement);

    public override int GetHashCode() =>
        JToken.EqualityComparer.GetHashCode(this.token);

    public string ToString(Formatting formatting) =>
        this.Serializer is { } s ?
            this.token.ToString(formatting, s.Converters.ToArray()) :
            this.token.ToString(formatting);

    public override string ToString() =>
        this.ToString(Formatting.Indented);

    private protected IDisposable? Begin() =>
        this.messenger is { } m ? ConverterContext.Begin(m) : null;

    public object? ToObject(Type objectType)
    {
        using var _ = this.Begin();

        return this.messenger != null ?
            this.token.ToObject(objectType, this.messenger.Serializer) :
            this.token.ToObject(objectType);
    }

    public T ToObject<T>()
    {
        using var _ = this.Begin();

        return this.messenger != null ?
            this.token.ToObject<T>(this.messenger.Serializer)! :
            this.token.ToObject<T>()!;
    }

    internal static JsonElement FromJToken(
        Messenger? messenger, JToken token) =>
        token switch
        {
            JObject obj => new JsonObject(messenger, obj),
            JArray arr => new JsonArray(messenger, arr),
            JValue v => new JsonValue(messenger, v),
            _ => throw new ArgumentException(),
        };

    /// <summary>
    /// Create JsonElement from JToken.
    /// </summary>
    /// <param name="token">JToken</param>
    /// <returns>JsonElement if success.</returns>
    public static JsonElement FromJToken(JToken token) =>
        FromJToken(null, token);

    /// <summary>
    /// Create JsonElement from an instance.
    /// </summary>
    /// <param name="value">Instance or null</param>
    /// <returns>JsonElement if success.</returns>
    public static JsonElement FromObject(object? value) =>
        value switch
        {
            null => JsonValue.CreateNull(),
            bool v => new JsonValue(null, new JValue(v)),
            byte v => new JsonValue(null, new JValue(v)),
            sbyte v => new JsonValue(null, new JValue(v)),
            short v => new JsonValue(null, new JValue(v)),
            ushort v => new JsonValue(null, new JValue(v)),
            int v => new JsonValue(null, new JValue(v)),
            uint v => new JsonValue(null, new JValue(v)),
            long v => new JsonValue(null, new JValue(v)),
            ulong v => new JsonValue(null, new JValue(v)),
            float v => new JsonValue(null, new JValue(v)),
            double v => new JsonValue(null, new JValue(v)),
            decimal v => new JsonValue(null, new JValue(v)),
            char v => new JsonValue(null, new JValue(v)),
            string v => new JsonValue(null, new JValue(v)),
            DateTime v => new JsonValue(null, new JValue(v)),
            DateTimeOffset v => new JsonValue(null, new JValue(v)),
            TimeSpan v => new JsonValue(null, new JValue(v)),
            Guid v => new JsonValue(null, new JValue(v)),
            Uri v => new JsonValue(null, new JValue(v)),
            Array arr => new JsonArray(null, JArray.FromObject(arr)),
            Enum v => new JsonValue(null, new JValue(v)),
            JObject obj => new JsonObject(null, obj),
            JArray arr => new JsonArray(null, arr),
            JValue v => new JsonValue(null, v),
            JToken _ => throw new ArgumentException(),
            JsonElement v => v,
            _ => new JsonObject(null, JObject.FromObject(value)),
        };

    public static implicit operator JsonElement(bool value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(byte value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(sbyte value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(short value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(ushort value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(int value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(uint value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(long value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(ulong value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(float value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(double value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(decimal value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(char value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(string? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(DateTime value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(DateTimeOffset value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(TimeSpan value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(Guid value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(bool? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(byte? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(sbyte? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(short? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(ushort? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(int? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(uint? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(long? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(ulong? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(float? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(double? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(decimal? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(char? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(DateTime? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(DateTimeOffset? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(TimeSpan? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(Guid? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(Uri? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(Array? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(Enum? value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(JObject value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(JArray value) =>
        FromObject(value)!;
    public static implicit operator JsonElement(JValue value) =>
        FromObject(value)!;
}

/// <summary>
/// Safer represent JContainer type.
/// </summary>
public abstract class JsonContainer :
    JsonElement, IEnumerable
{
    private protected JsonContainer(
        Messenger? messenger, JContainer token) :
        base(messenger, token)
    {
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private new JContainer token =>
        (JContainer)base.token;

    [JsonIgnore]
    public int Count =>
        this.token.Count;

    private protected abstract IEnumerator OnGetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        this.OnGetEnumerator();

    /// <summary>
    /// Create JsonContainer derived instance from an instance.
    /// </summary>
    /// <param name="value">Instance</param>
    /// <returns>JsonContainer if success.</returns>
    public static new JsonContainer FromObject(object value) =>
        value switch
        {
            null => throw new ArgumentException(),
            bool _ => throw new ArgumentException(),
            byte _ => throw new ArgumentException(),
            sbyte _ => throw new ArgumentException(),
            short _ => throw new ArgumentException(),
            ushort _ => throw new ArgumentException(),
            int _ => throw new ArgumentException(),
            uint _ => throw new ArgumentException(),
            long _ => throw new ArgumentException(),
            ulong _ => throw new ArgumentException(),
            float _ => throw new ArgumentException(),
            double _ => throw new ArgumentException(),
            decimal _ => throw new ArgumentException(),
            char _ => throw new ArgumentException(),
            string _ => throw new ArgumentException(),
            DateTime _ => throw new ArgumentException(),
            DateTimeOffset _ => throw new ArgumentException(),
            TimeSpan _ => throw new ArgumentException(),
            Guid _ => throw new ArgumentException(),
            Uri _ => throw new ArgumentException(),
            Array arr => new JsonArray(null, JArray.FromObject(arr)),
            Enum _ => throw new ArgumentException(),
            JObject obj => new JsonObject(null, obj),
            JArray arr => new JsonArray(null, arr),
            JToken _ => throw new ArgumentException(),
            JsonContainer c => c,
            JsonElement _ => throw new ArgumentException(),
            _ => new JsonObject(null, JObject.FromObject(value)),
        };

    public static implicit operator JsonContainer(Array value) =>
        FromObject(value)!;
    public static implicit operator JsonContainer(JContainer value) =>
        FromObject(value)!;
}

[DebuggerDisplay("{Name} : {Value}")]
internal readonly struct JsonPropertyItem
{
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string Name { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public JsonElement Value { get; }

    public JsonPropertyItem(string name, JsonElement value)
    {
        this.Name = name;
        this.Value = value;
    }
}

internal sealed class JsonObjectDebuggerTypeProxy
{
    private JsonObject jo;

    internal JsonObjectDebuggerTypeProxy(JsonObject jo) =>
        this.jo = jo;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal JsonPropertyItem[] Items =>
        this.jo.Select(kv => new JsonPropertyItem(kv.Key, kv.Value)).ToArray();
}

/// <summary>
/// Safer represent JObject type.
/// </summary>
[DebuggerDisplay("JsonObject: Count = {Count}")]
[DebuggerTypeProxy(typeof(JsonObjectDebuggerTypeProxy))]
public sealed class JsonObject :
    JsonContainer, IEnumerable<KeyValuePair<string, JsonElement>>, IEnumerable
#if !NET35 && !NET40
    , IReadOnlyDictionary<string, JsonElement>
#endif
{
    internal JsonObject(
        Messenger? messenger, JObject token) :
        base(messenger, token)
    {
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private new JObject token =>
        (JObject)base.token;

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    [JsonIgnore]
    public IEnumerable<string> Keys
    {
        get
        {
            using var _ = base.Begin();

            foreach (var p in this.token.Children().OfType<JProperty>())
            {
                yield return p.Name;
            }
        }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    [JsonIgnore]
    public IEnumerable<JsonElement> Values
    {
        get
        {
            using var _ = base.Begin();

            foreach (var p in this.token.Children().OfType<JProperty>())
            {
                yield return FromJToken(this.messenger, p.Value);
            }
        }
    }

    [JsonIgnore]
    public JsonElement this[string propertyName]
    {
        get
        {
            using var _ = base.Begin();

            return this.token.TryGetValue(propertyName, out var t) ?
                FromJToken(this.messenger!, t) :
                throw new KeyNotFoundException(propertyName);
        }
    }

    public bool ContainsKey(string key) =>
        this.token.ContainsKey(key);

    public bool TryGetValue(string key, out JsonElement value)
    {
        using var _ = base.Begin();

        if (this.token.TryGetValue(key, out var t))
        {
            value = FromJToken(this.messenger, t);
            return true;
        }
        else
        {
            value = null!;
            return false;
        }
    }

    public IEnumerator<KeyValuePair<string, JsonElement>> GetEnumerator()
    {
        using var _ = base.Begin();

        foreach (var p in this.token.Children().OfType<JProperty>())
        {
            yield return new(p.Name, FromJToken(this.messenger, p.Value));
        }
    }

    private protected override IEnumerator OnGetEnumerator() =>
        this.GetEnumerator();

    /// <summary>
    /// Create JsonObject derived instance from an instance.
    /// </summary>
    /// <param name="value">Instance</param>
    /// <returns>JsonObject if success.</returns>
    public static new JsonObject FromObject(object value) =>
        value switch
        {
            null => throw new ArgumentException(),
            bool _ => throw new ArgumentException(),
            byte _ => throw new ArgumentException(),
            sbyte _ => throw new ArgumentException(),
            short _ => throw new ArgumentException(),
            ushort _ => throw new ArgumentException(),
            int _ => throw new ArgumentException(),
            uint _ => throw new ArgumentException(),
            long _ => throw new ArgumentException(),
            ulong _ => throw new ArgumentException(),
            float _ => throw new ArgumentException(),
            double _ => throw new ArgumentException(),
            decimal _ => throw new ArgumentException(),
            char _ => throw new ArgumentException(),
            string _ => throw new ArgumentException(),
            DateTime _ => throw new ArgumentException(),
            DateTimeOffset _ => throw new ArgumentException(),
            TimeSpan _ => throw new ArgumentException(),
            Guid _ => throw new ArgumentException(),
            Uri _ => throw new ArgumentException(),
            Array _ => throw new ArgumentException(),
            Enum _ => throw new ArgumentException(),
            JObject obj => new JsonObject(null, obj),
            JToken _ => throw new ArgumentException(),
            JsonObject obj => obj,
            JsonElement _ => throw new ArgumentException(),
            _ => new JsonObject(null, new JObject(value)),
        };
}

internal sealed class JsonArrayDebuggerTypeProxy
{
    private JsonArray ja;

    internal JsonArrayDebuggerTypeProxy(JsonArray ja) =>
        this.ja = ja;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    internal JsonElement?[] Items =>
        this.ja.ToArray();
}

/// <summary>
/// Safer represent JArray type.
/// </summary>
[DebuggerDisplay("JsonArray: Count = {Count}")]
[DebuggerTypeProxy(typeof(JsonArrayDebuggerTypeProxy))]
public sealed class JsonArray :
    JsonContainer, IEnumerable<JsonElement?>, IEnumerable
#if !NET35 && !NET40
    , IReadOnlyList<JsonElement?>
#endif
{
    internal JsonArray(
        Messenger? messenger, JArray token) :
        base(messenger, token)
    {
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private new JArray token =>
        (JArray)base.token;

    [JsonIgnore]
    public JsonElement? this[int index]
    {
        get
        {
            using var _ = base.Begin();

            return FromJToken(this.messenger, this.token[index]);
        }
    }

    public IEnumerator<JsonElement?> GetEnumerator()
    {
        using var _ = base.Begin();

        foreach (var t in this.token.Children())
        {
            yield return FromJToken(this.messenger, t);
        }
    }

    private protected override IEnumerator OnGetEnumerator() =>
        this.GetEnumerator();

    /// <summary>
    /// Create JsonArray from an instance.
    /// </summary>
    /// <param name="value">Instance</param>
    /// <returns>JsonValue if success.</returns>
    public static new JsonArray FromObject(object value) =>
        value switch
        {
            null => throw new ArgumentException(),
            Array arr => new JsonArray(null, new JArray(arr)),
            JArray arr => new JsonArray(null, arr),
            JsonArray arr => arr,
            _ => throw new ArgumentException(),
        };

    public static implicit operator JsonArray(Array value) =>
        FromObject(value)!;
    public static implicit operator JsonArray(JArray value) =>
        FromObject(value)!;
}

/// <summary>
/// Safer represent JValue type.
/// </summary>
[DebuggerDisplay("{DisplayString}")]
public sealed class JsonValue :
    JsonElement
{
    internal JsonValue(
        Messenger? messenger, JValue token) :
        base(messenger, token)
    {
    }

    public object? Value =>
        ((JValue)base.token).Value;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DisplayString =>
        this.Value switch
        {
            null => null,
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            bool v => v.ToString(),
            var v => $"{v.GetType().Name}: {v}",
        } ?? "(null)";

    public static JsonValue CreateNull() =>
        new JsonValue(null, JValue.CreateNull());

    /// <summary>
    /// Create JsonValue from an instance.
    /// </summary>
    /// <param name="value">Instance or null</param>
    /// <returns>JsonValue or null if success.</returns>
    public static new JsonValue FromObject(object? value) =>
        value switch
        {
            null => CreateNull(),
            bool v => new JsonValue(null, new JValue(v)),
            byte v => new JsonValue(null, new JValue(v)),
            sbyte v => new JsonValue(null, new JValue(v)),
            short v => new JsonValue(null, new JValue(v)),
            ushort v => new JsonValue(null, new JValue(v)),
            int v => new JsonValue(null, new JValue(v)),
            uint v => new JsonValue(null, new JValue(v)),
            long v => new JsonValue(null, new JValue(v)),
            ulong v => new JsonValue(null, new JValue(v)),
            float v => new JsonValue(null, new JValue(v)),
            double v => new JsonValue(null, new JValue(v)),
            decimal v => new JsonValue(null, new JValue(v)),
            char v => new JsonValue(null, new JValue(v)),
            string v => new JsonValue(null, new JValue(v)),
            DateTime v => new JsonValue(null, new JValue(v)),
            DateTimeOffset v => new JsonValue(null, new JValue(v)),
            TimeSpan v => new JsonValue(null, new JValue(v)),
            Guid v => new JsonValue(null, new JValue(v)),
            Uri v => new JsonValue(null, new JValue(v)),
            Enum v => new JsonValue(null, new JValue(v)),
            JValue v => new JsonValue(null, v),
            JsonValue v => v,
            _ => throw new ArgumentException(),
        };

    public static implicit operator JsonValue(bool value) =>
        FromObject(value);
    public static implicit operator JsonValue(byte value) =>
        FromObject(value);
    public static implicit operator JsonValue(sbyte value) =>
        FromObject(value);
    public static implicit operator JsonValue(short value) =>
        FromObject(value);
    public static implicit operator JsonValue(ushort value) =>
        FromObject(value);
    public static implicit operator JsonValue(int value) =>
        FromObject(value);
    public static implicit operator JsonValue(uint value) =>
        FromObject(value);
    public static implicit operator JsonValue(long value) =>
        FromObject(value);
    public static implicit operator JsonValue(ulong value) =>
        FromObject(value);
    public static implicit operator JsonValue(float value) =>
        FromObject(value);
    public static implicit operator JsonValue(double value) =>
        FromObject(value);
    public static implicit operator JsonValue(decimal value) =>
        FromObject(value);
    public static implicit operator JsonValue(char value) =>
        FromObject(value);
    public static implicit operator JsonValue(string? value) =>
        FromObject(value);
    public static implicit operator JsonValue(DateTime value) =>
        FromObject(value);
    public static implicit operator JsonValue(DateTimeOffset value) =>
        FromObject(value);
    public static implicit operator JsonValue(TimeSpan value) =>
        FromObject(value);
    public static implicit operator JsonValue(Guid value) =>
        FromObject(value);
    public static implicit operator JsonValue(bool? value) =>
        FromObject(value);
    public static implicit operator JsonValue(byte? value) =>
        FromObject(value);
    public static implicit operator JsonValue(sbyte? value) =>
        FromObject(value);
    public static implicit operator JsonValue(short? value) =>
        FromObject(value);
    public static implicit operator JsonValue(ushort? value) =>
        FromObject(value);
    public static implicit operator JsonValue(int? value) =>
        FromObject(value);
    public static implicit operator JsonValue(uint? value) =>
        FromObject(value);
    public static implicit operator JsonValue(long? value) =>
        FromObject(value);
    public static implicit operator JsonValue(ulong? value) =>
        FromObject(value);
    public static implicit operator JsonValue(float? value) =>
        FromObject(value);
    public static implicit operator JsonValue(double? value) =>
        FromObject(value);
    public static implicit operator JsonValue(decimal? value) =>
        FromObject(value);
    public static implicit operator JsonValue(char? value) =>
        FromObject(value);
    public static implicit operator JsonValue(DateTime? value) =>
        FromObject(value);
    public static implicit operator JsonValue(DateTimeOffset? value) =>
        FromObject(value);
    public static implicit operator JsonValue(TimeSpan? value) =>
        FromObject(value);
    public static implicit operator JsonValue(Guid? value) =>
        FromObject(value);
    public static implicit operator JsonValue(Uri? value) =>
        FromObject(value);
    public static implicit operator JsonValue(Array? value) =>
        FromObject(value);
    public static implicit operator JsonValue(Enum? value) =>
        FromObject(value);
    public static implicit operator JsonValue(JValue value) =>
        FromObject(value);
}
