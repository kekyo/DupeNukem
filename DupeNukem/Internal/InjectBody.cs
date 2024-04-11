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
using Newtonsoft.Json.Serialization;

namespace DupeNukem.Internal
{
    internal readonly struct InjectBody
    {
        [JsonProperty("name")]
        public readonly string Name;
        [JsonProperty("obsolete", NullValueHandling = NullValueHandling.Ignore)]
        public readonly string? Obsolete;
        [JsonProperty("obsoleteMessage", NullValueHandling = NullValueHandling.Ignore)]
        public readonly string? ObsoleteMessage;

        [JsonConstructor]
        public InjectBody(string name, string? obsolete, string? obsoleteMessage)
        {
            this.Name = name;
            this.Obsolete = obsolete;
            this.ObsoleteMessage = obsoleteMessage;
        }
    }
}
