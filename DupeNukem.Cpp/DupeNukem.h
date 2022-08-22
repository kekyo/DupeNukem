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

#ifndef DUPENUKEM_H__
#define DUPENUKEM_H__

#pragma once

// Core dispatcher for c++ side.

#include <unordered_map>
#include <functional>
#include <stdexcept>

#include <ArduinoJson.h>

namespace DupeNukem
{
    class PeerInvocationException : public std::exception
    {
    private:
        const String name__;
        const String message__;
        const String detail__;
        
    public:
        PeerInvocationException(
            const String& name,
            const String& message,
            const String& detail) :
            name__(name), message__(message), detail__(detail) {
        }

        const char* name() {
            return name__.c_str();
        }

        virtual const char* what() {
            return message__.c_str();
        }

        const char* detail() {
            return detail__.c_str();
        }
    };

    class Message
    {
    private:
        const int32_t id__;
    public:
        Message(const int32_t id) :
            id__(id) {
        }

        int32_t id() const {
            return id__;
        }
    };

    class Descriptor
    {
    private:
        const std::function<void (const JsonVariant&)> resolve__;
        const std::function<void (const std::exception&)> reject__;
    public:
        Descriptor(
            const std::function<void (const JsonVariant&)>& resolve,
            const std::function<void (const std::exception&)>& reject) :
            resolve__(resolve), reject__(reject) {
        }

        Descriptor(const Descriptor& rhs) :
            resolve__(rhs.resolve__), reject__(rhs.reject__) {
        }

        void resolve(const JsonVariant& value) const {
            resolve__(value);
        }

        void reject(const std::exception& ex) const {
            reject__(ex);
        }
    };

    class Messenger
    {
    private:
        const std::function<void (const String&)> log__;
        const std::function<void (const String&)> sendToHostMessage__;
        const std::unordered_map<int32_t, Descriptor> suspendings__;
        int32_t id__ = 0;
        bool debugLog__ = false;

        void sendExceptionToHost__(
            const Message& message,
            const std::exception& exceptionBody) const {

            StaticJsonDocument<50> messageJson;
            messageJson["id"] = message.id();
            messageJson["type"] = "failed";
            messageJson["body"] = exceptionBody.what();

            String messageString;
            serializeJson(messageJson, messageString);

            sendToHostMessage__(messageString);
        };

        void arrivedHostMesssage__(const String& jsonString) {
            try {
                DynamicJsonDocument messageJson(200);
                deserializeJson(messageJson, jsonString);

                const auto id = messageJson["id"].as<int32_t>();
                const auto type = messageJson["type"].as<String>();
                const auto body = messageJson["body"];

                if (type == "succeeded") {
                    log__(String("DupeNukem: succeeded: ") + id);
                    const auto iter = suspendings__.find(id);
                    if (iter != suspendings__.end()) {
                        const auto successorDescriptor = iter->second;
                        suspendings__.erase(id);
                        successorDescriptor.resolve(body);
                    }
                    else {
                        log__("DupeNukem: suprious message received: " + jsonString);
                    }
                }
                else if (type == "failed") {
                    log__(String("DupeNukem: failed: ") + id);
                    const auto iter = suspendings__.find(id);
                    if (iter != suspendings__.end()) {
                        const auto failureDescriptor = iter->second;
                        suspendings__.erase(id);
                        const auto name = body["name"].as<String>();
                        const auto message = body["message"].as<String>();
                        const auto detail = body["detail"].as<String>();
                        PeerInvocationException ex(name, message, detail);
                        failureDescriptor.reject(ex);
                    }
                    else {
                        log__("DupeNukem: suprious message received: " + jsonString);
                    }
                }
                else if (type == "invoke")
                {
                    try {
                        const auto name = body["name"].as<String>();

                        log__("DupeNukem: invoke: " + name + "(...)");
                        const ne = name.split(".");
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
                                    then(result => window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify({ id: message.id, type: "succeeded", body: result, }))).
                                    catch(e => this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), }));
                            }
                        }
                    }
                    catch (e) {
                        this.sendExceptionToHost__(message, { name: e.name, message: e.message, detail: e.toString(), });
                    }
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
                }
            }
            catch (e) {
                console.warn("DupeNukem: unknown error: " + e.message + ": " + jsonString);
            }
        };

        this.invokeHostMethod__ = (name, args) => {
            return new Promise((resolve, reject) => {
                const id = "client_" + (this.id__++);
                try {
                    const descriptor = { resolve: resolve, reject: reject, };
                    this.suspendings__.set(id, descriptor);
                    window.__dupeNukem_Messenger_sendToHostMessage__(JSON.stringify({ id: id, type: "invoke", body: { name: name, args: args, }, }));
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

    public:
        Messenger(
            const std::function<void (const String&)>& log,
            const std::function<void(const String&)>& sendToHostMessage) :
            log__(log), sendToHostMessage__(sendToHostMessage) {

            StaticJsonDocument<50> message;
            message["id"] = "ready";
            message["type"] = "control";
            message["body"] = nullptr;

            String messageString;
            serializeJson(messageJson, messageString);

            sendToHostMessage__(messageString);

            log__("DupeNukem: Ready by host managed.");
        }
    };
}

#endif
