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

using DupeNukem.Internal;
using System;
using System.Threading.Tasks;

namespace DupeNukem
{
    public static class MessengerExtension
    {
        public static void RegisterAction(
            this Messenger messenger, Func<Task> action) =>
            messenger.RegisterMethod(Utilities.GetMethodName(action), new ActionDescriptor(action));
        public static void RegisterAction<T0>(
            this Messenger messenger, Func<T0, Task> action) =>
            messenger.RegisterMethod(Utilities.GetMethodName(action), new ActionDescriptor<T0>(action));
        public static void RegisterAction<T0, T1>(
            this Messenger messenger, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(Utilities.GetMethodName(action), new ActionDescriptor<T0, T1>(action));
        public static void RegisterAction<T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(Utilities.GetMethodName(action), new ActionDescriptor<T0, T1, T2>(action));
        public static void RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(Utilities.GetMethodName(action), new ActionDescriptor<T0, T1, T2, T3>(action));

        public static void RegisterAction(
            this Messenger messenger, string name, Func<Task> action) =>
            messenger.RegisterMethod(name, new ActionDescriptor(action));
        public static void RegisterAction<T0>(
            this Messenger messenger, string name, Func<T0, Task> action) =>
            messenger.RegisterMethod(name, new ActionDescriptor<T0>(action));
        public static void RegisterAction<T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task> action) =>
            messenger.RegisterMethod(name, new ActionDescriptor<T0, T1>(action));
        public static void RegisterAction<T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task> action) =>
            messenger.RegisterMethod(name, new ActionDescriptor<T0, T1, T2>(action));
        public static void RegisterAction<T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task> action) =>
            messenger.RegisterMethod(name, new ActionDescriptor<T0, T1, T2, T3>(action));

        ///////////////////////////////////////////////////////////////////////////////

        public static void RegisterFunc<TR>(
            this Messenger messenger, Func<Task<TR>> func) =>
            messenger.RegisterMethod(Utilities.GetMethodName(func), new FuncDescriptor<TR>(func));
        public static void RegisterFunc<TR, T0>(
            this Messenger messenger, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(Utilities.GetMethodName(func), new FuncDescriptor<TR, T0>(func));
        public static void RegisterFunc<TR, T0, T1>(
            this Messenger messenger, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(Utilities.GetMethodName(func), new FuncDescriptor<TR, T0, T1>(func));
        public static void RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(Utilities.GetMethodName(func), new FuncDescriptor<TR, T0, T1, T2>(func));
        public static void RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(Utilities.GetMethodName(func), new FuncDescriptor<TR, T0, T1, T2, T3>(func));

        public static void RegisterFunc<TR>(
            this Messenger messenger, string name, Func<Task<TR>> func) =>
            messenger.RegisterMethod(name, new FuncDescriptor<TR>(func));
        public static void RegisterFunc<TR, T0>(
            this Messenger messenger, string name, Func<T0, Task<TR>> func) =>
            messenger.RegisterMethod(name, new FuncDescriptor<TR, T0>(func));
        public static void RegisterFunc<TR, T0, T1>(
            this Messenger messenger, string name, Func<T0, T1, Task<TR>> func) =>
            messenger.RegisterMethod(name, new FuncDescriptor<TR, T0, T1>(func));
        public static void RegisterFunc<TR, T0, T1, T2>(
            this Messenger messenger, string name, Func<T0, T1, T2, Task<TR>> func) =>
            messenger.RegisterMethod(name, new FuncDescriptor<TR, T0, T1, T2>(func));
        public static void RegisterFunc<TR, T0, T1, T2, T3>(
            this Messenger messenger, string name, Func<T0, T1, T2, T3, Task<TR>> func) =>
            messenger.RegisterMethod(name, new FuncDescriptor<TR, T0, T1, T2, T3>(func));

        ///////////////////////////////////////////////////////////////////////////////

        public static void RegisterDynamicMethod(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(Utilities.GetMethodName(method), new DynamicMethodDescriptor(method));
        public static void RegisterDynamicMethod<TR>(
            this Messenger messenger, Delegate method) =>
            messenger.RegisterMethod(Utilities.GetMethodName(method), new DynamicMethodDescriptor<TR>(method));

        public static void RegisterDynamicMethod(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(name, new DynamicMethodDescriptor(method));
        public static void RegisterDynamicMethod<TR>(
            this Messenger messenger, string name, Delegate method) =>
            messenger.RegisterMethod(name, new DynamicMethodDescriptor<TR>(method));

        ///////////////////////////////////////////////////////////////////////////////

        public static void UnregisterMethod(
            this Messenger messenger, Delegate method) =>
            messenger.UnregisterMethod(Utilities.GetMethodName(method));

        ///////////////////////////////////////////////////////////////////////////////

        public static Task InvokeClientFunctionAsync(
            this Messenger messenger, string functionName, params object[] args) =>
            messenger.InvokeClientFunctionAsync(default, functionName, args);

        public static Task<TR> InvokeClientFunctionAsync<TR>(
            this Messenger messenger, string functionName, params object[] args) =>
            messenger.InvokeClientFunctionAsync<TR>(default, functionName, args);
    }
}
