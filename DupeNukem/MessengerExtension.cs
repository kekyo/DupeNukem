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
using System.Collections.Generic;
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
                new ActionDescriptor(action, messenger.serializer),
                false);
        public static string RegisterAction<T0>(
            this Messenger messenger, Func<T0, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0>(action, messenger.serializer),
                false);
        public static string RegisterAction<T0, T1>(
            this Messenger messenger, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1>(action, messenger.serializer),
                false);
        public static string RegisterAction<T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1, T2>(action, messenger.serializer),
                false);
        public static string RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1, T2, T3>(action, messenger.serializer),
                false);

        public static void RegisterAction(
            this Messenger messenger, string name, Func<Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor(action, messenger.serializer),
                true);
        public static void RegisterAction<T0>(
            this Messenger messenger, string name, Func<T0, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0>(action, messenger.serializer),
                true);
        public static void RegisterAction<T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1>(action, messenger.serializer),
                true);
        public static void RegisterAction<T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1, T2>(action, messenger.serializer),
                true);
        public static void RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1, T2, T3>(action, messenger.serializer),
                true);

        ///////////////////////////////////////////////////////////////////////////////

        public static string RegisterFunc<TR>(
            this Messenger messenger, Func<Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR>(func, messenger.serializer),
                false);
        public static string RegisterFunc<TR, T0>(
            this Messenger messenger, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0>(func, messenger.serializer),
                false);
        public static string RegisterFunc<TR, T0, T1>(
            this Messenger messenger, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1>(func, messenger.serializer),
                false);
        public static string RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1, T2>(func, messenger.serializer),
                false);
        public static string RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1, T2, T3>(func, messenger.serializer),
                false);

        public static void RegisterFunc<TR>(
            this Messenger messenger, string name, Func<Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR>(func, messenger.serializer),
                true);
        public static void RegisterFunc<TR, T0>(
            this Messenger messenger, string name, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0>(func, messenger.serializer),
                true);
        public static void RegisterFunc<TR, T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1>(func, messenger.serializer),
                true);
        public static void RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1, T2>(func, messenger.serializer),
                true);
        public static void RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1, T2, T3>(func, messenger.serializer),
                true);

        ///////////////////////////////////////////////////////////////////////////////

        public static string RegisterDynamicMethod(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(method),
                new DynamicMethodDescriptor(method, messenger.serializer),
                false);
        public static string RegisterDynamicMethod<TR>(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(method),
                new DynamicMethodDescriptor<TR>(method, messenger.serializer),
                false);

        public static void RegisterDynamicMethod(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(
                name,
                new DynamicMethodDescriptor(method, messenger.serializer),
                true);
        public static void RegisterDynamicMethod<TR>(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(
                name,
                new DynamicMethodDescriptor<TR>(method, messenger.serializer),
                true);

        ///////////////////////////////////////////////////////////////////////////////

        public static void UnregisterMethod(
            this Messenger messenger, Delegate method) =>
            messenger.UnregisterMethod(Utilities.GetMethodFullName(method), false);

        public static void UnregisterMethod(
            this Messenger messenger, string name) =>
            messenger.UnregisterMethod(name, true);

        ///////////////////////////////////////////////////////////////////////////////

        private static IEnumerable<Utilities.MethodEntry> EnumerateRegisterObjectMethods(
            Messenger messenger, string? scopeName, object target, bool isFullName) =>
            Utilities.EnumerateTargetMethods(
                target, isFullName, messenger.memberAccessNamingStrategy).
            Select(entry => new Utilities.MethodEntry
            {
                Method = entry.Method,
                MethodName = scopeName != null ?
                    $"{scopeName}.{entry.MethodName}" :
                    entry.MethodName
            });

        public static string[] RegisterObject(
            this Messenger messenger, string? scopeName, object target) =>
            EnumerateRegisterObjectMethods(messenger, scopeName, target, false).
            Select(entry => messenger.RegisterMethod(
                entry.MethodName,
                new ObjectMethodDescriptor(target, entry.Method, messenger.serializer),
                true)).
            ToArray();

        public static string[] RegisterObject(
            this Messenger messenger, object target, bool isFullName = true) =>
            EnumerateRegisterObjectMethods(messenger, null, target, isFullName).
            Select(entry => messenger.RegisterMethod(
                entry.MethodName,
                new ObjectMethodDescriptor(target, entry.Method, messenger.serializer),
                true)).
            ToArray();

        public static void UnregisterObject(
            this Messenger messenger, string? scopeName, object target)
        {
            foreach (var entry in
                EnumerateRegisterObjectMethods(messenger, scopeName, target, false))
            {
                messenger.UnregisterMethod(entry.MethodName, true);
            }
        }

        public static void UnregisterObject(
            this Messenger messenger, object target, bool isFullName = true)
        {
            foreach (var entry in
                EnumerateRegisterObjectMethods(messenger, null, target, isFullName))
            {
                messenger.UnregisterMethod(entry.MethodName, true);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        public static Task InvokeClientFunctionAsync(
            this Messenger messenger, string functionName, params object[] args) =>
            messenger.InvokeClientFunctionAsync(default, functionName, args);

        public static Task<TR> InvokeClientFunctionAsync<TR>(
            this Messenger messenger, string functionName, params object[] args) =>
            messenger.InvokeClientFunctionAsync<TR>(default, functionName, args);
    }
}
