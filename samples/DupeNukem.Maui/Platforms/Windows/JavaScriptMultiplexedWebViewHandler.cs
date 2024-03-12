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

using DupeNukem.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DupeNukem.Maui;

// DIRTY: JavaScriptMultiplexedWebView:
//   MAUI does not have a platform-neutral event interface,
//   so must implement MAUI handlers on each platform...
//   In this project, examples for Android and Windows are provided.
//   For more information, search for JavaScriptMultiplexedWebView.
internal sealed class JavaScriptMultiplexedWebViewHandler :
    WebViewHandler
{
    public JavaScriptMultiplexedWebViewHandler()
    {
    }

    protected override Microsoft.UI.Xaml.Controls.WebView2 CreatePlatformView()
    {
        var nativeWebView = base.CreatePlatformView();

        var serializer = Messenger.GetDefaultJsonSerializer();
        nativeWebView.WebMessageReceived += (s, e) =>
        {
            if (serializer.Deserialize(
                new StringReader(e.WebMessageAsJson),
                typeof(object))?.ToString() is { } m)
            {
                if (this.VirtualView is JavaScriptMultiplexedWebView webView)
                {
                    webView.SendToHostMessage(m);
                }
            }
        };

        ((JavaScriptMultiplexedWebView)base.VirtualView).SetInvokedJavaScriptListener(
            script => nativeWebView.ExecuteScriptAsync(script).AsTask());

        return nativeWebView;
    }
}
