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

namespace DupeNukem;

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

    public override void Dispose()
    {
        base.Dispose();
        this.Ready = null;
    }

    ///////////////////////////////////////////////////////////////////////////////

    public event EventHandler? Ready;

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
        this.SendControlMessageToPeer("inject", injectBody);
    }

    private void DeleteFunctionProxy(string name) =>
        this.SendControlMessageToPeer("delete", name);

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

    protected override async void OnReceivedControlMessage(
        string controlId, JToken? body)
    {
        switch (controlId)
        {
            case "ready":
                // Exhausted page content, maybe all suspending tasks are zombies.
                this.CancelAllSuspending();

                await this.SynchContext.Bind();

                // Inject JavaScript proxies.
                foreach (var kv in this.GetRegisteredMethodPairs())
                {
                    if (kv.Value.Metadata.IsProxyInjecting)
                    {
                        this.InjectFunctionProxy(kv.Key, kv.Value.Metadata);
                    }
                }

                // Invoke ready event.
                this.Ready?.Invoke(this, EventArgs.Empty);
                break;
        }
    }
}
