# DupeNukem

![DupeNukem](https://github.com/kekyo/DupeNukem/raw/main/Images/DupeNukem.100.png)

DupeNukem - WebView attachable full-duplex asynchronous interoperable independent messaging library between .NET and JavaScript.

[![Project Status: WIP – Initial development is in progress, but there has not yet been a stable, usable release suitable for the public.](https://www.repostatus.org/badges/latest/wip.svg)](https://www.repostatus.org/#wip)

## NuGet

|Package|NuGet|
|:--|:--|
|DupeNukem|[![NuGet DupeNukem](https://img.shields.io/nuget/v/DupeNukem.svg?style=flat)](https://www.nuget.org/packages/DupeNukem)|

## CI

|main|develop|
|:--|:--|
|[![DupeNukem CI build (main)](https://github.com/kekyo/DupeNukem/workflows/.NET/badge.svg?branch=main)](https://github.com/kekyo/DupeNukem/actions?query=branch%3Amain)|[![DupeNukem CI build (develop)](https://github.com/kekyo/DupeNukem/workflows/.NET/badge.svg?branch=develop)](https://github.com/kekyo/DupeNukem/actions?query=branch%3Adevelop)|

----

## What is this?

General purpose `WebView` attachable independent messaging (RPC like) library.

This library is intended for use with a browser component called `WebView` (Edge2, CefSharp, Android, Celenium and etc) where asynchronous interoperation is not possible or is limited.
It is also independent of any specific `WebView` implementation, so it can be applied to any `WebView` you use.
The only requirement is to be able to send and receive strings to and from each other.

This is a diagrammatic representation of the message transfer performed by DupeNukem.

.NET side to call a function on the JavaScript side, the `InvokeClientFunctionAsync` method returns a `Task`, so it can wait asynchronously:

![.NET world to JavaScript invoking](Images/diagram1.png)

Similarly, JavaScript side to call a method on the .NET side, the `invokeHostMethod` function returns `Promise`, so it can wait asynchronously too:

![.NET world to JavaScript invoking](Images/diagram2.png)

It is complemental design. Both .NET and JavaScript, we can design methods and functions assuming a nearly identical structure.
And with DupeNukem, you can use it for multi-platform `WebView` based applications without having to use different implementations for each `WebView` interface. The implementation can be standardized.

This may seem simple at first glance, but there are some difficult issues to be addressed, such as the following:

* Each call must be distinguished individually.
  DupeNukem manages each call and correctly distinguishes between them, even if multiple calls exist in parallel. (Yes, it is ready for asynchronous parallelism using `Task.WhenAll` and `Promise.all` and like.)
* On `WebView`, only strings must be used as a means of communication.
  DupeNukem uses JSON as the communication format, but the user does not need to be aware of it, except for custom type conversions.
  This can be thought of as the same as the custom type constraints used for sending and receiving in ASP.NET WebAPI, etc.

## Example

Really? Now let's look at the actual calling code both side.

Invoke JavaScript functions from .NET side:

```csharp
var result_add = await messenger.InvokeClientFunctionAsync<int>(
    "js_add", 1, 2);

var result_sub = await messenger.InvokeClientFunctionAsync<int>(
    "js_sub", 1, 2);
```

Invoke .NET methods from JavaScript side (using proxy objects):

```javascript
// `Add` method
const result_Add = await dupeNukem.viewModels.calculator.add(1, 2);

// `dotnet_add` delegate
const result_add = await dotnet_add(1, 2);
```

Here is an example using:

* [`Microsoft.Web.WebView2`](https://www.nuget.org/packages/Microsoft.Web.WebView2) on WPF. ([Fully sample code is here](https://github.com/kekyo/DupeNukem/blob/main/samples/DupeNukem.WebView2/ViewModels/MainWindowViewModel.cs))
* [`CefSharp.Wpf`](https://www.nuget.org/packages/CefSharp.Wpf) on WPF. ([Fully sample code is here](https://github.com/kekyo/DupeNukem/blob/main/samples/DupeNukem.CefSharp/ViewModels/MainWindowViewModel.cs))
* [`Xamarin.Forms`(`Xam.Plugin.WebView`)](https://www.nuget.org/packages/Xam.Plugin.WebView). ([Fully sample code is here](https://github.com/kekyo/DupeNukem/blob/main/samples/DupeNukem.Xamarin.Forms/ViewModels/ContentPageViewModel.cs))

----

## Setup sequence

Setup sequence is gluing between `WebView` and DupeNukem `WebViewMessenger`.
DupeNukem uses only "strings" to exchange messages.
In the code example below (Edge WebView2 on WPF), Step 2 and Step 3 are also set up to mutually exchange message strings.

(Another browser components maybe same as setup process.
See [Gluing browsers](#gluing-browsers) section below.)

```csharp
// Startup sequence.
// Bound between WebView2 and DupeNukem WebViewMessenger.

// Step 1: Construct DupeNukem WebViewMessenger.
// Default timeout duration is 30sec.
var messenger = new WebViewMessenger();

//////////////////////////////////////////

// Step 2: Hook up .NET --> JavaScript message handler.
messenger.SendRequest += (s, e) =>
    webView2.CoreWebView2.PostWebMessageAsString(e.Message);

// Step 3: Hook up JavaScript --> .NET message handler.
webView2.CoreWebView2.WebMessageReceived += (s, e) =>
    messenger.ReceivedRequest(e.TryGetWebMessageAsString());

// Step 4: Injected Messenger script.
await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
    messenger.GetInjectionScript().ToString());
```

----

## Register methods/functions

Bulk register methods on an object:

* Easy way, recommended.
* All methods automatically inject proxy functions in JavaScript side.

```csharp
// Apply `JavaScriptTarget` attribute on target callee method.
public class Calculator
{
    [JavaScriptTarget]
    public Task<int> Add(int a, int b)
    {
        // ...
    }

    [JavaScriptTarget("Subtract")]   // Strictly naming
    public Task<int> __Sub__123(int a, int b)
    {
        // ...
    }
}

////////////////////////////////////////

// JS: `const result = await dupeNukem.viewModels.calculator.add(1, 2);`
// JS: `const result = await dupeNukem.viewModels.calculator.Subtract(1, 2);`
var calculator = new Calculator();
messenger.RegisterObject(calculator);

// JS: `const result = await calc.add(1, 2);`
// JS: `const result = await calc.Subtract(1, 2);`
messenger.RegisterObject("calc", calculator);

// JS: `const result = await add(1, 2);`         // (Put on `window.add`)
// JS: `const result = await Subtract(1, 2);`    // (Put on `window.Subtract`)
messenger.RegisterObject("", calculator);
```

Register methods around .NET side:

* Strict declarative each methods.

```csharp
// JS: `const result = await dupeNukem.viewModels.mainWindowViewModel.add`
messenger.RegisterFunc<int, int, int>(this.Add);
// JS: `const result = await dupeNukem.viewModels.mainWindowViewModel.Subtract`
messenger.RegisterFunc<int, int, int>(this.__Sub__123);

// Or, register directly delegate with method name.

// JS: `const result = await dotnet_add(1, 2);`
messenger.RegisterFunc<int, int, int>(
    "dotnet_add", (a, b) => Task.FromResult(a + b));
// JS: `const result = await dotnet_sub(1, 2);`
messenger.RegisterFunc<int, int, int>(
    "dotnet_sub", (a, b) => Task.FromResult(a - b));
```

Declare functions around JavaScript side:

```javascript
// Global functions:

// .NET: `var result = await messenger.InvokeClientFunctionAsync("js_add", 1, 2);`
async function js_add(a, b) {
    return a + b;
}

// .NET: `var result = await messenger.InvokeClientFunctionAsync("js_sub", 1, 2);`
async function js_sub(a, b) {
    return a - b;
}

// Member functions:
class Foo
{
    async add(a, b) {
        return a + b;
    }

    async sub(a, b) {
        return a - b;
    }
}

// .NET: `var result = await messenger.InvokeClientFunctionAsync("foo.add", 1, 2);`
// .NET: `var result = await messenger.InvokeClientFunctionAsync("foo.sub", 1, 2);`
var foo = new Foo();
```

NOTE: We have to put JavaScript object instance with `var` keyword.
DupeNukem will fail invoking when use `const` or `let` keyword.
It is limitation for JavaScript specification.

----

## Cancellation

.NET has the `CancellationToken` type as the standard infrastructure for
asynchronous processing.
However, JavaScript does not have such a thing.
DupeNukem defines a `CancellationToken` type on the JavaScript side
that can be used as follows:

```javascript
// Prepare a CancellationToken
const ct = new CancellationToken();

// Setup canceler:
document.getElementById("cancelButton").onclick =
    () => ct.cancel();

try {
    // Invoke .NET method asynchronously:
    const resut = await
        dupeNukem.viewModels.mainWindowViewModel.
        longAwaitedMethod(1, 2, ct);
}
catch (e) {
    // An exception is thrown when a cancellation occurs.
}
```

.NET implementation:

```csharp
[JavaScriptTarget("longAwaitedMethod")]
public async Task<int> LongAwaitedMethod(
    int a, int b, CancellationToken ct)
{
    // Pass a CancellationToken to a time-consuming asynchronous process:
    await Task.Delay(1000, ct);
    return a + b;
}
```

NOTE:

* `CancellationToken` argument(s) can be defined anywhere in the argument set.
* The above example is a call in the JavaScript --> .NET direction.
  .NET --> JavaScript direction calls are not yet allowed to use `CancellationToken` in 0.10.0.

----

## Obsoleted / Deprecated

In JavaScript --> .NET method invoking, the following JavaScript debugging aids are available
if the `Obsolete` attribute is applied to the .NET method.

If the normal `Obsolete` attribute is applied,
the following warning message will appear in the JavaScript console output:

```csharp
[Obsolete("This method will be obsoleted, switch to use `add_ng`.")]
public static Task<int> add(int a, int b)
{
  // ...
}
```

```
calc.add is obsoleted: This method will be obsoleted, switch to use `add_ng`.
```

Also, if an error flag is applied to the `Obsolete` attribute,
an exception will be thrown on the fly:

```csharp
[Obsolete("This method is obsoleted, have to switch `add_ng`.", true)]
public static Task<int> add(int a, int b) =>
    throw new InvalidOperationException(
        "This method is obsoleted, have to switch `add_ng`.");
```

```javascript
try {
    consr r = await calc.add(1, 2);
}
catch (e) {
    // Raise error: calc.add is obsoleted: This method is obsoleted, have to switch `add_ng`.
}
```

Note: This function is only available for proxy access,
and will not work if called using the `invokeHostMethod()` function.

----

## Gluing browsers

There are examples for gluing sample code between your app and browser components.

### Edge WebView2

```csharp
// WebView2 webView2;

// Step 2: Hook up .NET --> JavaScript message handler.
messenger.SendRequest += (s, e) =>
    webView2.CoreWebView2.PostWebMessageAsString(e.Message);

// Step 3: Hook up JavaScript --> .NET message handler.
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
await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
    messenger.GetInjectionScript().ToString());
```

### CefSharp

```csharp
// ChromiumWebBrowser cefSharp;

// Step 2: Hook up .NET --> JavaScript message handler.
messenger.SendRequest += (s, e) =>
    cefSharp.BrowserCore.MainFrame.ExecuteJavaScriptAsync(
        e.ToJavaScript());

// Step 3: Attached JavaScript --> .NET message handler.
cefSharp.JavascriptMessageReceived += (s, e) =>
    messenger.ReceivedRequest(e.Message.ToString());

// Step 4: Injected Messenger script.
var script = messenger.GetInjectionScript();
cefSharp.FrameLoadEnd += (s, e) =>
{
    if (e.Frame.IsMain)
    {
        cefSharp.BrowserCore.MainFrame.ExecuteJavaScriptAsync(
            script.ToString());
    }
};
```

### Xamarin Forms (Xam.Plugin.Webview)

Xamarin Forms provides a `WebView` control as a common basis for displaying a web browser.
However, interoperating with JavaScript requires different implementations for each platform, such as Android and iOS.
I assume that this is because the same web browsers are used, Chrome for Android and Safari for iOS.

One package that alleviates such cumbersome implementation is [Xam.Plugin.Webview project](https://github.com/SKLn-Rad/Xam.Plugin.Webview).
Here is an example of using this package:

```csharp
// FormsWebView formsWebView;

// Step 2: Hook up .NET --> JavaScript message handler.
messenger.SendRequest += (s, e) =>
    formsWebView.InjectJavascriptAsync(e.ToJavaScript());

// Step 3: Attached JavaScript --> .NET message handler.
formsWebView.AddLocalCallback(
    messenger.PostMessageSymbolName,
    messenger.ReceivedRequest);

// Step 4: Injected Messenger script.
var script = messenger.GetInjectionScript();
formsWebView.OnNavigationCompleted += (s, url) =>
    formsWebView.InjectJavascriptAsync(script.ToString());
```

### Celenium WebDriver on .NET

TODO: WIP

In the case of Celenium WebDriver, there is no standard way to notify message strings from the browser component to .NET side.
In this example (Step 3), the `alert()` function is used to notify a message strings.
.NET side, the message is passed to DupeNukem when the alert occurs.

```csharp
// IWebDriver driver;

// Step 2: Hook up .NET --> JavaScript message handler.
messenger.SendRequest += (s, e) =>
    driver.ExecuteJavaScript(e.ToJavaScript());

// Step 3: Attached JavaScript --> .NET message handler.
var alert = wait.Until(ExpectedConditions.AlertIsPresent());
messenger.ReceivedRequest(alert.Text);
alert.Accept();

// Step 4: Injected Messenger script.
var script = messenger.GetInjectionScript();
driver.Navigated += (s, e) =>
{
    if (e.Result == WebNavigationResult.Success &&
        e.Url == webView.Source)
    {
        driver.ExecuteJavaScript(script.ToString());
    }
};
```

----

## License

Apache-v2.

----

## History

* 0.16.0:
  * Splitted core library into new `DupeNukem.Core` package, because need to be usable pure interoperation infrastructure. 
    * Please fix indicating at obsolete warnings `new WebViewMessenger` instead of `new Messenger(...)`.
  * Fixed failing to notify caught exception on JavaScript side before promise context.
  * Changed sample Edge WebView2 gluing code.
* 0.15.0:
  * Upgraded `Newtonsoft.Json` to 13.0.1 (See [Vulnerability: Improper Handling of Exceptional Conditions in Newtonsoft.Json - GitHub Advisory Database](https://github.com/advisories/GHSA-5crp-9r3c-p9vr))
* 0.14.0:
  * Fixed causing duplicated key exception when derived class has same named overrided expose method.
* 0.13.0:
  * Changed showing trace message instead raise exception when SendRequest aren't hooked.
* 0.12.0:
  * Unhooked any events on calling Dispose method. (avoid memory leaks)
* 0.11.0:
  * Help debugging by warning log and raise exception at JavaScript when , .NET method is marked with `Obsolete` attribute.
  * Fixed registering implicit proxy methods at around browser reloading. 
* 0.10.0:
  * Supported `CancellationToken` when JavaScript --> .NET direction calling.
* 0.9.0:
  * Fixed didn't initialize on XF iOS.
* 0.8.0:
  * Fixed causing InvalidMethodName exception when use longer method name.
* 0.7.0:
  * Supported CefSharp and Xamarin Forms.
* 0.6.0:
  * Supported proxy object on JavaScript side.
  * Implemented automatic thread marshaling (No need for marshalling to UI threads as manually.)
* 0.5.0:
  * Supported customize json format with `JsonSerializer` and made defaults with camel-casing serialization.
  * Made defaults for all symbol naming to camel case.
  * Added more target platforms.
* 0.4.0:
  * New bulk register methods on an object by `RegisterObject(obj)` method.
  * Fixed invoking silent result with invalid method name.
* 0.3.0:
  * Implemented flexible argument type handling.
* 0.2.0:
  * Improved enum type handling.
* 0.1.0:
  * Initial release.
