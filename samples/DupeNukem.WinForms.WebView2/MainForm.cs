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

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DupeNukem.WinForms.WebView2;

public partial class MainForm : Form
{
    private readonly WebViewMessenger messenger = new();

    public MainForm()
    {
        this.InitializeComponent();

        HookWithMessengerTestCode(this.messenger);   // FOR TEST
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
        AddJavaScriptTestCode(script);   // FOR TEST
        await this.webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            script.ToString());

        // Enable dev tools.
        this.webView2.CoreWebView2.OpenDevToolsWindow();

        // =========================================
        // Register an object:

        // name: `dupeNukem.winForms.webView2.calculator.add`, `dupeNukem.winForms.webView2.calculator.sub`
        var calculator = new Calculator();
        this.messenger.RegisterObject(calculator);

        // name: `calc.add`, `calc.sub`
        this.messenger.RegisterObject("calc", calculator);

        // ---- Or, register .NET side methods:

        // name: `dupeNukem.winForms.webView2.mainWindowViewModel.add`
        this.messenger.RegisterFunc<int, int, int>(this.Add);
        // name: `dupeNukem.winForms.webView2.mainWindowViewModel.sub`
        this.messenger.RegisterFunc<int, int, int>(this.Sub);
        // name: `dupeNukem.winForms.webView2.mainWindowViewModel.fromEnum`
        this.messenger.RegisterFunc<int, ConsoleKey>(this.FromEnum);
        // name: `dupeNukem.winForms.webView2.mainWindowViewModel.toEnum`
        this.messenger.RegisterFunc<ConsoleKey, int>(this.ToEnum);
        // name: `dupeNukem.winForms.webView2.mainWindowViewModel.toEnum`
        this.messenger.RegisterFunc<ConsoleKey[], ConsoleKey[]>(this.Array);

        // ---- Or, register directly delegate with method name.
        this.messenger.RegisterFunc<int, int, int>(
            "add",
            async (a, b) => { await Task.Delay(100); return a + b; });
        this.messenger.RegisterFunc<int, int, int>(
            "sub",
            async (a, b) => { await Task.Delay(100); return a - b; });
        this.messenger.RegisterFunc<int, ConsoleKey>(
            "fromEnum",
            async key => { await Task.Delay(100); return (int)key; });
        this.messenger.RegisterFunc<ConsoleKey, int>(
            "toEnum",
            async key => { await Task.Delay(100); return (ConsoleKey)key; });
        this.messenger.RegisterFunc<ConsoleKey[], ConsoleKey[]>(
            "array",
            async keys => { await Task.Delay(100); return keys; });

        this.webView2.Source = new Uri("https://www.google.com/");
    }

    /////////////////////////////////////////////////////////////////////////
    // For test

    public async Task<int> Add(int a, int b) { await Task.Delay(100); return a + b; }
    public async Task<int> Sub(int a, int b) { await Task.Delay(100); return a - b; }
    public async Task<int> FromEnum(ConsoleKey key) { await Task.Delay(100); return (int)key; }
    public async Task<ConsoleKey> ToEnum(int key) { await Task.Delay(100); return (ConsoleKey)key; }
    public async Task<ConsoleKey[]> Array(ConsoleKey[] keys) { await Task.Delay(100); return keys; }

    private static void HookWithMessengerTestCode(WebViewMessenger messenger)
    {
        // ---- Test code fragments: Will be invoke when Messenger script is loaded.
        messenger.Ready += async (s, e) =>
        {
            // Test JavaScript --> .NET methods:
            await messenger.InvokePeerMethodAsync("tester");

            // Invoke .NET --> JavaScript functions:
            var result_add = await messenger.InvokePeerMethodAsync<int>(
                "js_add", 1, 2);
            Trace.WriteLine($"js_add: {result_add}");
            var result_sub = await messenger.InvokePeerMethodAsync<int>(
                "js_sub", 1, 2);
            Trace.WriteLine($"js_sub: {result_sub}");
            var result_enum1 = await messenger.InvokePeerMethodAsync<ConsoleKey>(
                "js_enum1", ConsoleKey.Print);
            Trace.WriteLine($"js_enum1: {result_enum1}");
            var result_enum2 = await messenger.InvokePeerMethodAsync<ConsoleKey>(
                "js_enum2", ConsoleKey.Print);
            Trace.WriteLine($"js_enum2: {result_enum2}");
            var result_array = await messenger.InvokePeerMethodAsync<ConsoleKey[]>(
                "js_array", new[] { ConsoleKey.Print, ConsoleKey.Enter, ConsoleKey.Escape });
            Trace.WriteLine($"js_array: [{string.Join(",", result_array)}]");
            try
            {
                await messenger.InvokePeerMethodAsync("unknown");
                Trace.WriteLine("BUG detected. [unknown]");
            }
            catch (PeerInvocationException)
            {
                Trace.WriteLine("PASS: Unknown function invoking [unknown]");
            }

            Trace.WriteLine("ALL TEST IS DONE AT .NET SIDE.");
        };
    }

    private static void AddJavaScriptTestCode(StringBuilder script)
    {
        // ---- Added more JavaScript test code fragments:
        // You can verify on the developer tooling window,
        // trigger right click on the window and choice context menu,
        // then select console tab.
        script.AppendLine("async function js_add(a, b) { return a + b; }");
        script.AppendLine("async function js_sub(a, b) { return a - b; }");
        script.AppendLine("async function js_enum1(a) { console.log('js_enum1(' + a + ')'); return 'Print'; }");
        script.AppendLine("async function js_enum2(a) { console.log('js_enum2(' + a + ')'); return 42; }");
        script.AppendLine("async function js_array(a) { console.log('js_array(' + a + ')'); return ['Print', 13, 27]; }");

        // Invoke JavaScript --> .NET methods:
        script.AppendLine("var tester = async () => {");
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
        script.AppendLine("    console.log('BUG detected [unknown]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASS: Unknown method invoking [unknown]');");
        script.AppendLine("  }");

        script.AppendLine("  const result_fullName_calc_add = await invokeHostMethod('dupeNukem.winForms.webView2.calculator.add', 1, 2);");
        script.AppendLine("  console.log('fullName_calc.add: ' + result_fullName_calc_add);");

        script.AppendLine("  const result_fullName_calc_sub = await invokeHostMethod('dupeNukem.winForms.webView2.calculator.sub', 1, 2);");
        script.AppendLine("  console.log('fullName_calc.sub: ' + result_fullName_calc_sub);");

        script.AppendLine("  const result_calc_add = await invokeHostMethod('calc.add', 1, 2);");
        script.AppendLine("  console.log('calc.add: ' + result_calc_add);");

        script.AppendLine("  const result_calc_sub = await invokeHostMethod('calc.sub', 1, 2);");
        script.AppendLine("  console.log('calc.sub: ' + result_calc_sub);");

        script.AppendLine("  try {");
        script.AppendLine("    await invokeHostMethod('calc.mult', 1, 2);");
        script.AppendLine("    console.log('BUG detected [calc.mult]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASS: Unknown method invoking [calc.mult]');");
        script.AppendLine("  }");

        script.AppendLine("  const ct1 = new CancellationToken();");
        script.AppendLine("  const result_calc_add_cancellable1 = await invokeHostMethod('calc.add_cancellable', 1, 2, ct1);");
        script.AppendLine("  console.log('calc.add_cancellable1: ' + result_calc_add_cancellable1);");

        script.AppendLine("  const ct2 = new CancellationToken();");
        script.AppendLine("  const result_calc_add_cancellable2_p = invokeHostMethod('calc.add_cancellable', 1, 2, ct2);");
        script.AppendLine("  await delay(1000);");
        script.AppendLine("  ct2.cancel();");
        script.AppendLine("  try {");
        script.AppendLine("    await result_calc_add_cancellable2_p;");
        script.AppendLine("    console.log('BUG detected [calc.add_cancellable2]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASS: Operation canceled [calc.add_cancellable2]');");
        script.AppendLine("  }");

        script.AppendLine("  const result_fullName_proxy_calc_add = await dupeNukem.winForms.webView2.calculator.add(1, 2);");
        script.AppendLine("  console.log('fullName_proxy_calc.add: ' + result_fullName_proxy_calc_add);");

        script.AppendLine("  const result_proxy_calc_add = await calc.add(1, 2);");
        script.AppendLine("  console.log('proxy_calc.add: ' + result_proxy_calc_add);");

        script.AppendLine("  const result_calc_add_obsoleted1 = await calc.add_obsoleted1(1, 2);");
        script.AppendLine("  console.log('calc.add_obsoleted1: ' + result_calc_add_obsoleted1);");

        script.AppendLine("  const result_calc_add_obsoleted2 = await calc.add_obsoleted2(1, 2);");
        script.AppendLine("  console.log('calc.add_obsoleted2: ' + result_calc_add_obsoleted2);");

        script.AppendLine("  try {");
        script.AppendLine("    await calc.add_obsoleted3(1, 2);");
        script.AppendLine("    console.log('BUG detected [calc.add_obsoleted3]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASS: Fatal obsoleted [calc.add_obsoleted3]');");
        script.AppendLine("  }");

        script.AppendLine("  const result_calc_mul = await calc.mul(2, 3);");
        script.AppendLine("  console.log('calc.mul: ' + result_calc_mul);");

        script.AppendLine("  try {");
        script.AppendLine("    await calc.willBeThrow(1, 2);");
        script.AppendLine("    console.log('BUG detected [calc.willBeThrow, 1]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    if (e.props.a == 1 && e.props.b == 2)");
        script.AppendLine("      console.log('PASS: Will be throw [calc.willBeThrow]');");
        script.AppendLine("    else");
        script.AppendLine("      console.log('BUG detected [calc.willBeThrow, 2]');");
        script.AppendLine("  }");

        script.AppendLine("  console.log('ALL TEST IS DONE AT JavaScript SIDE.');");

        script.AppendLine("}");
        // ----
    }
}