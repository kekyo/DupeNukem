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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        }

        public static string GetFullName(Type type)
        {
            var ns = type.DeclaringType is { } dt ?
                GetFullName(dt) : type.Namespace;
            var tn = type.Name.LastIndexOf('`') is { } index && index >= 0 ?
                type.Name.Substring(0, index) : type.Name;
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

        public static string GetFullName(MethodInfo method)
        {
            var tn = method.DeclaringType is { } dt ?
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
            GetFullName(dlg.Method);

        ///////////////////////////////////////////////////////////////////////////////

        public struct MethodEntry
        {
            public string MethodName;
            public MethodInfo Method;
        }

        public static IEnumerable<MethodEntry> EnumerateTargetMethods(object target) =>
            target.GetType().
                Traverse(t => t.BaseType).
                SelectMany(t => new[] { t }.Concat(t.GetInterfaces())).
                SelectMany(t => t.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)).
                Select(method => new MethodEntry
                {
                    Method = method,
                    MethodName = method.GetCustomAttributes(typeof(JavaScriptTargetAttribute), true) is object[] cas &&
                        cas.Length >= 1 && cas[0] is JavaScriptTargetAttribute a ?
                            (IsNullOrWhiteSpace(a.Name) ? GetName(method) : a.Name!) :
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
#elif NET45
            Task.FromResult(0);
#else
            Task.CompletedTask;
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
    }
}
