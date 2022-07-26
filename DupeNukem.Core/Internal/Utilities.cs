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

using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem.Internal
{
    internal static class Utilities
    {
        private static readonly Dictionary<Type, object?> defaultValues = new();

        private abstract class DefaultValue
        {
            public abstract object? GetDefaultValue();
        }

        private sealed class DefaultValue<T> : DefaultValue
        {
            public override object? GetDefaultValue() =>
                default(T);
        }

        public static object? GetDefaultValue(Type type)
        {
            lock (defaultValues)
            {
                if (!defaultValues.TryGetValue(type, out var value))
                {
                    value = ((DefaultValue)Activator.CreateInstance(
                        typeof(DefaultValue<>).MakeGenericType(type))!).
                        GetDefaultValue();
                }
                return value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        public static string GetName(Type type)
        {
            var tn = type.Name.LastIndexOf('`') is { } index && index >= 0 ?
                type.Name.Substring(0, index) : type.Name;
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
            var ti = type.GetTypeInfo();
            if (ti.IsGenericType)
            {
                var gtns = Join(
                    ",", ti.GenericTypeArguments.Select(GetName));
                return $"{tn}<{gtns}>";
            }
            else
            {
                return $"{tn}";
            }
#else
            if (type.IsGenericType)
            {
                var gtns = Join(
                    ",", type.GetGenericArguments().Select(GetName));
                return $"{tn}<{gtns}>";
            }
            else
            {
                return $"{tn}";
            }
#endif
        }

        public static string GetFullName(Type type)
        {
            var ns = type.DeclaringType is { } dt ?
                GetFullName(dt) : type.Namespace;
            var tn = type.Name.LastIndexOf('`') is { } index && index >= 0 ?
                type.Name.Substring(0, index) : type.Name;
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
            var ti = type.GetTypeInfo();
            if (ti.IsGenericType)
            {
                var gtns = Join(
                    ",", ti.GenericTypeArguments.Select(GetFullName));
                return $"{ns}.{tn}<{gtns}>";
            }
            else
            {
                return $"{ns}.{tn}";
            }
#else
            if (type.IsGenericType)
            {
                var gtns = Join(
                    ",", type.GetGenericArguments().Select(GetFullName));
                return $"{ns}.{tn}<{gtns}>";
            }
            else
            {
                return $"{ns}.{tn}";
            }
#endif
        }

        public static string GetName(MethodInfo method)
        {
            var mn = method.Name.LastIndexOf('`') is { } index && index >= 0 ?
                method.Name.Substring(0, index) : method.Name;
            if (method.IsGenericMethod)
            {
                var gtns = Join(
                    ",", method.GetGenericArguments().Select(GetName));
                return $"{mn}<{gtns}>";
            }
            else
            {
                return $"{mn}";
            }
        }

        public static string GetFullName(MethodInfo method, Type? runtimeType)
        {
            var tn = (runtimeType != null) ?
                GetFullName(runtimeType) :
                method.DeclaringType is { } dt ?
                    GetFullName(dt) : "global";
            var mn = method.Name.LastIndexOf('`') is { } index && index >= 0 ?
                method.Name.Substring(0, index) : method.Name;
            if (method.IsGenericMethod)
            {
                var gtns = Join(
                    ",", method.GetGenericArguments().Select(GetFullName));
                return $"{tn}.{mn}<{gtns}>";
            }
            else
            {
                return $"{tn}.{mn}";
            }
        }

        public static string GetMethodFullName(Delegate dlg) =>
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
            GetFullName(dlg.GetMethodInfo(), dlg.Target?.GetType());
#else
            GetFullName(dlg.Method, dlg.Target?.GetType());
#endif

        ///////////////////////////////////////////////////////////////////////////////

        public struct MethodEntry
        {
            public string MethodName;
            public MethodInfo Method;
        }

        public static string GetConvertedName(
            this NamingStrategy ns, string name, bool hasSpecifiedName = false) =>
            Join(".", name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).
                Select(element => ns.GetPropertyName(element, hasSpecifiedName)));

        private static string GetMethodName(
            MethodInfo method, Type? runtimeType, string? specifiedName, bool isFullName, NamingStrategy ns)
        {
            if (IsNullOrWhiteSpace(specifiedName))
            {
                var methodName = isFullName ?
                    GetFullName(method, runtimeType) :
                    GetName(method);
                return ns.GetConvertedName(methodName);
            }
            else
            {
                var scopeName = (runtimeType != null) ?
                    ns.GetConvertedName(GetFullName(runtimeType)) :
                    method.DeclaringType is { } dt ?
                        ns.GetConvertedName(GetFullName(dt)) : "global";
                return isFullName ? $"{scopeName}.{specifiedName}" : specifiedName!;
            }
        }

        public static IEnumerable<MethodEntry> EnumerateTargetMethods(
            object target, bool isFullName, NamingStrategy memberAccessNamingStrategy) =>
            target.GetType().
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
                GetTypeInfo().
                Traverse(t => t.BaseType.GetTypeInfo()).
                SelectMany(t => new[] { t }.Concat(t.ImplementedInterfaces.Select(t => t.GetTypeInfo()))).
                Distinct().   // Exclude overriding interface types.
                SelectMany(t => t.DeclaredMethods).
                Where(m => !m.IsStatic).
#else
                Traverse(t => t.BaseType).
                SelectMany(t => new[] { t }.Concat(t.GetInterfaces())).
                Distinct().   // Exclude overriding interface types.
                SelectMany(t => t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)).
#endif
                Select(method => new MethodEntry
                {
                    Method = method,
                    MethodName = method.GetCustomAttributes(typeof(CallableTargetAttribute), true) is object[] cas &&
                        cas.Length >= 1 && cas[0] is CallableTargetAttribute a ?
                            GetMethodName(method, target.GetType(), a.Name, isFullName, memberAccessNamingStrategy) :
                            null!,
                }).
                Where(entry => entry.MethodName != null);

        ///////////////////////////////////////////////////////////////////////////////

        public static bool SafeTryGetValue<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key, out TValue value)
            where TKey : notnull
        {
            lock (dict)
            {
                return dict.TryGetValue(key, out value!);
            }
        }

        public static void SafeAdd<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key, TValue value)
            where TKey : notnull
        {
            lock (dict)
            {
                dict.Add(key, value);
            }
        }

        public static bool SafeRemove<TKey, TValue>(
            this Dictionary<TKey, TValue> dict, TKey key)
            where TKey : notnull
        {
            lock (dict)
            {
                return dict.Remove(key);
            }
        }

        public static IEnumerable<T> Traverse<T>(this T value, Func<T, T?> selector)
            where T : class
        {
            T? current = value;
            while (current != null)
            {
                yield return current;
                current = selector(current);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////

        public static Task CompletedTask =>
#if NET35 || NET40
            TaskEx.FromResult(0);
#elif NET45 || NET451 || NET452 || NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
            Task.FromResult(0);
#else
            Task.CompletedTask;
#endif

#if DEBUG
        public static async Task WhenAll(IEnumerable<Task> tasks)
        {
            foreach (var task in tasks)
            {
                await task.ConfigureAwait(false);
            }
        }
#else
        public static Task WhenAll(IEnumerable<Task> tasks) =>
#if NET35 || NET40
            TaskEx.WhenAll(tasks);
#else
            Task.WhenAll(tasks);
#endif
#endif

#if DEBUG
        public static async Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks)
        {
            var results = new List<T>();
            foreach (var task in tasks)
            {
                results.Add(await task.ConfigureAwait(false));
            }
            return results.ToArray();
        }
#else
        public static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
#if NET35 || NET40
            TaskEx.WhenAll(tasks);
#else
            Task.WhenAll(tasks);
#endif
#endif

        public static Task Delay(int msec) =>
#if NET35 || NET40
            TaskEx.Delay(msec);
#else
            Task.Delay(msec);
#endif

        public static bool IsNullOrWhiteSpace(string? str) =>
#if NET35
            string.IsNullOrEmpty(str) || str!.Trim().Length == 0;
#else
            string.IsNullOrWhiteSpace(str);
#endif

        public static string Join<T>(string separator, IEnumerable<T> enumerable) =>
#if NET35
            string.Join(separator, enumerable.Select(v => v?.ToString() ?? string.Empty).ToArray());
#else
            string.Join(separator, enumerable);
#endif

        public static readonly TimeSpan InfiniteTimeSpan =
#if NET35 || NET40
            new TimeSpan(0, 0, 0, 0, -1);
#else
            Timeout.InfiniteTimeSpan;
#endif

        ///////////////////////////////////////////////////////////////////////////////

        public sealed class DelegatedEqualityComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, int> getHashCode;
            private readonly Func<T, T, bool> equals;

            public DelegatedEqualityComparer(
                Func<T, int> getHashCode, Func<T, T, bool> equals)
            {
                this.getHashCode = getHashCode;
                this.equals = equals;
            }

            public int GetHashCode(T? obj) =>
                this.getHashCode(obj!);

            public bool Equals(T? x, T? y) =>
                this.equals(x!, y!);
        }

        ///////////////////////////////////////////////////////////////////////////////

        public static BindAwaitable Bind(this SynchronizationContext? context) =>
            new BindAwaitable(context);

        public struct BindAwaitable
        {
            private readonly SynchronizationContext? context;

            public BindAwaitable(SynchronizationContext? context) =>
                this.context = context;

            public BindAwaiter GetAwaiter() =>
                new BindAwaiter(this.context);
        }

        public sealed class BindAwaiter : INotifyCompletion
        {
            private SynchronizationContext? context;

            public BindAwaiter(SynchronizationContext? context) =>
                this.context = context;

            public bool IsCompleted =>
                this.context == null;

            public void OnCompleted(Action continuation)
            {
                if (Interlocked.CompareExchange(
                    ref this.context, null, this.context) is { } context &&
                    !object.ReferenceEquals(context, SynchronizationContext.Current))
                {
                    context.Post(c => ((Action)c!)(), continuation);
                }
                else
                {
                    continuation();
                }
            }

            public void GetResult() =>
                Debug.Assert(this.IsCompleted);
        }
    }
}
