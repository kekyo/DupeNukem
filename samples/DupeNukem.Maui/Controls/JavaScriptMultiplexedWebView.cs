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

using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace DupeNukem.Maui.Controls;

public sealed class JavaScriptMessageEventArgs : EventArgs
{
    public readonly string Message;

    public JavaScriptMessageEventArgs(string message) =>
        this.Message = message;
}

public sealed class JavaScriptMultiplexedWebView : WebView
{
    private Func<string, Task>? invokedJavaScript;

    // DIRTY: JavaScriptMultiplexedWebView:
    //   MAUI does not have a platform-neutral event interface,
    //   so must implement MAUI handlers on each platform...
    //   In this project, examples for Android and Windows are provided.
    //   For more information, search for JavaScriptMultiplexedWebView.
    public event EventHandler<JavaScriptMessageEventArgs>? MessageReceived;

    internal void SendToHostMessage(string message) =>
        this.MessageReceived?.Invoke(this, new(message));

    internal void SetInvokedJavaScriptListener(Func<string, Task>? invokedJavaScript) =>
        this.invokedJavaScript = invokedJavaScript;

    public Task InvokeJavaScriptAsync(string script) =>
        this.invokedJavaScript?.Invoke(script) ?? Task.CompletedTask;
}
