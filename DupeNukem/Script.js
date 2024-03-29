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
    this.ctss__ = new Map();

    this.sendToHostMessage__ = function (jsonMessage) {
        const sendTo = window.__dupeNukem_Messenger_sendToHostMessage__;
        if (sendTo != undefined) {
            // Guard identity (Avoiding another processor message).
            const message = "__dupeNukem__" + jsonMessage;
            sendTo(message);
        }
    }

    this.registry__ = new FinalizationRegistry(name => {
        this.sendToHostMessage__(
            JSON.stringify({ id: "discard", type: "metadata", body: name, }));
        this.log__("DupeNukem: Sent discarded closure function: " + name);
    });

    this.log__ = (message) => {
        if (this.debugLog__) {
            console.log(message);
        }
    };

    this.initialize__ = () => {
        if (!this.isInitialized__) {
            this.isInitialized__ = true;
            if (window.__dupeNukem_Messenger__.sendToHostMessage__ != null) {
                window.__dupeNukem_Messenger__.sendToHostMessage__(
                    JSON.stringify({ id: "ready", type: "control", body: null, }));
            }
            else {
                const innerInit = function () {
                    if (window.__dupeNukem_Messenger__.sendToHostMessage__ != null) {
                        console.info("DupeNukem: Ready by host managed.");
                        window.__dupeNukem_Messenger__.sendToHostMessage__(
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
        window.__dupeNukem_Messenger__.sendToHostMessage__(JSON.stringify(
            { id: message.id, type: "failed", body: exceptionBody, }))
    };

    this.arrivedHostMesssage__ = (jsonString) => {
        // Guard identity (Avoiding another processor message).
        if (!jsonString.startsWith("__dupeNukem__")) {
            return;
        }
        try {
            const message = JSON.parse(jsonString.substring(13));
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
                                        } else if (arg.type == "metadata") {
                                            const name = arg.body;
                                            switch (arg.id) {
                                                case "closure":
                                                    if (name.startsWith("closure_$")) {
                                                        const cb = function () {
                                                            const args = new Array(arguments.length);
                                                            for (let i = 0; i < args.length; i++) {
                                                                args[i] = arguments[i];
                                                            }
                                                            return window.__dupeNukem_Messenger__.invokeHostMethod__(name, args);
                                                        };
                                                        this.registry__.register(cb, name);
                                                        return cb;
                                                    }
                                                    break;
                                                case "cancellationToken":
                                                    if (name.startsWith("cancellationToken_$")) {
                                                        let wr = this.ctss__.get(name);
                                                        let ct = wr?.deref();
                                                        if (ct != undefined) {
                                                            return ct;
                                                        }
                                                        ct = new CancellationToken(name);
                                                        wr = new WeakRef(ct);
                                                        this.ctss__.set(name, wr);
                                                        return ct;
                                                    }
                                                    break;
                                            }
                                            return undefined;
                                        } else {
                                            return arg;
                                        }
                                    });
                                f.apply(ti, args).
                                    then(result => window.__dupeNukem_Messenger__.sendToHostMessage__(JSON.stringify({ id: message.id, type: "succeeded", body: result, }))).
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
                case "metadata":
                    this.log__("DupeNukem: metadata: " + message.id + ": " + message.body);
                    switch (message.id) {
                        case "discard":
                            // Decline invalid name to avoid security attacks.
                            if (message.body.startsWith("__peerClosures__.closure_$")) {
                                const baseName = message.body.substring(17);
                                delete window.__peerClosures__[baseName];
                                this.log__("DupeNukem: Deleted peer closure target function: " + baseName);
                            }
                            break;
                        case "cancel":
                            if (message.body.startsWith("cancellationToken_$")) {
                                const name = message.body;
                                let wr = this.ctss__.get(name);
                                let ct = wr?.deref();
                                if (ct == undefined) {
                                    ct = new CancellationToken(name);
                                    ct.__isCanceled__ = true;
                                    wr = new WeakRef(ct);
                                    this.ctss__.set(name, wr);
                                } else if (!ct.__isCanceled__) {
                                    ct.__isCanceled__ = true;
                                    if (ct.__action__ != undefined) {
                                        ct.__action__();
                                    }
                                }
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
                return { id: "closure", type: "metadata", body: name, };
            } else if (arg?.__ctid__ != undefined) {
                return { id: "cancellationToken", type: "metadata", body: arg.__ctid__, };
            }
            return arg;
        });

        return new Promise((resolve, reject) => {
            const id = "client_" + (this.id__++);
            try {
                const descriptor = { resolve: resolve, reject: reject, };
                this.suspendings__.set(id, descriptor);
                window.__dupeNukem_Messenger__.sendToHostMessage__(JSON.stringify({ id: id, type: "invoke", body: { name: name, args: rargs, }, }));
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

    // Initialize:
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
    delay || function (msec, ct) {
    return new Promise(function (resolve, reject) {
        if (ct != undefined) {
            ct.register(() => reject(new OperationCancelledError()));
        }
        setTimeout(resolve, msec);
    });
}

//////////////////////////////////////////////////

// OperationCancelledError declaration.
var OperationCancelledError =
    OperationCancelledError || (function () {
        function OperationCancelledError(...args) {
            Error.apply(this, args);
            this.name = this.constructor.name;
            if (Error.captureStackTrace) {
                Error.captureStackTrace(this, OperationCancelledError);
            }
        }
        OperationCancelledError.prototype = Object.create(Error.prototype);
        OperationCancelledError.prototype.constructor = OperationCancelledError;
        return OperationCancelledError;
})();

// CancellationToken declaration.
var CancellationToken =
    CancellationToken || function (ctid) {
        if (ctid != undefined) {
            this.__ctid__ = ctid;
        } else {
            this.__ctid__ = "cancellationToken_$" + (window.__dupeNukem_Messenger__.id__++);
        }
        this.__isCanceled__ = false;

        this.cancel = () => {
            if (!this.__isCanceled__) {
                this.__isCanceled__ = true;
                window.__dupeNukem_Messenger__.sendToHostMessage__(
                    JSON.stringify({ id: "cancel", type: "metadata", body: this.__ctid__, }));
            }
        };
        this.register = action => {
            if (this.__isCanceled__) {
                action();
            } else {
                this.__action__ = action;
            }
        };
        this.isCancellationRequested = () => {
            return this.__isCanceled__;
        };
        this.throwIfCancellationRequested = () => {
            if (this.__isCanceled__) {
                throw new OperationCancelledError();
            }
        };
    };

//////////////////////////////////////////////////

// Final initializer.
__dupeNukem_Messenger__.initialize__();

///////////////////////////////////////////////////////////////////////////////

