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
                        const ti = ne.slice(0, ne.length - 1).reduce(function (o, n) { return o[n]; }, window);
                        ti[ne[ne.length - 1]].
                            apply(ti, message.body.args).
                            then(result => this.sendToHostMessage__(JSON.stringify({ id: message.id, type: "succeeded", body: result, }))).
                            catch(e => this.sendToHostMessage__(JSON.stringify({ id: message.id, type: "failed", body: { name: e.name, message: e.message, detail: e.toString() }, })));
                    }
                    catch (e) {
                        this.sendToHostMessage__(JSON.stringify({ id: message.id, type: "failed", body: { name: e.name, message: e.message, detail: e.toString() }, }));
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
};

const dupeNukem_Messenger__ =
    new DupeNukem_Messenger__(window.dupeNukem_Messenger_hookup);

//////////////////////////////////////////////////

function invokeHostMethod(methodName) {
    const args = new Array(arguments.length - 1);
    for (let i = 0; i < args.length; i++) {
        args[i] = arguments[i + 1];
    }
    return dupeNukem_Messenger__.invokeHostMethod__(methodName, args);
}

invokeHostMethod("dupeNukem_Messenger_ready__");

///////////////////////////////////////////////////////////////////////////////

