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

using Android.Webkit;
using DupeNukem.Maui.Controls;
using Java.Interop;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using System;
using System.Threading.Tasks;

namespace DupeNukem.Maui;

internal sealed class JavaScriptInterface :
    Java.Lang.Object
{
    private readonly WeakReference<JavaScriptMultiplexedWebViewHandler> handler;

    public JavaScriptInterface(JavaScriptMultiplexedWebViewHandler parent) =>
        this.handler = new(parent);

    [global::Android.Webkit.JavascriptInterface]
    [Export(WebViewMessenger.PostMessageHostSymbolName)]
    public void SendToHostMessage(string data)
    {
        if (this.handler.TryGetTarget(out var handler) &&
            handler.VirtualView is JavaScriptMultiplexedWebView webView)
        {
            webView.SendToHostMessage(data);
        }
    }
}

internal sealed class JavaScriptResultCallback :
    Java.Lang.Object, IValueCallback
{
    private readonly TaskCompletionSource<Java.Lang.Object?> tcs = new();

    public Task<Java.Lang.Object?> Task =>
        tcs.Task;

    public void OnReceiveValue(Java.Lang.Object? value) =>
        this.tcs.TrySetResult(value);
}

// DIRTY: JavaScriptMultiplexedWebView:
//   MAUI does not have a platform-neutral event interface,
//   so must implement MAUI handlers on each platform...
//   In this project, examples for Android and Windows are provided.
//   For more information, search for JavaScriptMultiplexedWebView.
public sealed class JavaScriptMultiplexedWebViewHandler :
    WebViewHandler
{
    public JavaScriptMultiplexedWebViewHandler()
    {
    }

    protected override global::Android.Webkit.WebView CreatePlatformView()
    {
        var nativeWebView = base.CreatePlatformView();

        nativeWebView.Settings.JavaScriptEnabled = true;
        nativeWebView.Settings.DomStorageEnabled = true;
        nativeWebView.AddJavascriptInterface(
            new JavaScriptInterface(this),
            WebViewMessenger.PostMessageHostObjectName);

        ((JavaScriptMultiplexedWebView)base.VirtualView).SetInvokedJavaScriptListener(
            script =>
            {
                var callback = new JavaScriptResultCallback();
                nativeWebView.EvaluateJavascript(script, callback);
                return callback.Task;
            });

        return nativeWebView;
    }
}
