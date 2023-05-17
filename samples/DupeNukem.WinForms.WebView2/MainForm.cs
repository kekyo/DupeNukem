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

// This initialization code contains a sample implementation of the glue code
// that ties together DupeNukem's Messenger and WebView.
// All sample codes use a common test implementation/test script,
// which are implemented in the `TestModel` class of the `DupeNukem.Sample.Common` project.
// By comparing the implementation of each platform,
// we have made it easier to understand the elements required for glue code.

using DupeNukem.Models;
using System;
using System.IO;
using System.Windows.Forms;

namespace DupeNukem.WinForms.WebView2;

public partial class MainForm : Form
{
    private readonly WebViewMessenger messenger = new();

    public MainForm()
    {
        this.InitializeComponent();

        // FOR TEST: Initialize tester model.
        var test = new TestModel();
        test.RegisterTestObjects(this.messenger);
    }

    /////////////////////////////////////////////////////////////////////////

    private async void MainForm_Load(object sender, EventArgs e)
    {
        // Startup sequence.
        // Bound between WebView2 and DupeNukem Messenger.

        // Initialize WebView2.
        await this.webView2.EnsureCoreWebView2Async();

        // Step 2: Hook up .NET --> JavaScript message handler.
        this.messenger.SendRequest += (s, e) =>
        {
            try
            {
                // Marshal to main thread.
                this.Invoke(() => this.webView2.CoreWebView2.PostWebMessageAsString(e.JsonString));
            }
            // Ignore all exception, because may cause it on application shutdown sequence.
            catch
            {
            }
        };

        // Step 3: Attached JavaScript --> .NET message handler.
        var serializer = Messenger.GetDefaultJsonSerializer();
        this.webView2.CoreWebView2.WebMessageReceived += (s, e) =>
        {
            if (serializer.Deserialize(
                new StringReader(e.WebMessageAsJson),
                typeof(object))?.ToString() is { } m)
            {
                this.messenger.ReceivedRequest(m);
            }
        };

        // Step 4: Injected Messenger script.
        var script = this.messenger.GetInjectionScript(true);
        TestModel.AddTestJavaScriptCode(script);   // FOR TEST
        await this.webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            script.ToString());

        // Enable dev tools.
        this.webView2.CoreWebView2.OpenDevToolsWindow();

        this.webView2.Source = new Uri("https://www.google.com/");
    }
}