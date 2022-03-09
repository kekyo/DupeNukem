////////////////////////////////////////////////////////////////////////////
//
// DupeNukem - WebView attachable full-duplex asynchronous interoperable
// messaging library between .NET and JavaScript.
//
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DupeNukem.Internal
{
    internal enum MessageTypes
    {
        Success,
        Failed,
        Invoke,
    }

    internal readonly struct Message
    {
        public readonly string Id;
        public readonly MessageTypes MessageType;
        public readonly JToken Body;

        [JsonConstructor]
        public Message(string id, MessageTypes messageType, JToken body)
        {
            this.Id = id;
            this.MessageType = messageType;
            this.Body = body;
        }
    }

    internal readonly struct InvokeBody
    {
        public readonly string Name;
        public readonly object[] Args;

        [JsonConstructor]
        public InvokeBody(string name, object[] args)
        {
            this.Name = name;
            this.Args = args;
        }
    }

    internal readonly struct ExceptionBody
    {
        public readonly string Name;
        public readonly string Message;
        public readonly string Detail;

        [JsonConstructor]
        public ExceptionBody(string name, string message, string detail)
        {
            this.Name = name;
            this.Message = message;
            this.Detail = detail;
        }
    }
}
