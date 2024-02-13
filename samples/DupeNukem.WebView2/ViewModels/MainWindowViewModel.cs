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

// This code is a ViewModel, and the initialization code contains a sample implementation of the glue code
// that ties together DupeNukem's Messenger and WebView.
// All sample codes use a common test implementation/test script,
// which are implemented in the `TestModel` class of the `DupeNukem.Sample.Common` project.
// By comparing the implementation of each platform,
// we have made it easier to understand the elements required for glue code.

// Epoxy is used to simplify the ViewModel implementation.

using DupeNukem.Models;
using Epoxy;
using System;
using System.IO;

namespace DupeNukem.ViewModels;

[ViewModel]   // PropChanged injection by Epoxy
internal sealed class MainWindowViewModel
{
    public Command Loaded { get; }

    public Uri? Url { get; private set; }

    public Pile<Microsoft.Web.WebView2.Wpf.WebView2> WebView2Pile { get; } =
        Pile.Factory.Create<Microsoft.Web.WebView2.Wpf.WebView2>();

    public MainWindowViewModel()
    {
        // Step 1: Construct DupeNukem Messenger.
        var messenger = new WebViewMessenger();

        // FOR TEST: Initialize tester model.
        var test = new TestModel();
        test.RegisterTestObjects(messenger);

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
                TestModel.AddTestJavaScriptCode(script);   // FOR TEST
                await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    script.ToString());

                // Enable dev tools.
                webView2.CoreWebView2.OpenDevToolsWindow();
            });

            this.Url = new Uri("https://www.google.com/");
        });
    }
}
