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

using DupeNukem.Internal;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DupeNukem
{
    public static class MessengerExtension
    {
        public static string RegisterAction(
            this Messenger messenger, Func<Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor(action, messenger.serializer));
        public static string RegisterAction<T0>(
            this Messenger messenger, Func<T0, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0>(action, messenger.serializer));
        public static string RegisterAction<T0, T1>(
            this Messenger messenger, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1>(action, messenger.serializer));
        public static string RegisterAction<T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1, T2>(action, messenger.serializer));
        public static string RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1, T2, T3>(action, messenger.serializer));

        public static void RegisterAction(
            this Messenger messenger, string name, Func<Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor(action, messenger.serializer));
        public static void RegisterAction<T0>(
            this Messenger messenger, string name, Func<T0, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0>(action, messenger.serializer));
        public static void RegisterAction<T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1>(action, messenger.serializer));
        public static void RegisterAction<T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1, T2>(action, messenger.serializer));
        public static void RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1, T2, T3>(action, messenger.serializer));

        ///////////////////////////////////////////////////////////////////////////////

        public static string RegisterFunc<TR>(
            this Messenger messenger, Func<Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR>(func, messenger.serializer));
        public static string RegisterFunc<TR, T0>(
            this Messenger messenger, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0>(func, messenger.serializer));
        public static string RegisterFunc<TR, T0, T1>(
            this Messenger messenger, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1>(func, messenger.serializer));
        public static string RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1, T2>(func, messenger.serializer));
        public static string RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1, T2, T3>(func, messenger.serializer));

        public static void RegisterFunc<TR>(
            this Messenger messenger, string name, Func<Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR>(func, messenger.serializer));
        public static void RegisterFunc<TR, T0>(
            this Messenger messenger, string name, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0>(func, messenger.serializer));
        public static void RegisterFunc<TR, T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1>(func, messenger.serializer));
        public static void RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1, T2>(func, messenger.serializer));
        public static void RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1, T2, T3>(func, messenger.serializer));

        ///////////////////////////////////////////////////////////////////////////////

        public static string RegisterDynamicMethod(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(method),
                new DynamicMethodDescriptor(method, messenger.serializer));
        public static string RegisterDynamicMethod<TR>(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(method),
                new DynamicMethodDescriptor<TR>(method, messenger.serializer));

        public static void RegisterDynamicMethod(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(
                name,
                new DynamicMethodDescriptor(method, messenger.serializer));
        public static void RegisterDynamicMethod<TR>(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(
                name,
                new DynamicMethodDescriptor<TR>(method, messenger.serializer));

        ///////////////////////////////////////////////////////////////////////////////

        public static void UnregisterMethod(
            this Messenger messenger, Delegate method) =>
            messenger.UnregisterMethod(Utilities.GetMethodFullName(method));

        ///////////////////////////////////////////////////////////////////////////////

        public static string[] RegisterObject(
            this Messenger messenger, string? scopeName, object target) =>
            Utilities.EnumerateTargetMethods(target).
            Select(entry => messenger.RegisterMethod(
                scopeName != null ?
                    $"{scopeName}.{entry.MethodName}" :
                    entry.MethodName,
                new ObjectMethodDescriptor(target, entry.Method, messenger.serializer))).
            ToArray();

        public static string[] RegisterObject(
            this Messenger messenger, object target, bool isFullName = true) =>
            RegisterObject(
                messenger,
                isFullName ? Utilities.GetFullName(target.GetType()) : null,
                target);

        public static void UnregisterObject(
            this Messenger messenger, string? scopeName, object target)
        {
            foreach (var entry in Utilities.EnumerateTargetMethods(target))
            {
                var methodName = scopeName != null ?
                    $"{scopeName}.{entry.MethodName}" :
                    entry.MethodName;
                messenger.UnregisterMethod(methodName);
            }
        }

        public static void UnregisterObject(
            this Messenger messenger, object target, bool isFullName = true) =>
            UnregisterObject(
                messenger,
                isFullName ? Utilities.GetFullName(target.GetType()) : null,
                target);

        ///////////////////////////////////////////////////////////////////////////////

        public static Task InvokeClientFunctionAsync(
            this Messenger messenger, string functionName, params object[] args) =>
            messenger.InvokeClientFunctionAsync(default, functionName, args);

        public static Task<TR> InvokeClientFunctionAsync<TR>(
            this Messenger messenger, string functionName, params object[] args) =>
            messenger.InvokeClientFunctionAsync<TR>(default, functionName, args);
    }
}
