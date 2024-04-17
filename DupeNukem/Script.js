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
                JSON.stringify({ id: "discard", type: "control", body: name, }));
            this.log__("DupeNukem: Sent discarded closure function: " + name);
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

    this.constructClosure__ = f => {
        if (f.__dupeNukem_id__ === undefined) {
            const closureId = "closure_$" + (this.id__++);
            f.__dupeNukem_id__ = closureId;
            window.__peerClosures__[closureId] = f;
            const name = "__peerClosures__." + closureId;
            return {
                __type__: "closure",
                __body__: name
            };
        } else {
            const closureId = f.__dupeNukem_id__;
            const name = "__peerClosures__." + closureId;
            return {
                __type__: "closure",
                __body__: name
            };
        }
    };

    this.constructAbortSignal__ = signal => {
        if (signal.__dupeNukem_id__ === undefined) {
            const signalId = "abortSignal_$" + (this.id__++);
            signal.__dupeNukem_id__ = signalId;
            if (!signal.aborted) {
                signal.addEventListener(
                    "abort",
                    () => window.invokeHostMethod(signalId + ".cancel"));
            }
            return {
                __type__: "abortSignal",
                __body__: { __scope__: signalId, __aborted__: signal.aborted }
            };
        } else {
            const signalId = signal.__dupeNukem_id__;
            return {
                __type__: "abortSignal",
                __body__: { __scope__: signalId, __aborted__: signal.aborted }
            };
        }
    };

    this.constructArray__ = arr => {
        const base64 = btoa(new TextDecoder().decode(arr));
        return {
            __type__: "byteArray",
            __body__: base64
        };
    };

    this.normalizeObjects__ = obj => {
        if (obj instanceof Function) {
            return this.constructClosure__(obj);
        } else if (obj instanceof ArrayBuffer) {
            return this.constructArray__(new Uint8Array(obj));
        } else if (obj instanceof Uint8Array) {
            return this.constructArray__(obj);
        } else if (obj instanceof Uint8ClampedArray) {
            return this.constructArray__(obj);
        } else if (obj instanceof Array) {
            return obj.map(this.normalizeObjects__);
        } else if (obj instanceof AbortSignal) {
            return this.constructAbortSignal__(obj);
        } else if (obj instanceof CancellationToken) {
            return this.constructAbortSignal__(obj.__ac__.signal);
        } else if (obj instanceof Object) {
            const newobj = {};
            for (const [k, v] of Object.entries(obj)) {
                newobj[k] = this.normalizeObjects__(v);
            }
            return newobj;
        } else {
            return obj;
        }
    }

    this.unnormalizeObjects__ = obj => {
        if (obj === undefined || obj === null) {
            return obj;
        } else if (obj.__type__ !== undefined && obj.__body__ !== undefined) {
            switch (obj.__type__) {
                case "closure":
                    if (obj.__body__.startsWith("closure_$")) {
                        const closureId = obj.__body__;
                        const cb = function () {
                            const args = new Array(arguments.length);
                            for (let i = 0; i < args.length; i++) {
                                args[i] = arguments[i];
                            }
                            return window.__dupeNukem_Messenger__.invokeHostMethod__(closureId, args);
                        };
                        this.registry__.register(cb, closureId);
                        return cb;
                    }
                    break;
                case "abortSignal":
                    // TODO:
                    console.warn("DupeNukem: CancellationToken in host is not supported.");
                    break;
                case "byteArray":
                    if (obj.__body__ !== null) {
                        const base64 = obj.__body__;
                        const arr = new TextEncoder().encode(atob(base64));
                        return arr.buffer;
                    } else {
                        return null;
                    }
            }
            return obj;
        } else if (obj instanceof Array) {
            return obj.map(this.unnormalizeObjects__);
        } else if (obj instanceof Object) {
            const newobj = {};
            for (const [k, v] of Object.entries(obj)) {
                newobj[k] = this.unnormalizeObjects__(v);
            }
            return newobj;
        } else {
            return obj;
        }
    }

    this.arrivedHostMesssage__ = (jsonString) => {
        try {
            const message = JSON.parse(jsonString);
            switch (message.type) {
                case "succeeded":
                    this.log__("DupeNukem: host message: succeeded: " + message.id);
                    const successorDescriptor = this.suspendings__.get(message.id);
                    if (successorDescriptor !== undefined) {
                        this.suspendings__.delete(message.id);
                        successorDescriptor.resolve(this.unnormalizeObjects__(message.body));
                    } else {
                        console.warn("DupeNukem: suprious message received: " + jsonString);
                    }
                    break;
                case "failed":
                    this.log__("DupeNukem: host message: failed: " + message.id);
                    const failureDescriptor = this.suspendings__.get(message.id);
                    if (failureDescriptor !== undefined) {
                        this.suspendings__.delete(message.id);
                        const e = new Error(message.body.message);
                        e.name = message.body.name;
                        e.detail = message.body.detail;
                        e.props = message.body.props;
                        failureDescriptor.reject(e);
                    } else {
                        console.warn("DupeNukem: host message: suprious message received: " + jsonString);
                    }
                    break;
                case "invoke":
                    try {
                        this.log__("DupeNukem: host message: invoke: " + message.body.name + "(...)");
                        const ne = message.body.name.split(".");
                        const ti = ne.
                            slice(0, ne.length - 1).
                            reduce((o, n) => {
                                if (o !== undefined) {
                                    const next = o[n];
                                    if (next === undefined) {
                                        this.sendExceptionToHost__(message, { name: "invalidFieldName", message: "Field \"" + n + "\" is not found.", detail: "", });
                                    }
                                    return next;
                                } else {
                                    return o;
                                }
                            }, window);
                        if (ti !== undefined) {
                            const fn = ne[ne.length - 1];
                            const f = ti[fn];
                            if (f === undefined) {
                                this.sendExceptionToHost__(message, { name: "invalidFunctionName", message: "Function \"" + fn + "\" is not found.", detail: "", });
                            } else {
                                const args = message.body.args.map(this.unnormalizeObjects__);
                                f.apply(ti, args).
                                    then(result => window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify({ id: message.id, type: "succeeded", body: this.normalizeObjects__(result), }))).
                                    catch(e => this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), }));
                            }
                        }
                    } catch (e) {
                        this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), });
                    }
                    break;
                case "control":
                    this.log__("DupeNukem: host message: control: " + message.id + ": " + message.body);
                    switch (message.id) {
                        case "inject":
                            this.injectProxy__(message.body);
                            break;
                        case "delete":
                            this.deleteProxy__(message.body);
                            break;
                        case "discard":
                            // Decline invalid name to avoid security attacks.
                            if (message.body.startsWith("__peerClosures__.closure_$")) {
                                const baseName = message.body.substring(17);
                                delete window.__peerClosures__[baseName];
                                this.log__("DupeNukem: Deleted peer closure target function: " + baseName);
                            }
                            break;
                        default:
                            throw new Error("Invalid host message id: " + message.id);
                    }
                    break;
            }
        }
        catch (e) {
            console.warn("DupeNukem: host message: unknown error: " + e.message + ": " + jsonString);
        }
    };
    
    this.invokeHostMethod__ = (name, args) => {
        const rargs = args.map(this.normalizeObjects__);
        return new Promise((resolve, reject) => {
            const id = "client_" + (this.id__++);
            try {
                const descriptor = { resolve: resolve, reject: reject, };
                this.suspendings__.set(id, descriptor);
                window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify({ id: id, type: "invoke", body: { name: name, args: rargs, }, }));
            } catch (e) {
                reject(e);
            }
        });
    };

    this.getScopedElement__ = (names) => {
        let current = window;
        for (const name of names.slice(0, names.length - 1)) {
            let next = current[name];
            if (next === undefined) {
                next = {};
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
            if (next === undefined) {
                next = {};
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
            if (next === undefined) {
                return;
            }
            current = next;
        }

        delete current[fn];
    }

    if (window.external !== undefined &&
        window.external.notify !== undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ = window.external.notify;
        console.info("DupeNukem: Microsoft WebView1 detected.");
    }
    else if (window.chrome !== undefined &&
        window.chrome.webview !== undefined &&
        window.chrome.webview.postMessage !== undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ = window.chrome.webview.postMessage;
        window.chrome.webview.addEventListener(
            "message", e => { this.arrivedHostMesssage__(e.data); });
        console.info("DupeNukem: Microsoft WebView2 detected.");
    }
    else if (window.CefSharp !== undefined &&
        window.CefSharp.PostMessage !== undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ = window.CefSharp.PostMessage;
        console.info("DupeNukem: CefSharp detected.");
    }
    else if (window.__dupeNukem_Messenger_host__ !== undefined &&
        window.__dupeNukem_Messenger_host__.__sendToHostMessage__ !== undefined) {
        window.__dupeNukem_Messenger_sendToHostMessage__ =
            m => window.__dupeNukem_Messenger_host__.__sendToHostMessage__(m);
        console.info("DupeNukem: Ready to host managed [1].");
    }
    else if (window.__dupeNukem_Messenger_sendToHostMessage__ !== undefined) {
        console.info("DupeNukem: Ready to host managed [2].");
    }
})();

var __peerClosures__ = __peerClosures__ || {};

var __dupeNukem_invokeHostMethod__ =
    __dupeNukem_invokeHostMethod__ || function (entry) {
        const name = entry.name;
        const obsolete = entry.obsolete;
        if (obsolete === "obsolete") {
            console.warn(entry.obsoleteMessage);
        } else if (obsolete === "error") {
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

// CancellationToken declaration (obsoleted).
var CancellationToken =
    CancellationToken || function () {
        const warn = function () {
            console.warn("DupeNukem: CancellationToken is obsoleted, will be removed in future release. You have to switch to use AbortController and AbortSignal on ECMAScript standard instead.");
        }
        this.__ac__ = new AbortController();
        this.cancel = () => { warn(); this.__ac__.abort(); };
        warn();
    };

//////////////////////////////////////////////////

// Final initializer.
__dupeNukem_Messenger__.initialize__();

///////////////////////////////////////////////////////////////////////////////

