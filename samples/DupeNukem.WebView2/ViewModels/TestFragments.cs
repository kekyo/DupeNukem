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
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

////////////////////////////////////////////////////////////////////////////////////////
// For testing commons (Edge2 [WPF/Windows Forms], CefSharp, Xamarin Forms and MAUI)
    
#if WINDOWS_FORMS
namespace DupeNukem.WinForms.WebView2;
partial class MainForm
#elif XAMARIN_FORMS || MAUI
namespace DupeNukem.ViewModels;
partial class ContentPageViewModel
#else
namespace DupeNukem.ViewModels;

partial class MainWindowViewModel
#endif
{
    public sealed class CustomType
    {
        public int Value1 = 0;
        public string Value2 = null!;
        public CustomType Value3 = null!;
        public JsonElement Value4 = null!;

        public override bool Equals(object? rhs) =>
            rhs is CustomType r &&
            this.Value1.Equals(r.Value1) &&
            (this.Value2 == r.Value2) &&
            (this.Value3?.Equals(r.Value3) ?? r.Value3 == null) &&
            (this.Value4?.Equals(r.Value4) ?? r.Value4 == null);

        public override int GetHashCode() =>
            this.Value1.GetHashCode() ^
            (this.Value2?.GetHashCode() ?? 0) ^
            (this.Value3?.GetHashCode() ?? 0) ^
            (this.Value4?.GetHashCode() ?? 0);
    }

    private void RegisterTestObjects(WebViewMessenger messenger)
    {
        // ================================================
        // Register some objects for testing purpose:

        // name: `dupeNukem.viewModels.calculator.add`, `dupeNukem.viewModels.calculator.sub`
        var calculator = new Calculator();
        messenger.RegisterObject(calculator);

        // name: `calc.add`, `calc.sub`
        messenger.RegisterObject("calc", calculator);

        // ---- Or, register .NET side methods:

        // name: `dupeNukem.viewModels.mainWindowViewModel.add`
        messenger.RegisterFunc<int, int, int>(this.Add);
        // name: `dupeNukem.viewModels.mainWindowViewModel.sub`
        messenger.RegisterFunc<int, int, int>(this.Sub);
        // name: `dupeNukem.viewModels.mainWindowViewModel.fromEnum`
        messenger.RegisterFunc<int, ConsoleKey>(this.FromEnum);
        // name: `dupeNukem.viewModels.mainWindowViewModel.toEnum`
        messenger.RegisterFunc<ConsoleKey, int>(this.ToEnum);
        // name: `dupeNukem.viewModels.mainWindowViewModel.toEnum`
        messenger.RegisterFunc<ConsoleKey[], ConsoleKey[]>(this.Array);

        // ---- Or, register directly delegate with method name.
        messenger.RegisterFunc<int, int, int>(
            "add",
            async (a, b) => { await Task.Delay(100); return a + b; });
        messenger.RegisterFunc<int, int, int>(
            "sub",
            async (a, b) => { await Task.Delay(100); return a - b; });
        messenger.RegisterFunc<int, ConsoleKey>(
            "fromEnum",
            async key => { await Task.Delay(100); return (int)key; });
        messenger.RegisterFunc<ConsoleKey, int>(
            "toEnum",
            async key => { await Task.Delay(100); return (ConsoleKey)key; });
        messenger.RegisterFunc<ConsoleKey[], ConsoleKey[]>(
            "array",
            async keys => { await Task.Delay(100); return keys; });
        messenger.RegisterFunc<byte[], byte[]>(
            "arrayBuffer",
            async arr => { await Task.Delay(100); return arr; });
        messenger.RegisterFunc<int, int, int, Func<int, int, Task<int>>>(
            "callback",
            async (a, b, cb) => { var r = await cb(a, b); return r; });
        messenger.RegisterFunc<int, int, int, Func<int, int, CancellationToken, Task<int>>>(
            "callback2",
            async (a, b, cb) => { var r = await cb(a, b, default); return r; });
        messenger.RegisterFunc<CustomType, CustomType>(
            "customType",
            async ct => { await Task.Delay(100); return ct; });
    }

    /////////////////////////////////////////////////////////
    // Some testing callable target methods:

    public async Task<int> Add(int a, int b) { await Task.Delay(100); return a + b; }
    public async Task<int> Sub(int a, int b) { await Task.Delay(100); return a - b; }
    public async Task<int> FromEnum(ConsoleKey key) { await Task.Delay(100); return (int)key; }
    public async Task<ConsoleKey> ToEnum(int key) { await Task.Delay(100); return (ConsoleKey)key; }
    public async Task<ConsoleKey[]> Array(ConsoleKey[] keys) { await Task.Delay(100); return keys; }

    private static void Assert(object expected, object actual, string message)
    {
        if (expected.Equals(actual))
        {
            Trace.WriteLine($"PASSED: {message}");
        }
        else if (expected is IEnumerable ex && actual is IEnumerable ac &&
            ex.Cast<object>().SequenceEqual(ac.Cast<object>()))
        {
            Trace.WriteLine($"PASSED: {message}");
        }
        else
        {
            Trace.Fail($"FAILED: {message}");
        }
    }

    private static void HookWithMessengerTestCode(WebViewMessenger messenger)
    {
        /////////////////////////////////////////////////////////
        // Test code fragments: Will be invoke when Messenger script is loaded.
        
        messenger.Ready += async (s, e) =>
        {
            // Test JavaScript --> .NET methods:
            await messenger.InvokePeerMethodAsync("tester");

            /////////////////////////////////////////////////////////
            // Invoke .NET --> JavaScript functions:

            var result_add = await messenger.InvokePeerMethodAsync<int>(
                "js_add", 1, 2);
            Assert(3, result_add, $"js_add: {result_add}");

            var result_sub = await messenger.InvokePeerMethodAsync<int>(
                "js_sub", 1, 2);
            Assert(-1, result_sub, $"js_sub: {result_sub}");

            var result_enum1 = await messenger.InvokePeerMethodAsync<ConsoleKey>(
                "js_enum1", ConsoleKey.Print);
            Assert(ConsoleKey.Print, result_enum1, $"js_enum1: {result_enum1}");

            var result_enum2 = await messenger.InvokePeerMethodAsync<ConsoleKey>(
                "js_enum2", ConsoleKey.Print);
            Assert((ConsoleKey)42, result_enum2, $"js_enum2: {result_enum2}");

            var result_array = await messenger.InvokePeerMethodAsync<ConsoleKey[]>(
                "js_array", new[] { ConsoleKey.Print, ConsoleKey.Enter, ConsoleKey.Escape });
            Assert(new[] { ConsoleKey.Print, ConsoleKey.Enter, ConsoleKey.Escape }, result_array, $"js_array: {result_array}");

            try
            {
                await messenger.InvokePeerMethodAsync("unknown");
                Trace.Fail("FAILED: [unknown]");
            }
            catch (PeerInvocationException)
            {
                Trace.WriteLine("PASSED: Unknown function invoking [unknown]");
            }

            var result_custonType1 = await messenger.InvokePeerMethodAsync<CustomType>(
                "js_customType",
                new CustomType
                {
                    Value1 = 123,
                    Value2 = "ABC",
                    Value3 = new CustomType
                    {
                        Value1 = 456,
                        Value2 = "DEF",
                        Value4 = 111,
                    },
                    Value4 = JsonElement.FromObject(new CustomType
                    {
                        Value1 = 789,
                        Value2 = "GHI",
                        Value4 = JsonElement.FromObject(new CustomType
                        {
                            Value1 = 999,
                            Value2 = "XXX",
                            Value4 = 222,
                        }),
                    }),
                });
            Assert(new CustomType
            {
                Value1 = 123,
                Value2 = "ABC",
                Value3 = new CustomType
                {
                    Value1 = 456,
                    Value2 = "DEF",
                    Value4 = 111,
                },
                Value4 = JsonElement.FromObject(new CustomType
                {
                    Value1 = 789,
                    Value2 = "GHI",
                    Value4 = JsonElement.FromObject(new CustomType
                    {
                        Value1 = 999,
                        Value2 = "XXX",
                        Value4 = 222,
                    }),
                }),
            }, result_custonType1, "js_customType");

            /////////////////////////////////////////////////////////
            // Test JavaScript --> .NET methods with callback

            Func<int, int, Task<int>> callback = (a, b) =>
                Task.FromResult(a + b);
            var result_callback = await messenger.InvokePeerMethodAsync<int>("js_callback", 1, 2, callback);
            Assert(3, result_callback, $"js_callback: {result_callback}");
            
            Func<int, int, CancellationToken, Task<int>> callback2 = (a, b, ct) =>
                Task.FromResult(a - b);
            var result_callback2 = await messenger.InvokePeerMethodAsync<int>("js_callback2", 1, 2, callback2);
            Assert(-1, result_callback2, $"js_callback2: {result_callback2}");
            
            Func<int, int, CancellationToken, Task<int>> callback3 = (a, b, ct) =>
                Task.FromResult(a * b);
            var result_callback3 = await messenger.InvokePeerMethodAsync<int>("js_callback3", 1, 2, callback3);
            Assert(2, result_callback3, $"js_callback3: {result_callback3}");

            Trace.WriteLine("ALL TEST IS DONE AT .NET SIDE.");
        };
    }

    private static void AddJavaScriptTestCode(StringBuilder script)
    {
        script.AppendLine("function assert(expected, actual, name) {");
        script.AppendLine("  if (expected == actual) {");
        script.AppendLine("    console.log('PASSED: ' + name);");
        script.AppendLine("  } else if (expected instanceof Array && actual instanceof Array &&");
        script.AppendLine("    expected.length == actual.length &&");
        script.AppendLine("    expected.every((v,i)=> v === actual[i])) {");
        script.AppendLine("    console.log('PASSED: ' + name);");
        script.AppendLine("  } else {");
        script.AppendLine("    console.error('FAILED: ' + name + ': ' + expected + ' != ' + actual);");
        script.AppendLine("  }");
        script.AppendLine("};");

        /////////////////////////////////////////////////////////
        // ---- Added more JavaScript test code fragments:
        // You can verify on the developer tooling window,
        // trigger right click on the window and choice context menu,
        // then select console tab.
        script.AppendLine("async function js_add(a, b) { return a + b; }");
        script.AppendLine("async function js_sub(a, b) { return a - b; }");
        script.AppendLine("async function js_enum1(a) { console.log('js_enum1(' + a + ')'); return 'Print'; }");
        script.AppendLine("async function js_enum2(a) { console.log('js_enum2(' + a + ')'); return 42; }");
        script.AppendLine("async function js_array(a) { console.log('js_array(' + a + ')'); return ['Print', 13, 27]; }");
        script.AppendLine("async function js_callback(a, b, cb) { return await cb(a, b); }");
        script.AppendLine("async function js_callback2(a, b, cb) { return await cb(a, b, new CancellationToken()); }");
        script.AppendLine("async function js_callback3(a, b, cb) { return await cb(a, b, new AbortController().signal); }");
        script.AppendLine("async function js_arrayBuffer1(arr) { console.log('js_arrayBuffer1(' + arr + ')'); return arr; }");
        script.AppendLine("async function js_arrayBuffer2(arr) { console.log('js_arrayBuffer2(' + arr + ')'); return new Uint8Array(arr); }");
        script.AppendLine("async function js_customType(ct) { console.log('js_customType(' + ct + ')'); return ct; }");

        /////////////////////////////////////////////////////////
        // Invoke JavaScript --> .NET methods:

        script.AppendLine("var tester = async () => {");
        
        // Low-level invoking with `invokeHostMethod()` and varies of common types.
        script.AppendLine("  const result_add = await invokeHostMethod('add', 1, 2);");
        script.AppendLine("  assert(3, result_add, 'add');");

        script.AppendLine("  const result_sub = await invokeHostMethod('sub', 1, 2);");
        script.AppendLine("  assert(-1, result_sub, 'sub');");

        script.AppendLine("  const result_enum1 = await invokeHostMethod('fromEnum', 'Print');");
        script.AppendLine("  assert(42, result_enum1, 'enum1');");

        script.AppendLine("  const result_enum2 = await invokeHostMethod('fromEnum', 42);");
        script.AppendLine("  assert(42, result_enum2, 'enum2');");

        script.AppendLine("  const result_enum3 = await invokeHostMethod('toEnum', 42);");
        script.AppendLine("  assert('print', result_enum3, 'enum3');");

        script.AppendLine("  const result_array = await invokeHostMethod('array', [42, 13, 27]);");
        script.AppendLine("  assert(['print','enter','escape'], result_array, 'array');");

        script.AppendLine("  const arg_arrayBuffer = new Uint8Array(5);");
        script.AppendLine("  arg_arrayBuffer[0] = 1;");
        script.AppendLine("  arg_arrayBuffer[1] = 2;");
        script.AppendLine("  arg_arrayBuffer[2] = 3;");
        script.AppendLine("  arg_arrayBuffer[3] = 4;");
        script.AppendLine("  arg_arrayBuffer[4] = 5;");
        script.AppendLine("  const result_arrayBuffer1 = await invokeHostMethod('arrayBuffer', arg_arrayBuffer.buffer);");
        script.AppendLine("  assert('1,2,3,4,5', new Uint8Array(result_arrayBuffer1).toString(), 'arrayBuffer1');");

        script.AppendLine("  const result_arrayBuffer2 = await invokeHostMethod('arrayBuffer', arg_arrayBuffer);");
        script.AppendLine("  assert('1,2,3,4,5', new Uint8Array(result_arrayBuffer2).toString(), 'arrayBuffer2');");

        script.AppendLine("  const result_callback = await invokeHostMethod('callback', 1, 2, async (a, b) => a - b);");
        script.AppendLine("  assert(-1, result_callback, 'callback');");

        script.AppendLine("  const result_callback2 = await invokeHostMethod('callback2', 1, 2, async (a, b, ct) => a + b);");
        script.AppendLine("  assert(3, result_callback2, 'callback2');");

        script.AppendLine("  const result_customType1 = await invokeHostMethod('customType', { value1:123, value2: 'ABC', value3: { value1: 456, value2: 'DEF', value4: 111 }, value4: { value1: 789, value2: 'GHI', value4: { value1: 999, value2: 'XXX', value4: 222 } } });");
        script.AppendLine("  assert(JSON.stringify({ value1:123, value2: 'ABC', value3: { value1: 456, value2: 'DEF', value3: null, value4: 111 }, value4: { value1: 789, value2: 'GHI', value4: { value1: 999, value2: 'XXX', value4: 222 } } }), JSON.stringify(result_customType1), 'customType1');");

        // Unknown method with `invokeHostMethod()`.
        script.AppendLine("  try {");
        script.AppendLine("    await invokeHostMethod('unknown', 12, 34, 56);");
        script.AppendLine("    console.error('FAILED: [unknown]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASSED: Unknown method invoking [unknown]');");
        script.AppendLine("  }");

#if WINDOWS_FORMS
        // Fully qualified object/function naming with `invokeHostMethod()`.
        script.AppendLine("  const result_fullName_calc_add = await invokeHostMethod('dupeNukem.winForms.webView2.calculator.add', 1, 2);");
        script.AppendLine("  assert(3, result_fullName_calc_add, 'fullName_calc.add');");

        // Able to call different fully qualified function on an object with `invokeHostMethod()`.
        script.AppendLine("  const result_fullName_calc_sub = await invokeHostMethod('dupeNukem.winForms.webView2.calculator.sub', 1, 2);");
        script.AppendLine("  assert(-1, result_fullName_calc_sub, 'fullName_calc.sub');");
#else
        // Fully qualified object/function naming with `invokeHostMethod()`.
        script.AppendLine("  const result_fullName_calc_add = await invokeHostMethod('dupeNukem.viewModels.calculator.add', 1, 2);");
        script.AppendLine("  assert(3, result_fullName_calc_add, 'fullName_calc.add');");

        // Able to call different fully qualified function on an object with `invokeHostMethod()`.
        script.AppendLine("  const result_fullName_calc_sub = await invokeHostMethod('dupeNukem.viewModels.calculator.sub', 1, 2);");
        script.AppendLine("  assert(-1, result_fullName_calc_sub, 'fullName_calc.sub');");
#endif

        // An object/function naming with `invokeHostMethod()`.
        script.AppendLine("  const result_calc_add = await invokeHostMethod('calc.add', 1, 2);");
        script.AppendLine("  assert(3, result_calc_add, 'calc.add');");

        // Able to call different function on an object with `invokeHostMethod()`.
        script.AppendLine("  const result_calc_sub = await invokeHostMethod('calc.sub', 1, 2);");
        script.AppendLine("  assert(-1, result_calc_sub, 'calc.sub');");

        // Unknown method on a object.
        script.AppendLine("  try {");
        script.AppendLine("    await invokeHostMethod('calc.mult', 1, 2);");
        script.AppendLine("    console.error('FAILED: [calc.mult]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASSED: Unknown method invoking [calc.mult]');");
        script.AppendLine("  }");

        // CancellationToken (obsoleted)
        script.AppendLine("  const ct1 = new CancellationToken();");
        script.AppendLine("  const result_calc_add_cancellable1 = await invokeHostMethod('calc.add_cancellable', 1, 2, ct1);");
        script.AppendLine("  assert(3, result_calc_add_cancellable1, 'calc.add_cancellable1');");

        script.AppendLine("  const ct2 = new CancellationToken();");
        script.AppendLine("  const result_calc_add_cancellable2_p = invokeHostMethod('calc.add_cancellable', 1, 2, ct2);");
        script.AppendLine("  await delay(1000);");
        script.AppendLine("  ct2.cancel();");
        script.AppendLine("  try {");
        script.AppendLine("    await result_calc_add_cancellable2_p;");
        script.AppendLine("    console.error('FAILED: [calc.add_cancellable2]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASSED: Operation canceled [calc.add_cancellable2]');");
        script.AppendLine("  }");

        // AbortController/AbortSignal
        script.AppendLine("  const ac1 = new AbortController();");
        script.AppendLine("  const result_calc_add_cancellable1_as = await invokeHostMethod('calc.add_cancellable', 1, 2, ac1.signal);");
        script.AppendLine("  assert(3, result_calc_add_cancellable1_as, 'calc.add_cancellable1_as');");

        script.AppendLine("  const ac2 = new AbortController();");
        script.AppendLine("  const result_calc_add_cancellable2_p_as = invokeHostMethod('calc.add_cancellable', 1, 2, ac2.signal);");
        script.AppendLine("  await delay(1000);");
        script.AppendLine("  ac2.abort();");
        script.AppendLine("  try {");
        script.AppendLine("    await result_calc_add_cancellable2_p_as;");
        script.AppendLine("    console.error('FAILED: [calc.add_cancellable2_as]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASSED: Operation canceled [calc.add_cancellable2_as]');");
        script.AppendLine("  }");

        // AbortController/AbortSignal (nested)
        script.AppendLine("  const ac3 = new AbortController();");
        script.AppendLine("  const result_calc_nested_cancellable = await invokeHostMethod('calc.nested_cancellable', { a: 1, b: 2, ct: ac3.signal, });");
        script.AppendLine("  assert(3, result_calc_nested_cancellable, 'calc.nested_cancellable');");

        script.AppendLine("  const ac4 = new AbortController();");
        script.AppendLine("  const result_calc_nested_cancellable_p = invokeHostMethod('calc.nested_cancellable', { a: 1, b: 2, ct: ac4.signal, });");
        script.AppendLine("  await delay(1000);");
        script.AppendLine("  ac4.abort();");
        script.AppendLine("  try {");
        script.AppendLine("    await result_calc_nested_cancellable_p;");
        script.AppendLine("    console.error('FAILED: [calc.nested_cancellable_p]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASSED: Operation canceled [calc.nested_cancellable_p]');");
        script.AppendLine("  }");

#if WINDOWS_FORMS
        // Fully qualified proxy object/function naming.
        script.AppendLine("  const result_fullName_proxy_calc_add = await dupeNukem.winForms.webView2.calculator.add(1, 2);");
        script.AppendLine("  assert(3, result_fullName_proxy_calc_add, 'fullName_proxy_calc.add');");
#else
        // Fully qualified proxy object/function naming.
        script.AppendLine("  const result_fullName_proxy_calc_add = await dupeNukem.viewModels.calculator.add(1, 2);");
        script.AppendLine("  assert(3, result_fullName_proxy_calc_add, 'fullName_proxy_calc.add');");
#endif
        // Proxy object/function.
        script.AppendLine("  const result_proxy_calc_add = await calc.add(1, 2);");
        script.AppendLine("  assert(3, result_proxy_calc_add, 'proxy_calc.add');");

        // Obsoleted marking.
        script.AppendLine("  const result_calc_add_obsoleted1 = await calc.add_obsoleted1(1, 2);");
        script.AppendLine("  assert(3, result_calc_add_obsoleted1, 'calc.add_obsoleted1');");

        script.AppendLine("  const result_calc_add_obsoleted2 = await calc.add_obsoleted2(1, 2);");
        script.AppendLine("  assert(3, result_calc_add_obsoleted2, 'calc.add_obsoleted2');");

        script.AppendLine("  try {");
        script.AppendLine("    await calc.add_obsoleted3(1, 2);");
        script.AppendLine("    console.error('FAILED: [calc.add_obsoleted3]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    console.log('PASSED: Fatal obsoleted [calc.add_obsoleted3]');");
        script.AppendLine("  }");

        // Able to call different functions on a proxy object.
        script.AppendLine("  const result_proxy_calc_mul = await calc.mul(2, 3);");
        script.AppendLine("  assert(6, result_proxy_calc_mul, 'proxy_calc.mul');");

        // Interoperated exceptions.
        script.AppendLine("  try {");
        script.AppendLine("    await calc.willBeThrow(1, 2);");
        script.AppendLine("    console.error('FAILED: [calc.willBeThrow, 1]');");
        script.AppendLine("  } catch (e) {");
        script.AppendLine("    if (e.props.a == 1 && e.props.b == 2)");
        script.AppendLine("      console.log('PASSED: Will be throw [calc.willBeThrow]');");
        script.AppendLine("    else");
        script.AppendLine("      console.error('FAILED: [calc.willBeThrow, 2]');");
        script.AppendLine("  }");

        script.AppendLine("  console.log('ALL TEST IS DONE AT JavaScript SIDE.');");

        script.AppendLine("}");
        // ----
    }
}