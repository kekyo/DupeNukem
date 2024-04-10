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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DupeNukem.ViewModels;

[ViewModel]   // PropChanged injection by Epoxy
internal sealed partial class MainWindowViewModel
{
    public Command Loaded { get; }

    public Uri? Url { get; private set; }

    public Pile<Microsoft.Web.WebView2.Wpf.WebView2> WebView2Pile { get; } =
        Pile.Factory.Create<Microsoft.Web.WebView2.Wpf.WebView2>();

    public MainWindowViewModel()
    {
        // Step 1: Construct DupeNukem Messenger.
        var messenger = new WebViewMessenger();
        HookWithMessengerTestCode(messenger);   // FOR TEST
        // ----

        // MainWindow.Loaded:
        this.Loaded = Command.Factory.Create<EventArgs>(async _ =>
        {
            await this.WebView2Pile.RentAsync(async webView2 =>
            {
                // Startup sequence.
                // Bound between WebView2 and DupeNukem Messenger.

                // Initialize WebView2.
                await webView2.EnsureCoreWebView2Async();

                // Step 2: Hook up .NET --> JavaScript message handler.
                messenger.SendRequest += async (s, e) =>
                {
                    // Marshal to main thread.
                    if (await UIThread.TryBind())
                    {
                        webView2.CoreWebView2.PostWebMessageAsString(e.JsonString);
                    }
                };

                // Step 3: Attached JavaScript --> .NET message handler.
                var serializer = Messenger.GetDefaultJsonSerializer();
                webView2.CoreWebView2.WebMessageReceived += (s, e) =>
                {
                    if (serializer.Deserialize(
                        new StringReader(e.WebMessageAsJson),
                        typeof(object))?.ToString() is { } m)
                    {
                        messenger.ReceivedRequest(m);
                    }
                };

                // Step 4: Injected Messenger script.
                var script = messenger.GetInjectionScript(true);
                AddJavaScriptTestCode(script);   // FOR TEST
                await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    script.ToString());

                // Enable dev tools.
                webView2.CoreWebView2.OpenDevToolsWindow();

                // Register test objects.
                this.RegisterTestObjects(messenger);
            });

            this.Url = new Uri("https://www.google.com/");
        });
    }
}
