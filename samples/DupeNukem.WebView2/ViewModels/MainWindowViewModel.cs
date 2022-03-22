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
using System.Threading.Tasks;

namespace DupeNukem.ViewModels
{
    [ViewModel]   // PropChanged injection by Epoxy
    internal sealed class MainWindowViewModel
    {
        public Command Loaded { get; }

        public Uri? Url { get; private set; }

        public Pile<Microsoft.Web.WebView2.Wpf.WebView2> WebView2Pile { get; } =
            PileFactory.Create<Microsoft.Web.WebView2.Wpf.WebView2>();

        public MainWindowViewModel()
        {
            // Step 1: Construct DupeNukem Messenger.
            var messenger = new Messenger();

            // TEST CODE: Will be invoke when Messenger script is loaded.
            messenger.Ready += async (s, e) =>
            {
                // Invoke JavaScript functions:
                var result_add = await messenger.InvokeClientFunctionAsync<int>(
                    "js_add", 1, 2);
                Trace.WriteLine($"js_add: {result_add}");
                var result_sub = await messenger.InvokeClientFunctionAsync<int>(
                    "js_sub", 1, 2);
                Trace.WriteLine($"js_sub: {result_sub}");
            };

            // MainWindow.Loaded:
            this.Loaded = CommandFactory.Create<EventArgs>(async _ =>
            {
                this.Url = new Uri("https://www.google.com/");

                await this.WebView2Pile.RentAsync(async webView2 =>
                {
                    // Startup sequence.
                    // Bound both WebView2 and DupeNukem Messenger.

                    // Initialize WebView2.
                    await webView2.EnsureCoreWebView2Async();

                    // Step 2: Hook up .NET --> JavaScript message handler.
                    messenger.SendRequest += async (s, e) =>
                    {
                        await UIThread.Bind();   // Marshaling into UI thread (by Epoxy)
                        webView2.CoreWebView2.PostWebMessageAsString(e.JsonString);
                    };

                    // Step 3: Attached JavaScript --> .NET message handler.
                    webView2.CoreWebView2.WebMessageReceived += (s, e) =>
                        messenger.ReceivedRequest(e.TryGetWebMessageAsString());

                    // Step 4: Injected Messenger script.
                    var script = messenger.GetInjectionScript();

                    // ---- Added more JavaScript test code fragments:
                    script.AppendLine("async function js_add(a, b) { return a + b; }");
                    script.AppendLine("async function js_sub(a, b) { return a - b; }");
                    script.AppendLine("(async function () {");
                    script.AppendLine("  const result_add = await invokeHostMethod('add', 1, 2);");
                    script.AppendLine("  console.log('add: ' + result_add);");
                    script.AppendLine("  const result_sub = await invokeHostMethod('sub', 1, 2);");
                    script.AppendLine("  console.log('sub: ' + result_sub);");
                    script.AppendLine("})();");
                    // ----

                    await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                        script.ToString());

                    // Register .NET side methods:

                    // name: `DupeNukem.ViewModels.MainWindowViewModel.Add`
                    messenger.RegisterFunc<int, int, int>(this.Add);
                    // name: `DupeNukem.ViewModels.MainWindowViewModel.Sub`
                    messenger.RegisterFunc<int, int, int>(this.Sub);

                    // Or, register directly delegate with method name.
                    messenger.RegisterFunc<int, int, int>(
                        "add",
                        (a, b) => Task.FromResult(a + b));
                    messenger.RegisterFunc<int, int, int>(
                        "sub",
                        (a, b) => Task.FromResult(a - b));
                });
            });
        }

        public Task<int> Add(int a, int b) =>
            Task.FromResult(a + b);
        public Task<int> Sub(int a, int b) =>
            Task.FromResult(a - b);
    }
}
