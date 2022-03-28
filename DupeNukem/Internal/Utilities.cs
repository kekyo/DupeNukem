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
                var gtns = string.Join(
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
                var gtns = string.Join(
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
                var gtns = string.Join(
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
                var gtns = string.Join(
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
            public string Name;
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
                    Name = method.GetCustomAttribute(typeof(JavaScriptTargetAttribute), true) is JavaScriptTargetAttribute a ?
                        (string.IsNullOrWhiteSpace(a.Name) ? GetName(method) : a.Name!) : null!,
                }).
                Where(entry => entry.Name != null);

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
    }
}
