﻿////////////////////////////////////////////////////////////////////////////
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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.ComponentModel;

namespace DupeNukem.Internal;

[EditorBrowsable(EditorBrowsableState.Advanced)]
[JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
public enum MessageTypes
{
    Control,
    Succeeded,
    Failed,
    Invoke,
}

[EditorBrowsable(EditorBrowsableState.Advanced)]
public readonly struct Message
{
    [JsonProperty("id")]
    public readonly string Id;
    [JsonProperty("type")]
    public readonly MessageTypes Type;
    [JsonProperty("body")]
    public readonly JToken? Body;

    [JsonConstructor]
    public Message(string id, MessageTypes type, JToken? body)
    {
        this.Id = id;
        this.Type = type;
        this.Body = body;
    }
}

internal readonly struct InvokeBody
{
    [JsonProperty("name")]
    public readonly string Name;
    [JsonProperty("args")]
    public readonly JToken?[] Args;

    [JsonConstructor]
    public InvokeBody(string name, JToken?[] args)
    {
        this.Name = name;
        this.Args = args;
    }
}

internal readonly struct ExceptionBody
{
    [JsonProperty("name")]
    public readonly string Name;
    [JsonProperty("message")]
    public readonly string Message;
    [JsonProperty("detail")]
    public readonly string Detail;
    [JsonProperty("props")]
    public readonly Dictionary<string, object?> Properties;

    [JsonConstructor]
    public ExceptionBody(
        string name, string message, string detail,
        Dictionary<string, object?> properties)
    {
        this.Name = name;
        this.Message = message;
        this.Detail = detail;
        this.Properties = properties;
    }
}

internal enum TypedValueTypes
{
    Closure,
    AbortSignal,
    ByteArray,
}

internal readonly struct TypedValue
{
    [JsonProperty("__type__")]
    public readonly TypedValueTypes Type;

    [JsonProperty("__body__")]
    public readonly JToken Body;

    [JsonConstructor]
    public TypedValue(TypedValueTypes __type__, JToken __body__)
    {
        this.Type = __type__;
        this.Body = __body__;
    }
}

internal readonly struct CancellationTokenBody
{
    [JsonProperty("__scope__")]
    public readonly string Scope;

    [JsonProperty("__aborted__")]
    public readonly bool Aborted;

    [JsonConstructor]
    public CancellationTokenBody(string __scope__, bool __aborted__)
    {
        this.Scope = __scope__;
        this.Aborted = __aborted__;
    }
}
