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

---

## What is this?

General purpose `WebView` attachable independent messaging library.

* Made full-duplex and asynchronous messaging between .NET WebView and JavaScript.
  * In .NET side, all function calls are made with `Task`.
  * In JavaScript side, all method calls are made with `Promise`.
* General purpose - it requirements are only:
  * Send and receive pure string from .NET world to JavaScript.
  * Send and receive pure string from JavaScript world to .NET.
  * That means, DupeNukem can attach all WebView-like browser components with bit glue code fragments.

---

## Example

Here is an example using [`Microsoft.Web.WebView2`](https://www.nuget.org/packages/Microsoft.Web.WebView2) on WPF. ([Fully sample code is here](https://github.com/kekyo/DupeNukem/blob/main/samples/DupeNukem.WebView2/ViewModels/MainWindowViewModel.cs))

### Setup

Setup process is glueing between `WebView` and DupeNukem `Messenger`.
Another browser components maybe same as setup process. See `Another browsers` below.

```csharp
// Startup sequence.
// Bound both WebView2 and DupeNukem Messenger.

// Step 1: Construct DupeNukem Messenger.
// Default timeout duration is 30sec.
var messenger = new Messenger();

//////////////////////////////////////////

// Initialize WebView2.
await webView2.EnsureCoreWebView2Async();

// Step 2: Hook up .NET --> JavaScript message handler.
messenger.SendRequest += (s, e) =>
    // WPF requires switching to UI thread when manipulate UI elements.
    dispatcher.BeginInvoke(() =>
        webView2.CoreWebView2.PostWebMessageAsString(e.Message));

// Step 3: Hook up JavaScript --> .NET message handler.
webView2.CoreWebView2.WebMessageReceived += (s, e) =>
    messenger.ReceivedRequest(e.TryGetWebMessageAsString());

// Step 4: Injected Messenger script.
await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
    messenger.GetInjectionScript().ToString());
```

### Register methods/functions

Register methods around .NET side:

```csharp
// Register .NET side methods:

// name: `DupeNukem.ViewModels.MainWindowViewModel.Add`
messenger.RegisterFunc<int, int, int>(this.Add);
// name: `DupeNukem.ViewModels.MainWindowViewModel.Sub`
messenger.RegisterFunc<int, int, int>(this.Sub);

// Or, register directly delegate with method name.
messenger.RegisterFunc<int, int, int>(
    "dotnet_add", (a, b) => Task.FromResult(a + b));
messenger.RegisterFunc<int, int, int>(
    "dotnet_sub", (a, b) => Task.FromResult(a - b));
```

Declare functions around JavaScript side:

```javascript
async function js_add(a, b) {
    return a + b;
}_

async function js_sub(a, b) {
    return a - b;
}_
```

### Use it

Invoke JavaScript functions from .NET side:

```csharp
// Invoke JavaScript functions:

var result_add = await messenger.InvokeClientFunctionAsync<int>(
    "js_add", 1, 2);
var result_sub = await messenger.InvokeClientFunctionAsync<int>(
    "js_sub", 1, 2);
```

Invoke .NET methods from JavaScript side:

```javascript
// Invoke .NET methods:

// `invokeHostMethod` function will return with `Promise`,
// so we can handle asynchronous operation naturally.

// `Add` method
const result_Add_ = await invokeHostMethod(
    "DupeNukem.ViewModels.MainWindowViewModel.Add",
    1, 2);
// `dotnet_add` delegate
const result_add_ = await invokeHostMethod(
    "dotnet_add", 1, 2);
```

---

## Another browsers

It's knowledges for glueing browser components.

### CefSharp

TODO:

### Android WebView

TODO:

---

## License

Apache-v2.

---

## History

* 0.3.0:
  * Implemented flexible argument type handling.
* 0.2.0:
  * Improved enum type handling.
* 0.1.0:
  * Initial release.
