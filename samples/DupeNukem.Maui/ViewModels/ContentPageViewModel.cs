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
using Epoxy;
using Microsoft.Maui.Controls;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

using Command = Epoxy.Command;

namespace DupeNukem.ViewModels;

[ViewModel]   // PropChanged injection by Epoxy
internal sealed partial class ContentPageViewModel
{
    public Command Ready { get; }

    public string? Url { get; private set; }

    public Pile<JavaScriptMultiplexedWebView> WebViewPile { get; } =
        Pile.Factory.Create<JavaScriptMultiplexedWebView>();

    public ContentPageViewModel()
    {
        // Step 1: Construct DupeNukem Messenger.
        var messenger = new WebViewMessenger();
        HookWithMessengerTestCode(messenger);   // FOR TEST
        // ----

        // ContentPage.Appearing:
        this.Ready = Command.Factory.Create(async () =>
        {
            await this.WebViewPile.RentAsync(webView =>
            {
                // Startup sequence.
                // Bound between MAUI WebView and DupeNukem Messenger.

                // Step 2: Hook up .NET --> JavaScript message handler.
                messenger.SendRequest += (s, e) =>
                    webView.InvokeJavaScriptAsync(e.ToJavaScript());

                // Step 3: Attached JavaScript --> .NET message handler.
                // DIRTY: JavaScriptMultiplexedWebView:
                //   MAUI does not have a platform-neutral event interface,
                //   so must implement MAUI handlers on each platform...
                //   In this project, examples for Android and Windows are provided.
                //   For more information, search for JavaScriptMultiplexedWebView.
                webView.MessageReceived += (s, e) =>
                    messenger.ReceivedRequest(e.Message);

                // Step 4: Injected Messenger script.
                var script = messenger.GetInjectionScript(true);
                AddJavaScriptTestCode(script);   // FOR TEST
                webView.Navigated += (s, e) =>
                {
                    if (e.Source is UrlWebViewSource eu &&
                        webView.Source is UrlWebViewSource wu &&
                        eu.Url == wu.Url)
                    {
                        webView.InvokeJavaScriptAsync(script.ToString());
                    }
                };

                // Register test objects.
                this.RegisterTestObjects(messenger);

                return default;
            });

            this.Url = "https://www.google.com/";
        });
    }
}
