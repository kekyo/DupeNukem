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
using Xam.Plugin.WebView.Abstractions;
using Xamarin.Forms;

using Command = Epoxy.Command;

namespace DupeNukem.ViewModels
{
    [ViewModel]   // PropChanged injection by Epoxy
    internal sealed class ContentPageViewModel
    {
        public Command Ready { get; }

        public string? Url { get; private set; }

        public Pile<FormsWebView> WebViewPile { get; } =
            Pile.Factory.Create<FormsWebView>();

        public ContentPageViewModel()
        {
            // Step 1: Construct DupeNukem Messenger.
            var messenger = new WebViewMessenger();

            // FOR TEST: Initialize tester model.
            var test = new TestModel();
            test.RegisterTestObjects(messenger);

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
                            Application.Current.Dispatcher.BeginInvokeOnMainThread(() =>
                                formsWebView.InjectJavascriptAsync(e.ToJavaScript()));
                        }
                    };

                    // Step 3: Attached JavaScript --> .NET message handler.
                    formsWebView.AddLocalCallback(
                        messenger.PostMessageSymbolName,
                        messenger.ReceivedRequest);

                    // Step 4: Injected Messenger script.
                    var script = messenger.GetInjectionScript(true);
                    TestModel.AddTestJavaScriptCode(script);   // FOR TEST
                    formsWebView.OnNavigationCompleted += (s, url) =>
                    {
                        if (url == formsWebView.Source)
                        {
                            formsWebView.InjectJavascriptAsync(script.ToString());
                        }
                    };

                    return default;
                });

                this.Url = "https://www.google.com/";
            });
        }
    }
}
