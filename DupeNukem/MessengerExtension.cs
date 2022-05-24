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
using System.Reflection;
using System.Threading.Tasks;

namespace DupeNukem
{
    public static class MessengerExtension
    {
        public static string RegisterAction(
            this Messenger messenger, Func<Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor(action, new(null), messenger),
                false, true);
        public static string RegisterAction<T0>(
            this Messenger messenger, Func<T0, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0>(action, new(null), messenger),
                false, true);
        public static string RegisterAction<T0, T1>(
            this Messenger messenger, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1>(action, new(null), messenger),
                false, true);
        public static string RegisterAction<T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1, T2>(action, new(null), messenger),
                false, true);
        public static string RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(action),
                new ActionDescriptor<T0, T1, T2, T3>(action, new(null), messenger),
                false, true);

        public static void RegisterAction(
            this Messenger messenger, string name, Func<Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor(action, new(null), messenger),
                true, true);
        public static void RegisterAction<T0>(
            this Messenger messenger, string name, Func<T0, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0>(action, new(null), messenger),
                true, true);
        public static void RegisterAction<T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1>(action, new(null), messenger),
                true, true);
        public static void RegisterAction<T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1, T2>(action, new(null), messenger),
                true, true);
        public static void RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(
                name,
                new ActionDescriptor<T0, T1, T2, T3>(action, new(null), messenger),
                true, true);

        ///////////////////////////////////////////////////////////////////////////////

        public static string RegisterFunc<TR>(
            this Messenger messenger, Func<Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR>(func, new(null), messenger),
                false, true);
        public static string RegisterFunc<TR, T0>(
            this Messenger messenger, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0>(func, new(null), messenger),
                false, true);
        public static string RegisterFunc<TR, T0, T1>(
            this Messenger messenger, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1>(func, new(null), messenger),
                false, true);
        public static string RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1, T2>(func, new(null), messenger),
                false, true);
        public static string RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(func),
                new FuncDescriptor<TR, T0, T1, T2, T3>(func, new(null), messenger),
                false, true);

        public static void RegisterFunc<TR>(
            this Messenger messenger, string name, Func<Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR>(func, new(null), messenger),
                true, true);
        public static void RegisterFunc<TR, T0>(
            this Messenger messenger, string name, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0>(func, new(null), messenger),
                true, true);
        public static void RegisterFunc<TR, T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1>(func, new(null), messenger),
                true, true);
        public static void RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1, T2>(func, new(null), messenger),
                true, true);
        public static void RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(
                name,
                new FuncDescriptor<TR, T0, T1, T2, T3>(func, new(null), messenger),
                true, true);

        ///////////////////////////////////////////////////////////////////////////////

        private static MethodMetadata GetMetadata(MethodInfo method) =>
            new MethodMetadata(method.GetCustomAttributes(typeof(ObsoleteAttribute), true) is ObsoleteAttribute[] oas ? oas.FirstOrDefault() : null);

        public static string RegisterDynamicMethod(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(method),
                new DynamicMethodDescriptor(method, GetMetadata(method.Method), messenger),
                false, true);
        public static string RegisterDynamicMethod<TR>(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(
                Utilities.GetMethodFullName(method),
                new DynamicMethodDescriptor<TR>(method, GetMetadata(method.Method), messenger),
                false, true);

        public static void RegisterDynamicMethod(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(
                name,
                new DynamicMethodDescriptor(method, GetMetadata(method.Method), messenger),
                true, true);
        public static void RegisterDynamicMethod<TR>(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(
                name,
                new DynamicMethodDescriptor<TR>(method, GetMetadata(method.Method), messenger),
                true, true);

        ///////////////////////////////////////////////////////////////////////////////

        public static void UnregisterMethod(
            this Messenger messenger, Delegate method) =>
            messenger.UnregisterMethod(Utilities.GetMethodFullName(method), false, true);

        public static void UnregisterMethod(
            this Messenger messenger, string name) =>
            messenger.UnregisterMethod(name, true, true);

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

        internal static string[] RegisterObject(
            this Messenger messenger, string? scopeName, object target, bool injectProxy) =>
            EnumerateRegisterObjectMethods(messenger, scopeName, target, false).
            Select(entry => messenger.RegisterMethod(
                entry.MethodName,
                new ObjectMethodDescriptor(target, entry.Method, GetMetadata(entry.Method), messenger),
                true, injectProxy)).
            ToArray();

        public static string[] RegisterObject(
            this Messenger messenger, string? scopeName, object target) =>
            EnumerateRegisterObjectMethods(messenger, scopeName, target, false).
            Select(entry => messenger.RegisterMethod(
                entry.MethodName,
                new ObjectMethodDescriptor(target, entry.Method, GetMetadata(entry.Method), messenger),
                true, true)).
            ToArray();

        public static string[] RegisterObject(
            this Messenger messenger, object target, bool isFullName = true) =>
            EnumerateRegisterObjectMethods(messenger, null, target, isFullName).
            Select(entry => messenger.RegisterMethod(
                entry.MethodName,
                new ObjectMethodDescriptor(target, entry.Method, GetMetadata(entry.Method), messenger),
                true, true)).
            ToArray();

        internal static void UnregisterObject(
            this Messenger messenger, string? scopeName, object target, bool injectedProxy)
        {
            foreach (var entry in
                EnumerateRegisterObjectMethods(messenger, scopeName, target, false))
            {
                messenger.UnregisterMethod(entry.MethodName, true, injectedProxy);
            }
        }

        public static void UnregisterObject(
            this Messenger messenger, string? scopeName, object target)
        {
            foreach (var entry in
                EnumerateRegisterObjectMethods(messenger, scopeName, target, false))
            {
                messenger.UnregisterMethod(entry.MethodName, true, true);
            }
        }

        public static void UnregisterObject(
            this Messenger messenger, object target, bool isFullName = true)
        {
            foreach (var entry in
                EnumerateRegisterObjectMethods(messenger, null, target, isFullName))
            {
                messenger.UnregisterMethod(entry.MethodName, true, true);
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
