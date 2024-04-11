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

using CefSharp;
using CefSharp.Wpf;
using Epoxy;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DupeNukem.ViewModels
{
    [ViewModel]   // PropChanged injection by Epoxy
    internal sealed partial class MainWindowViewModel
    {
        public Command Loaded { get; }

        public string? Url { get; private set; }

        public Pile<ChromiumWebBrowser> CefSharpPile { get; } =
            Pile.Factory.Create<ChromiumWebBrowser>();

        public MainWindowViewModel()
        {
            // Step 1: Construct DupeNukem Messenger.
            var messenger = new WebViewMessenger();
            HookWithMessengerTestCode(messenger); // FOR TEST
            // ----

            // MainWindow.Loaded:
            this.Loaded = Command.Factory.Create<EventArgs>(async _ =>
            {
                await this.CefSharpPile.RentAsync(cefSharp =>
                {
                    // Startup sequence.
                    // Bound between CefSharp and DupeNukem Messenger.

                    // Step 2: Hook up .NET --> JavaScript message handler.
                    messenger.SendRequest += async (s, e) =>
                    {
                        // Marshal to main thread.
                        if (await UIThread.TryBind())
                        {
                            cefSharp.BrowserCore.MainFrame.ExecuteJavaScriptAsync(
                                e.ToJavaScript());
                        }
                    };

                    // Step 3: Attached JavaScript --> .NET message handler.
                    cefSharp.JavascriptMessageReceived += (s, e) =>
                        messenger.ReceivedRequest(e.Message.ToString());

                    // Step 4: Injected Messenger script.
                    var script = messenger.GetInjectionScript(true);
                    AddJavaScriptTestCode(script); // FOR TEST
                    cefSharp.FrameLoadEnd += (s, e) =>
                    {
                        if (e.Frame.IsMain)
                        {
                            cefSharp.BrowserCore.MainFrame.ExecuteJavaScriptAsync(
                                script.ToString());
                        }
                    };

                    // Enable dev tools.
                    cefSharp.ShowDevTools();

                    // Register test objects.
                    this.RegisterTestObjects(messenger);

                    return default;
                });

                this.Url = "https://www.google.com/";
            });
        }
    }
}
