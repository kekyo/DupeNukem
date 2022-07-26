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

using DupeNukem.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Text;

#pragma warning disable CS0618

namespace DupeNukem
{
    public static class SendRequestEventArgsExtension
    {
        public static string ToJavaScript(this SendRequestEventArgs e) =>
            $"if (window.__dupeNukem_Messenger__ != undefined && window.__dupeNukem_Messenger__.arrivedHostMesssage__ != undefined) window.__dupeNukem_Messenger__.arrivedHostMesssage__('{e.JsonString.Replace("'", "\\'")}');";
    }

    public sealed class WebViewMessenger : Messenger
    {
        public WebViewMessenger(TimeSpan? timeoutDuration = default) :
            base(timeoutDuration)
        {
        }

        public WebViewMessenger(
            JsonSerializer serializer,
            NamingStrategy memberAccessNamingStrategy,
            TimeSpan? timeoutDuration) :
            base(serializer, memberAccessNamingStrategy, timeoutDuration)
        {
        }

        ///////////////////////////////////////////////////////////////////////////////

        public string PostMessageSymbolName =>
            "__dupeNukem_Messenger_sendToHostMessage__";

        public StringBuilder GetInjectionScript(bool debugLog = false)
        {
            using var s = this.GetType().Assembly.
                GetManifestResourceStream("DupeNukem.Script.js");
            var tr = new StreamReader(s!, Encoding.UTF8);
            var sb = new StringBuilder(tr.ReadToEnd());
            if (debugLog)
            {
                sb.AppendLine("__dupeNukem_Messenger__.debugLog__ = true;");
            }
            return sb;
        }

        private void InjectFunctionProxy(string name, MethodMetadata metadata)
        {
            var obsolete = metadata.Obsolete;

            var injectBody = new InjectBody(
                name,
                obsolete is { } ? (obsolete.IsError ? "error" : "obsolete") : null,
                obsolete is { } ? ($"{name} is obsoleted: {obsolete.Message ?? "(none)"}") : null);
            var request = new Message(
                "inject",
                MessageTypes.Control,
                JToken.FromObject(injectBody, this.Serializer));
            var tw = new StringWriter();
            this.Serializer.Serialize(tw, request);
            this.SendMessageToClient(tw.ToString());
        }

        private void DeleteFunctionProxy(string name)
        {
            var request = new Message(
                "delete",
                MessageTypes.Control,
                JToken.FromObject(name, this.Serializer));
            var tw = new StringWriter();
            this.Serializer.Serialize(tw, request);
            this.SendMessageToClient(tw.ToString());
        }

        protected override void OnRegisterMethod(
            string name, MethodDescriptor method, bool hasSpecifiedName)
        {
            if (method.Metadata.IsProxyInjecting)
            {
                this.InjectFunctionProxy(name, method.Metadata);
            }
        }

        protected override void OnUnregisterMethod(
            string name, MethodDescriptor method, bool hasSpecifiedName)
        {
            if (method.Metadata.IsProxyInjecting)
            {
                this.DeleteFunctionProxy(name);
            }
        }

        protected override void OnReady()
        {
            // Inject JavaScript proxies.
            foreach (var kv in this.GetRegisteredMethodPairs())
            {
                if (kv.Value.Metadata.IsProxyInjecting)
                {
                    this.InjectFunctionProxy(kv.Key, kv.Value.Metadata);
                }
            }
        }
    }
}
