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
var __dupeNukem_Messenger__ =
    __dupeNukem_Messenger__ || new (function () {

    this.suspendings__ = new Map();
    this.id__ = 0;
    this.debugLog__ = false;
    this.isInitialized__ = false;

    this.registry__ = new FinalizationRegistry(name => {
        if (window.__dupeNukem_Messenger_sendToHostMessage__ != null) {
            window.__dupeNukem_Messenger_sendToHostMessage__(
                JSON.stringify({ id: "discard", type: "closure", body: name, }));
        }
    });

    this.log__ = (message) => {
        if (this.debugLog__) {
            console.log(message);
        }
    };

    this.initialize__ = () => {
        if (!this.isInitialized__) {
            this.isInitialized__ = true;
            if (window.__dupeNukem_Messenger_sendToHostMessage__ != null) {
                window.__dupeNukem_Messenger_sendToHostMessage__(
                    JSON.stringify({ id: "ready", type: "control", body: null, }));
            }
            else {
                const innerInit = function () {
                    if (window.__dupeNukem_Messenger_sendToHostMessage__ != null) {
                        console.info("DupeNukem: Ready by host managed.");
                        window.__dupeNukem_Messenger_sendToHostMessage__(
                            JSON.stringify({ id: "ready", type: "control", body: null, }));
                    }
                    else {
                        console.info("DupeNukem: Waiting managed by host...");
                        setTimeout(innerInit, 1000);
                    }
                }
                setTimeout(innerInit, 1000);
            }
        }
    };

    this.sendExceptionToHost__ = (message, exceptionBody) => {
        window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify(
            { id: message.id, type: "failed", body: exceptionBody, }))
    };

    this.arrivedHostMesssage__ = (jsonString) => {
        try {
            const message = JSON.parse(jsonString);
            switch (message.type) {
                case "succeeded":
                    this.log__("DupeNukem: succeeded: " + message.id);
                    const successorDescriptor = this.suspendings__.get(message.id);
                    if (successorDescriptor != undefined) {
                        this.suspendings__.delete(message.id);
                        successorDescriptor.resolve(message.body);
                    }
                    else {
                        console.warn("DupeNukem: suprious message received: " + jsonString);
                    }
                    break;
                case "failed":
                    this.log__("DupeNukem: failed: " + message.id);
                    const failureDescriptor = this.suspendings__.get(message.id);
                    if (failureDescriptor != undefined) {
                        this.suspendings__.delete(message.id);
                        const e = new Error(message.body.message);
                        e.name = message.body.name;
                        e.detail = message.body.detail;
                        e.props = message.body.props;
                        failureDescriptor.reject(e);
                    }
                    else {
                        console.warn("DupeNukem: suprious message received: " + jsonString);
                    }
                    break;
                case "invoke":
                    try {
                        this.log__("DupeNukem: invoke: " + message.body.name + "(...)");
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
                                const args = message.body.args.map(
                                    arg => {
                                        if (arg == null) {
                                            return null;
                                        } else if (arg.id == "descriptor" && arg.type == "closure" && arg.body?.startsWith("closure_$")) {
                                            const name = arg.body;
                                            const cb = function () {
                                                const args = new Array(arguments.length);
                                                for (let i = 0; i < args.length; i++) {
                                                    args[i] = arguments[i];
                                                }
                                                return window.__dupeNukem_Messenger__.invokeHostMethod__(name, args);
                                            };
                                            this.registry__.register(cb, name);
                                            return cb;
                                        } else {
                                            return arg;
                                        }
                                    });
                                f.apply(ti, args).
                                    then(result => window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify({ id: message.id, type: "succeeded", body: result, }))).
                                    catch(e => this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), }));
                            }
                        }
                    }
                    catch (e) {
                        this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), });
                    }
                    break;
                case "control":
                    this.log__("DupeNukem: control: " + message.id + ": " + message.body);
                    switch (message.id) {
                        case "inject":
                            this.injectProxy__(message.body);
                            break;
                        case "delete":
                            this.deleteProxy__(message.body);
                            break;
                    }
                    break;
                case "closure":
                    this.log__("DupeNukem: closure: " + message.id + ": " + message.body);
                    switch (message.id) {
                        case "discard":
                            // Decline invalid name to avoid security attacks.
                            if (message.body.startsWith("__peerClosures__.closure_$")) {
                                const baseName = message.body.substring(17);
                                delete window.__peerClosures__[baseName];
                                this.log__("Detected abandoned peer closure: " + baseName);
                            }
                            break;
                    }
                    break;
            }
        }
        catch (e) {
            console.warn("DupeNukem: unknown error: " + e.message + ": " + jsonString);
        }
    };

    this.invokeHostMethod__ = (name, args) => {
        const rargs = args.map(arg => {
            if (typeof arg == "function") {
                const baseName = "closure_$" + (this.id__++);
                window.__peerClosures__[baseName] = arg;
                const name = "__peerClosures__." + baseName;
                return { id: "descriptor", type: "closure", body: name, };
            } else {
                return arg;
            }
        });

        return new Promise((resolve, reject) => {
            const id = "client_" + (this.id__++);
            try {
                const descriptor = { resolve: resolve, reject: reject, };
                this.suspendings__.set(id, descriptor);
                window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify({ id: id, type: "invoke", body: { name: name, args: rargs, }, }));
            }
            catch (e) {
                reject(e);
            }
        });
    };

    this.getScopedElement__ = (names) => {
        let current = window;
        for (const name of names.slice(0, names.length - 1)) {
            let next = current[name];
            if (next == undefined) {
                next = new Object();
                Object.defineProperty(current, name, {
                    value: next,
                    writable: false,
                    enumerable: true,
                    configurable: true,
                });
            }
            current = next;
        }
        return current;
    };

    this.injectProxy__ = (entry) => {
        const name = entry.name;

        const ne = name.split(".");
        const fn = ne[ne.length - 1];

        let current = window;
        for (const name of ne.slice(0, ne.length - 1)) {
            let next = current[name];
            if (next == undefined) {
                next = new Object();
                Object.defineProperty(current, name, {
                    value: next,
                    writable: false,
                    enumerable: true,
                    configurable: true,
                });
            }
            current = next;
        }

        Object.defineProperty(current, fn, {
            value: window.__dupeNukem_invokeHostMethod__.bind(current, entry),
            writable: false,
            enumerable: true,
            configurable: true,
        });
    };

    this.deleteProxy__ = (name) => {
        const ne = name.split(".");
        const fn = ne[ne.length - 1];

        let current = window;
        for (const name of ne.slice(0, ne.length - 1)) {
            let next = current[name];
            if (next == undefined) {
                return;
            }
            current = next;
        }

        delete current[fn];
    }

    if (window.external != undefined &&
        window.external.notify != undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ = window.external.notify;
        console.info("DupeNukem: Microsoft WebView1 detected.");
    }
    else if (window.chrome != undefined &&
        window.chrome.webview != undefined &&
        window.chrome.webview.postMessage != undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ = window.chrome.webview.postMessage;
        window.chrome.webview.addEventListener(
            "message", e => { this.arrivedHostMesssage__(e.data); });
        console.info("DupeNukem: Microsoft WebView2 detected.");
    }
    else if (window.CefSharp != undefined &&
        window.CefSharp.PostMessage != undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ = window.CefSharp.PostMessage;
        console.info("DupeNukem: CefSharp detected.");
    }
    else if (window.__dupeNukem_Messenger_sendToHostMessage__ != undefined) {
        console.info("DupeNukem: Ready to host managed.");
    }
})();

var __peerClosures__ = new Object();

var __dupeNukem_invokeHostMethod__ =
    __dupeNukem_invokeHostMethod__ || function (entry) {
        const name = entry.name;
        const obsolete = entry.obsolete;
        if (obsolete == "obsolete") {
            console.warn(entry.obsoleteMessage);
        }
        else if (obsolete == "error") {
            throw new Error(entry.obsoleteMessage);
        }
        const args = new Array(arguments.length - 1);
        for (let i = 0; i < args.length; i++) {
            args[i] = arguments[i + 1];
        }
        return window.__dupeNukem_Messenger__.invokeHostMethod__(name, args);
    }

//////////////////////////////////////////////////

// Invoke to .NET method.
// invokeHostMethod(methodName, ...) : Promise
var invokeHostMethod =
    invokeHostMethod || function (methodName) {
    const args = new Array(arguments.length - 1);
    for (let i = 0; i < args.length; i++) {
        args[i] = arguments[i + 1];
    }
    return window.__dupeNukem_Messenger__.invokeHostMethod__(methodName, args);
}

// Task.Delay like function
var delay =
    delay || function (msec) {
    return new Promise(function (resolve, reject) {
        setTimeout(resolve, msec);
    });
}

//////////////////////////////////////////////////

// CancellationToken declaration.
var CancellationToken =
    CancellationToken || function () {
        this.__scope__ = "cancellationToken_" + (window.__dupeNukem_Messenger__.id__++);
        this.cancel = () => invokeHostMethod(this.__scope__ + ".cancel");
    };

//////////////////////////////////////////////////

// Final initializer.
__dupeNukem_Messenger__.initialize__();

///////////////////////////////////////////////////////////////////////////////

