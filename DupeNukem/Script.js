////////////////////////////////////////////////////////////////////////////
//
// DupeNukem - WebView attachable full-duplex asynchronous interoperable
// messaging library between .NET and JavaScript.
//
// Copyright (c) Kouji Matsui (@kozy_kekyo, @kekyo@mastodon.cloud)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

// Core dispatcher for JavaScript side.
class DupeNukem_Messenger__ {

    constructor(hookup) {
        this.suspendings__ = new Map();
        this.id__ = 0;

        if (hookup != undefined) {
            hookup();
        }
        else {
            if (window?.external?.notify != undefined) {
                this.sendToHostMessage__ = window.external.notify;
                console.info("DupeNukem: Microsoft WebView1 detected.");
            }
            else if (window?.chrome?.webview?.postMessage != undefined) {
                this.sendToHostMessage__ = window.chrome.webview.postMessage;
                window.chrome.webview.addEventListener(
                    "message", e => { this.arrivedHostMesssage__(e.data); });
                console.info("DupeNukem: Microsoft WebView2 detected.");
            }
            else {
                this.sendToHostMessage__ = function (_) { };
                console.warn("DupeNukem: couldn't detect host browser type.");
            }
        }
    }

    sendExceptionToHost__(message, exceptionBody) {
        this.sendToHostMessage__(JSON.stringify(
            { id: message.id, type: "failed", body: exceptionBody, }))
    }

    arrivedHostMesssage__(jsonString) {
        try {
            const message = JSON.parse(jsonString);
            switch (message.type) {
                case "succeeded":
                    const successorDescriptor = this.suspendings__.get(message.id);
                    if (successorDescriptor != undefined) {
                        this.suspendings__.delete(message.id);
                        successorDescriptor.resolve(message.body);
                    }
                    else {
                        console.warn("DupeNukem: sprious message received: " + jsonString);
                    }
                    break;
                case "failed":
                    const failureDescriptor = this.suspendings__.get(message.id);
                    if (failureDescriptor != undefined) {
                        this.suspendings__.delete(message.id);
                        const e = new Error(message.body.message);
                        e.name = message.body.name;
                        e.detail = message.body.detail;
                        failureDescriptor.reject(e);
                    }
                    else {
                        console.warn("DupeNukem: sprious message received: " + jsonString);
                    }
                    break;
                case "invoke":
                    try {
                        const ne = message.body.name.split(".");
                        const ti = ne.
                            slice(0, ne.length - 1).
                            reduce(function (o, n) {
                                if (o != undefined) {
                                    const next = o[n];
                                    if (next == undefined) {
                                        this.sendExceptionToHost__(message, { name: "invalidFieldName", message: "Field \"" + n + "\" is not found.", detail: "", });
                                    }
                                    return next;
                                }
                                else {
                                    return o;
                                }
                            }, window);
                        if (ti != undefined) {
                            const fn = ne[ne.length - 1];
                            const f = ti[fn];
                            if (f == undefined) {
                                this.sendExceptionToHost__(message, { name: "invalidFunctionName", message: "Function \"" + fn + "\" is not found.", detail: "", });
                            }
                            else {
                                f.apply(ti, message.body.args).
                                    then(result => this.sendToHostMessage__(JSON.stringify({ id: message.id, type: "succeeded", body: result, }))).
                                    catch(e => this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), }));
                            }
                        }
                    }
                    catch (e) {
                        this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), });
                    }
                    break;
            }
        }
        catch (e) {
            console.warn("DupeNukem: unknown error: " + e.message + ": " + jsonString);
        }
    }

    invokeHostMethod__(name, args) {
        return new Promise((resolve, reject) => {
            const id = "client_" + (this.id__++);
            try {
                const descriptor = { resolve: resolve, reject: reject, };
                this.suspendings__.set(id, descriptor);
                this.sendToHostMessage__(JSON.stringify({ id: id, type: "invoke", body: { name: name, args: args, }, }));
            }
            catch (e) {
                reject(e);
            }
        });
    }
}

//////////////////////////////////////////////////

// Often you have to give a custom hook up function `dupeNukem_Messenger_hookup`
// BEFORE this script when you need to another browser support
// at initializing process on `messenger.GetInjectionScript()`.
//
// ```csharp
// var script = messenger.GetInjectionScript();
// script.Insert(0, "function dupeNukem_Messenger_hookup() { ... }");
//
// webView.InjectScript(script.ToString());
// ```
const dupeNukem_Messenger__ =
    new DupeNukem_Messenger__(window.dupeNukem_Messenger_hookup);

//////////////////////////////////////////////////

// Invoke to .NET method.
// invokeHostMethod(methodName, ...) : Promise
function invokeHostMethod(methodName) {
    const args = new Array(arguments.length - 1);
    for (let i = 0; i < args.length; i++) {
        args[i] = arguments[i + 1];
    }
    return dupeNukem_Messenger__.invokeHostMethod__(methodName, args);
}

// Final initializer.
invokeHostMethod("dupeNukem_Messenger_ready__");

///////////////////////////////////////////////////////////////////////////////

