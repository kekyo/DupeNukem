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
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace DupeNukem.Internal
{
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
    internal enum MessageTypes
    {
        Control,
        Succeeded,
        Failed,
        Invoke,
    }

    internal readonly struct Message
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

        [JsonConstructor]
        public ExceptionBody(string name, string message, string detail)
        {
            this.Name = name;
            this.Message = message;
            this.Detail = detail;
        }
    }

    internal readonly struct CancellationTokenBody
    {
        [JsonProperty("__scope__")]
        public readonly string Scope;

        [JsonConstructor]
        public CancellationTokenBody(string id) =>
            this.Scope = id;
    }
}
