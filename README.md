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
    "js_add",
    1, 2);
var result_sub = await messenger.InvokeClientFunctionAsync<int>(
    "js_sub",
    1, 2);
```

Invoke .NET methods from JavaScript side:

```javascript
// `Add` method
const result_Add = await invokeHostMethod(
    "DupeNukem.ViewModels.Calculator.Add",
    1, 2);
// `dotnet_add` delegate
const result_add = await invokeHostMethod(
    "dotnet_add",
    1, 2);
```

Here is an example using [`Microsoft.Web.WebView2`](https://www.nuget.org/packages/Microsoft.Web.WebView2) on WPF. ([Fully sample code is here](https://github.com/kekyo/DupeNukem/blob/main/samples/DupeNukem.WebView2/ViewModels/MainWindowViewModel.cs))

----

## Setup infrastructure

Setup process is gluing between `WebView` and DupeNukem `Messenger`.
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

```csharp
// Apply `JavaScriptTarget` attribute on target callee method.
public class Calculator
{
    [JavaScriptTarget]
    public Task<int> Add(int a, int b)
    {
        // ...
    }

    [JavaScriptTarget("Sub")]   // Strictly naming
    public Task<int> __Sub__123(int a, int b)
    {
        // ...
    }
}

////////////////////////////////////////

// name: `DupeNukem.ViewModels.Calculator.Add`, `DupeNukem.ViewModels.Calculator.Sub`
var calculator = new Calculator();
messenger.RegisterObject(calculator);

// name: `calc.Add`, `calc.Sub`
messenger.RegisterObject("calc", calculator);
```

Register methods around .NET side:

* Strict declarative each methods.

```csharp
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
}

async function js_sub(a, b) {
    return a - b;
}
```

----

## Another browsers

It's knowledges for gluing browser components.

### CefSharp

TODO:

### Android WebView

TODO:

### Celenium WebDriver on .NET

TODO:

----

## License

Apache-v2.

----

## History

* 0.6.0:
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
