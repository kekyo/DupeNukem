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

using Epoxy;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Xam.Plugin.WebView.Abstractions;
using Xamarin.Forms;
using Command = Epoxy.Command;

namespace DupeNukem.ViewModels
{
    [ViewModel]   // PropChanged injection by Epoxy
    internal sealed partial class ContentPageViewModel
    {
        public Command Ready { get; }

        public string? Url { get; private set; }

        public Pile<FormsWebView> WebViewPile { get; } =
            Pile.Factory.Create<FormsWebView>();

        public ContentPageViewModel()
        {
            // Step 1: Construct DupeNukem Messenger.
            var messenger = new WebViewMessenger();
            HookWithMessengerTestCode(messenger);   // FOR TEST
            // ----

            // ContentPage.Appearing:
            this.Ready = Command.Factory.Create<EventArgs>(async _ =>
            {
                await this.WebViewPile.RentAsync(formsWebView =>
                {
                    // Startup sequence.
                    // Bound between CefSharp and DupeNukem Messenger.

                    // Step 2: Hook up .NET --> JavaScript message handler.
                    messenger.SendRequest += async (s, e) =>
                    {
                        // Marshal to main thread.
                        if (await UIThread.TryBind())
                        {
                            await formsWebView.InjectJavascriptAsync(e.ToJavaScript());
                        }
                    };

                    // Step 3: Attached JavaScript --> .NET message handler.
                    formsWebView.AddLocalCallback(
                        WebViewMessenger.PostMessageSymbolName,
                        messenger.ReceivedRequest);

                    // Step 4: Injected Messenger script.
                    var script = messenger.GetInjectionScript(true);
                    AddJavaScriptTestCode(script);   // FOR TEST
                    formsWebView.OnNavigationCompleted += (s, url) =>
                    {
                        if (url == formsWebView.Source)
                        {
                            formsWebView.InjectJavascriptAsync(script.ToString());
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
}
