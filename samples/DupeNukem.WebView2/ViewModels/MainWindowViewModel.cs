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
using System.Linq;
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

            // ---- Test code fragments: Will be invoke when Messenger script is loaded.
            messenger.Ready += async (s, e) =>
            {
                // Invoke .NET --> JavaScript functions:
                var result_add = await messenger.InvokeClientFunctionAsync<int>(
                    "js_add", 1, 2);
                Trace.WriteLine($"js_add: {result_add}");
                var result_sub = await messenger.InvokeClientFunctionAsync<int>(
                    "js_sub", 1, 2);
                Trace.WriteLine($"js_sub: {result_sub}");
                var result_enum1 = await messenger.InvokeClientFunctionAsync<ConsoleKey>(
                    "js_enum1", ConsoleKey.Print);
                Trace.WriteLine($"js_enum1: {result_enum1}");
                var result_enum2 = await messenger.InvokeClientFunctionAsync<ConsoleKey>(
                    "js_enum2", ConsoleKey.Print);
                Trace.WriteLine($"js_enum2: {result_enum2}");
                var result_array = await messenger.InvokeClientFunctionAsync<ConsoleKey[]>(
                    "js_array", new[] { ConsoleKey.Print, ConsoleKey.Enter, ConsoleKey.Escape });
                Trace.WriteLine($"js_array: [{string.Join(",", result_array)}]");
                try
                {
                    await messenger.InvokeClientFunctionAsync("aaa");
                    Trace.WriteLine("BUG detected.");
                }
                catch (JavaScriptException)
                {
                    Trace.WriteLine("PASS: Unknown function invoking.");
                }
            };
            // ----

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
                    script.AppendLine("async function js_enum1(a) { console.log('js_enum1(' + a + ')'); return 'Print'; }");
                    script.AppendLine("async function js_enum2(a) { console.log('js_enum2(' + a + ')'); return 42; }");
                    script.AppendLine("async function js_array(a) { console.log('js_array(' + a + ')'); return ['Print', 13, 27]; }");
                    // Invoke JavaScript --> .NET methods:
                    script.AppendLine("(async function () {");
                    script.AppendLine("  const result_add = await invokeHostMethod('add', 1, 2);");
                    script.AppendLine("  console.log('add: ' + result_add);");
                    script.AppendLine("  const result_sub = await invokeHostMethod('sub', 1, 2);");
                    script.AppendLine("  console.log('sub: ' + result_sub);");
                    script.AppendLine("  const result_enum1 = await invokeHostMethod('fromEnum', 'Print');");
                    script.AppendLine("  console.log('enum1: ' + result_enum1);");
                    script.AppendLine("  const result_enum2 = await invokeHostMethod('fromEnum', 42);");
                    script.AppendLine("  console.log('enum2: ' + result_enum2);");
                    script.AppendLine("  const result_enum3 = await invokeHostMethod('toEnum', 42);");
                    script.AppendLine("  console.log('enum3: ' + result_enum3);");
                    script.AppendLine("  const result_array = await invokeHostMethod('array', [42, 13, 27]);");
                    script.AppendLine("  console.log('array: ' + result_array);");
                    script.AppendLine("  try {");
                    script.AppendLine("    await invokeHostMethod('unknown', 12, 34, 56);");
                    script.AppendLine("    console.log('BUG detected.');");
                    script.AppendLine("  } catch (e) {");
                    script.AppendLine("    console.log('PASS: Unknown method invoking.');");
                    script.AppendLine("  }");
                    script.AppendLine("})();");
                    // ----

                    await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                        script.ToString());

                    // Register .NET side methods:

                    // name: `DupeNukem.ViewModels.MainWindowViewModel.Add`
                    messenger.RegisterFunc<int, int, int>(this.Add);
                    // name: `DupeNukem.ViewModels.MainWindowViewModel.Sub`
                    messenger.RegisterFunc<int, int, int>(this.Sub);
                    // name: `DupeNukem.ViewModels.MainWindowViewModel.FromEnum`
                    messenger.RegisterFunc<int, ConsoleKey>(this.FromEnum);
                    // name: `DupeNukem.ViewModels.MainWindowViewModel.ToEnum`
                    messenger.RegisterFunc<ConsoleKey, int>(this.ToEnum);
                    // name: `DupeNukem.ViewModels.MainWindowViewModel.ToEnum`
                    messenger.RegisterFunc<ConsoleKey[], ConsoleKey[]>(this.Array);

                    // Or, register directly delegate with method name.
                    messenger.RegisterFunc<int, int, int>(
                        "add",
                        (a, b) => Task.FromResult(a + b));
                    messenger.RegisterFunc<int, int, int>(
                        "sub",
                        (a, b) => Task.FromResult(a - b));
                    messenger.RegisterFunc<int, ConsoleKey>(
                        "fromEnum",
                        key => Task.FromResult((int)key));
                    messenger.RegisterFunc<ConsoleKey, int> (
                        "toEnum",
                        key => Task.FromResult((ConsoleKey)key));
                    messenger.RegisterFunc<ConsoleKey[], ConsoleKey[]> (
                        "array",
                        keys => Task.FromResult(keys));
                });
            });
        }

        public Task<int> Add(int a, int b) =>
            Task.FromResult(a + b);
        public Task<int> Sub(int a, int b) =>
            Task.FromResult(a - b);
        public Task<int> FromEnum(ConsoleKey key) =>
            Task.FromResult((int)key);
        public Task<ConsoleKey> ToEnum(int key) =>
            Task.FromResult((ConsoleKey)key);
        public Task<ConsoleKey[]> Array(ConsoleKey[] keys) =>
            Task.FromResult(keys);
    }
}
