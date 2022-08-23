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

#include <stdlib.h>

#include <vector>
#include <unordered_map>
#include <functional>
#include <stdexcept>
#include <mutex>

#include <ArduinoJson.h>

namespace DupeNukem
{
    template<typename T> class Promise
    {
    public:
        using resolve_type = std::function<void(const T&)>;
        using reject_type = std::function<void(const std::exception&)>;
        using final_type = std::function<void(void)>;

    private:
        enum States {
            Pending,
            Resolved,
            Rejected,
            Finalized
        };

        using locker_type = std::lock_guard<std::mutex>;

        std::vector<resolve_type> resolvers__;
        std::vector<reject_type> rejectors__;
        std::vector<final_type> finalizers__;

        std::unique_ptr<std::mutex> mutexPtr__;
        volatile States state__ = Pending;

        T value__;
        std::exception ex__;

        void finalizeAllWhenRequired() {
            locker_type locker(mutexPtr__.get());
            if (finalizers__.size() >= 1) {
                state__ = Finalized;
                for (auto& finalize : finalizers__) {
                    finalize();
                }
            }
        }

    public:
        Promise() :
            mutexPtr__(new std::mutex()) {
        }

        Promise(const Promise<T>& rhs) {
            *this = rhs;
        }

        Promise(Promise<T>&& rhs) noexcept {
            *this = rhs;
        }

        Promise<T>& operator =(const Promise& rhs) {
            mutexPtr__ = rhs.mutexPtr__;
            locker_type locker(mutexPtr__.get());

            resolvers__ = rhs.resolvers__;
            rejectors__ = rhs.rejectors__;
            finalizers__ = rhs.finalizers__;
            state__ = rhs.state__;
            value__ = rhs.value__;
            ex__ = rhs.ex__;
            return *this;
        }

        Promise<T>& operator =(Promise&& rhs) noexcept {
            mutexPtr__ = std::move(rhs.mutexPtr__);
            locker_type locker(mutexPtr__.get());

            resolvers__ = std::move(rhs.resolvers__);
            rejectors__ = std::move(rhs.rejectors__);
            finalizers__ = std::move(rhs.finalizers__);
            state__ = std::move(rhs.state__);
            value__ = std::move(rhs.value__);
            ex__ = std::move(rhs.ex__);
            return *this;
        }

        void resolved(const T& value) {
            locker_type locker(mutexPtr__.get());
            if (state__ == Pending) {
                value__ = value;
                state__ = Resolved;
                for (auto& resolve : resolvers__) {
                    resolve(value__);
                }
                finalizeAllWhenRequired();
            }
        }

        void rejected(const std::exception& ex) {
            locker_type locker(mutexPtr__.get());
            if (state__ == Pending) {
                ex__ = ex;
                state__ = Rejected;
                for (auto& reject : rejectors__) {
                    reject(value__);
                }
                finalizeAllWhenRequired();
            }
        }

        Promise<T>& then(const resolve_type& resolve) {
            locker_type locker(mutexPtr__.get());
            switch (state__) {
            case Pending:
                resolvers__.push_back(resolve);
                break;
            case Resolved:
                resolve(value__);
                break;
            case Finalized:
                throw std::logic_error("State already made finalized.");
                break;
            }
            return *this;
        }

        Promise<T>& caught(const reject_type& reject) {
            locker_type locker(mutexPtr__.get());
            switch (state__) {
            case Pending:
                rejectors__.push_back(reject);
                break;
            case Rejected:
                reject(ex__);
                break;
            case Finalized:
                throw std::logic_error("State already made finalized.");
                break;
            }
            return *this;
        }

        Promise<T> & final(const final_type& finalize) {
            locker_type locker(mutexPtr__.get());
            switch (state__) {
            case Pending:
                finalizers__.push_back(finalize);
                break;
            case Resolved:
            case Rejected:
                state__ = Finalized;
            default:
                finalize();
                break;
            }
            return *this;
        }
    };

    //////////////////////////////////////////////////////////

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

        PeerInvocationException(
            const PeerInvocationException&) = default;
        PeerInvocationException(
            PeerInvocationException&&) noexcept = default;

        PeerInvocationException& operator =(
            const PeerInvocationException&) = default;
        PeerInvocationException& operator =(
            PeerInvocationException&&) noexcept = default;

        const char* name() const {
            return name__.c_str();
        }

        virtual const char* what() const noexcept {
            return message__.c_str();
        }

        const char* detail() const {
            return detail__.c_str();
        }
    };

    //////////////////////////////////////////////////////////

    template <typename T> class Optional
    {
    private:
        bool has__;
        T value__;
    public:
        constexpr Optional() noexcept :
            has__(false) {
        }

        constexpr Optional(const T& value) :
            has__(true), value__(value) {
        }

        constexpr Optional(T&& value) noexcept :
            has__(true), value__(std::move(value)) {
        }

        constexpr Optional(const Optional<T>& rhs) = default;
        constexpr Optional(Optional<T>&& rhs) noexcept = default;

        Optional<T>& operator =(const T& rhs) {
            has__ = true;
            value__ = rhs;
            return *this;
        }
        Optional<T>& operator =(T&& rhs) noexcept {
            has__ = true;
            value__ = std::move(rhs.value__);
            return *this;
        }

        Optional<T>& operator =(const Optional<T>& rhs) = default;
        Optional<T>& operator =(Optional<T>&& rhs) noexcept = default;

        void reset() {
            has__ = false;
            value__ = std::move(T());
        }

        constexpr explicit operator bool() const noexcept {
            return has__;
        }
        T* operator ->() {
            return &value__;
        }
        constexpr const T* operator ->() const {
            return &value__;
        }
        T& operator *() {
            return value__;
        }
        constexpr const T& operator *() const {
            return value__;
        }
    };

    //////////////////////////////////////////////////////////

    template <typename TJsonAllocator> class Messenger__
    {
    public:
        using log_type = std::function<void(const String&)>;
        using sendToHostMessage_type = std::function<void(const String&)>;
        using promise_type = Promise<const JsonVariant>;
        using function_type = std::function<promise_type(JsonArray)>;

    private:
        using jsonDocument_type = BasicJsonDocument<TJsonAllocator>;
        using locker_type = std::lock_guard<std::mutex>;

        const log_type log__;
        const sendToHostMessage_type sendToHostMessage__;

        std::unordered_map<const String, function_type> functions__;
        std::unordered_map<const String, promise_type> suspendings__;

        std::mutex functionsMutex__;
        std::mutex suspendingsMutex__;

        int32_t id__ = 0;
        bool debugLog__ = false;

        template <typename T> static Optional<T> find(
            std::unordered_map<const String, T>& map,
            const String& key,
            std::mutex& mutex) {
            locker_type locker(mutex);
            auto iter = map.find(key);
            return iter != map.end() ?
                Optional<T>(*iter) :
                Optional<T>();
        }

        void sendMessageToHost(
            JsonDocument& messageJson,
            const String& id,
            const char* pType) {
            messageJson["id"] = id;
            messageJson["type"] = pType;

            String messageString;
            serializeJson(messageJson, messageString);

            sendToHostMessage__(messageString);
        }

        void sendExceptionToHost(
            const String& id,
            const String& name,
            const String& message) const {
            jsonDocument_type messageJson;

            auto body = messageJson.createNestedObject("body");
            body["name"] = name;
            body["message"] = message;
            body["detail"] = message;

            sendMessageToHost(messageJson, id, "failed");
        }

        void sendExceptionToHost(
            const String& id,
            const std::exception& ex) const {
            sendExceptionToHost(id, "exception", ex.what());
        }

        void arrivedHostMesssage(const String& jsonString) {
            try {
                jsonDocument_type messageJson;
                deserializeJson(messageJson, jsonString);

                const String& id = messageJson["id"];
                const String& type = messageJson["type"];
                const JsonVariant& body = messageJson["body"];

                if (type == "succeeded") {
                    log__(String("DupeNukem: succeeded: ") + id);
                    auto promise = find(suspendings__, id, suspendingsMutex__);
                    if (promise) {
                        suspendings__.erase(id);

                        promise->resolved(body);
                    }
                    else {
                        log__("DupeNukem: suprious message received: " + jsonString);
                    }
                }
                else if (type == "failed") {
                    log__(String("DupeNukem: failed: ") + id);
                    auto promise = find(suspendings__, id, suspendingsMutex__);
                    if (promise) {
                        suspendings__.erase(id);

                        const String& name = body["name"];
                        const String& message = body["message"];
                        const String& detail = body["detail"];
                        PeerInvocationException ex(name, message, detail);

                        promise->rejected(ex);
                    }
                    else {
                        log__("DupeNukem: suprious message received: " + jsonString);
                    }
                }
                else if (type == "invoke")
                {
                    try {
                        const String& name = body["name"];
                        log__("DupeNukem: invoke: " + name + "(...)");

                        auto function = find(functions__, name, functionsMutex__);
                        if (function) {
                            const JsonArray& args = body["args"];

                            (*function)(args).
                                then([this, id](const JsonVariant& result) {
                                jsonDocument_type messageJson;
                                messageJson["body"] = result;
                                sendMessageToHost(messageJson, id, "succeeded");
                                    }).
                                caught([this, id](const std::exception& ex) {
                                        sendExceptionToHost(id, ex);
                                    });
                        }
                        else {
                            sendExceptionToHost(
                                id,
                                "invalidFunctionName",
                                "Function \"" + name + "\" is not found.");
                        }
                    }
                    catch (const std::exception& ex) {
                        sendExceptionToHost(id, "exception", ex.what());
                    }
                }
            }
            catch (const std::exception& ex) {
                log__(String("DupeNukem: unknown error: ") + ex.what() + ": " + jsonString);
            }
        }

    public:
        Messenger__(
            const log_type& log,
            const sendToHostMessage_type& sendToHostMessage) :
            log__(log), sendToHostMessage__(sendToHostMessage) {
            jsonDocument_type messageJson;
            messageJson.createNestedObject("body");

            sendMessageToHost(messageJson, "ready", "control");

            log__("DupeNukem: Ready by host managed.");
        }

        void registerFunction(const char* pName, const function_type& function) {
            locker_type locker(functionsMutex__);
            functions__[pName] = function;
        }

        void unregisterFunction(const char* pName) {
            locker_type locker(functionsMutex__);
            functions__.erase(pName);
        }

        promise_type invokeHostMethod(const char* pMethodName, const JsonArray& args) {
            promise_type promise;

            String id = "client_";
            id += id__++;

            {
                locker_type locker(suspendingsMutex__);
                suspendings__[id] = promise;
            }

            jsonDocument_type messageJson;
            auto body = messageJson.createNestedObject("body");
            body["name"] = pMethodName;
            body["args"] = args;

            try {
                sendMessageToHost(messageJson, id, "invoke");
            }
            catch (const std::exception& ex) {
                promise.rejected(ex);
            }

            return promise;
        }
    };

    //////////////////////////////////////////////////////////

    // For ArduinoJson
    class DefaultAllocator
    {
    public:
        void* allocate(size_t size) {
            return malloc(size);
        }

        void deallocate(void* ptr) {
            free(ptr);
        }

        void* reallocate(void* ptr, size_t new_size) {
            return realloc(ptr, new_size);
        }
    };

    using Messenger = Messenger__<DefaultAllocator>;
}

#endif
