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

namespace DupeNukem.Internal;

public static class Utilities
{
    private static readonly Dictionary<Type, object?> defaultValues = new();
    private static readonly Dictionary<Type, Func<Exception, Dictionary<string, object?>>> exceptionPropertiesGetters = new();

    private abstract class DefaultValue
    {
        public abstract object? GetDefaultValue();
    }

    private sealed class DefaultValue<T> : DefaultValue
    {
        public override object? GetDefaultValue() =>
            default(T);
    }

    internal static object? GetDefaultValue(Type type)
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

    internal static bool IsNullOrWhitespace(string? str) =>
#if NET35
        string.IsNullOrEmpty(str?.Trim());
#else
        string.IsNullOrWhiteSpace(str);
#endif

#if NET35 || NET40 || NET45 || NET451 || NET452
    private static class ArrayEmpty<T>
    {
        public static readonly T[] Empty = new T[0];
    }

    internal static T[] Empty<T>() =>
        ArrayEmpty<T>.Empty;
#else
    internal static T[] Empty<T>() =>
        Array.Empty<T>();
#endif

#if !NET471_OR_GREATER || !NETSTANDARD2_0_OR_GREATER
    internal static IEnumerable<T> Append<T>(
        this IEnumerable<T> enumerable, T value)
    {
        foreach (var item in enumerable)
        {
            yield return item;
        }
        yield return value;
    }
#endif

    ///////////////////////////////////////////////////////////////////////////////

    internal static string GetName(Type type)
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

    internal static string GetFullName(Type type)
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

    internal static string GetName(
        MethodInfo method, bool trimAsync)
    {
        var mn = method.Name.LastIndexOf('`') is { } index && index >= 0 ?
            method.Name.Substring(0, index) : method.Name;
        if (trimAsync && mn.EndsWith("Async"))
        {
            mn = mn.Substring(0, mn.Length - 5);
        }
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

    internal static string GetFullName(
        MethodInfo method, Type? runtimeType, bool trimAsync)
    {
        var tn = (runtimeType != null) ?
            GetFullName(runtimeType) :
            method.DeclaringType is { } dt ?
                GetFullName(dt) : "global";
        var mn = method.Name.LastIndexOf('`') is { } index && index >= 0 ?
            method.Name.Substring(0, index) : method.Name;
        if (trimAsync && mn.EndsWith("Async"))
        {
            mn = mn.Substring(0, mn.Length - 5);
        }
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

    internal static string GetMethodFullName(Delegate dlg) =>
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
        GetFullName(dlg.GetMethodInfo(), dlg.Target?.GetType(), true);
#else
        GetFullName(dlg.Method, dlg.Target?.GetType(), true);
#endif

    ///////////////////////////////////////////////////////////////////////////////

    internal struct MethodEntry
    {
        public string MethodName;
        public MethodInfo Method;
    }

    internal static string GetConvertedName(
        this NamingStrategy ns, string name, bool hasSpecifiedName = false) =>
        Join(".", name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries).
            Select(element => ns.GetPropertyName(element, hasSpecifiedName)));

    private static string GetMethodName(
        MethodInfo method, Type? runtimeType,
        string? specifiedName, bool isFullName,
        NamingStrategy ns, bool trimAsync)
    {
        if (IsNullOrWhiteSpace(specifiedName))
        {
            var methodName = isFullName ?
                GetFullName(method, runtimeType, trimAsync) :
                GetName(method, trimAsync);
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

#if NET35 || NET40
    internal static MethodInfo GetMethodInfo(
        this Delegate d) =>
        d.Method;
#endif

#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
    internal static bool IsAssignableFrom(
        this Type type, Type rhs) =>
        type.GetTypeInfo().IsAssignableFrom(rhs.GetTypeInfo());
#endif

#if NET35 || NET40
    internal static Delegate CreateDelegate(
        this MethodInfo method, Type delegateType, object target) =>
        Delegate.CreateDelegate(delegateType, target, method);
#endif

    internal static bool IsGenericType(
        this Type type) =>
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
        type.GetTypeInfo().IsGenericType;
#else
        type.IsGenericType;
#endif

#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
    internal static Type[] GetGenericArguments(
        this Type type) =>
        type.GetTypeInfo().GenericTypeArguments;
#endif

#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
    internal static MethodInfo GetMethod(
        this Type type, string name) =>
        type.GetTypeInfo().GetDeclaredMethod(name);
#endif

    internal static IEnumerable<MethodEntry> EnumerateTargetMethods(
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
                        GetMethodName(
                            method, target.GetType(),
                            a.Name, isFullName,
                            memberAccessNamingStrategy, a.TrimAsync) :
                        null!,
            }).
            Where(entry => entry.MethodName != null);

    ///////////////////////////////////////////////////////////////////////////////

    internal static bool SafeTryGetValue<TKey, TValue>(
        this Dictionary<TKey, TValue> dict, TKey key, out TValue value)
        where TKey : notnull
    {
        lock (dict)
        {
            return dict.TryGetValue(key, out value!);
        }
    }

    internal static void SafeAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        where TKey : notnull
    {
        lock (dict)
        {
            dict.Add(key, value);
        }
    }

    internal static bool SafeRemove<TKey, TValue>(
        this Dictionary<TKey, TValue> dict, TKey key)
        where TKey : notnull
    {
        lock (dict)
        {
            return dict.Remove(key);
        }
    }

    internal static IEnumerable<T> Traverse<T>(this T value, Func<T, T?> selector)
        where T : class
    {
        T? current = value;
        while (current != null)
        {
            yield return current;
            current = selector(current);
        }
    }

    internal static IEnumerable<TResult> Collect<TItem, TResult>(
        this IEnumerable<TItem> enumerable,
        Func<TItem, TResult?> selector)
    {
        foreach (var item in enumerable)
        {
            if (selector(item) is { } value)
            {
                yield return value;
            }
        }
    }

    ///////////////////////////////////////////////////////////////////////////////

    internal static Task CompletedTask =>
#if NET35 || NET40
        TaskEx.FromResult(0);
#elif NET45 || NET451 || NET452 || NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
        Task.FromResult(0);
#else
        Task.CompletedTask;
#endif

#if DEBUG
    internal static async Task WhenAll(IEnumerable<Task> tasks)
    {
        foreach (var task in tasks)
        {
            await task.ConfigureAwait(false);
        }
    }
#else
    internal static Task WhenAll(IEnumerable<Task> tasks) =>
#if NET35 || NET40
        TaskEx.WhenAll(tasks);
#else
        Task.WhenAll(tasks);
#endif
#endif

#if DEBUG
    internal static async Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks)
    {
        var results = new List<T>();
        foreach (var task in tasks)
        {
            results.Add(await task.ConfigureAwait(false));
        }
        return results.ToArray();
    }
#else
    internal static Task<T[]> WhenAll<T>(IEnumerable<Task<T>> tasks) =>
#if NET35 || NET40
        TaskEx.WhenAll(tasks);
#else
        Task.WhenAll(tasks);
#endif
#endif

    internal static Task Delay(int msec) =>
#if NET35 || NET40
        TaskEx.Delay(msec);
#else
        Task.Delay(msec);
#endif

    internal static bool IsNullOrWhiteSpace(string? str) =>
#if NET35
        string.IsNullOrEmpty(str) || str!.Trim().Length == 0;
#else
        string.IsNullOrWhiteSpace(str);
#endif

    internal static string Join<T>(string separator, IEnumerable<T> enumerable) =>
#if NET35
        string.Join(separator, enumerable.Select(v => v?.ToString() ?? string.Empty).ToArray());
#else
        string.Join(separator, enumerable);
#endif

    internal static readonly TimeSpan InfiniteTimeSpan =
#if NET35 || NET40
        new TimeSpan(0, 0, 0, 0, -1);
#else
        Timeout.InfiniteTimeSpan;
#endif

    ///////////////////////////////////////////////////////////////////////////////

    internal sealed class DelegatedEqualityComparer<T> : IEqualityComparer<T>
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

    ///////////////////////////////////////////////////////////////////////////////

    private sealed class MemberNameComparer : IEqualityComparer<MemberInfo>
    {
        public bool Equals(MemberInfo? x, MemberInfo? y) =>
            x!.Name == y!.Name;

        public int GetHashCode(MemberInfo obj) =>
            obj.Name.GetHashCode();

        public static readonly MemberNameComparer Instance = new();
    }

    internal static Dictionary<string, object?> ExtractExceptionProperties(
        Exception ex, NamingStrategy namingStrategy)
    {
        var type = ex.GetType();

        Func<Exception, Dictionary<string, object?>> getter;
        lock (exceptionPropertiesGetters)
        {
            if (!exceptionPropertiesGetters.TryGetValue(type, out getter!))
            {
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
                var memberGetters = type.GetTypeInfo().
                    Traverse(t => t.BaseType?.GetTypeInfo()).
                    SelectMany(t => t.DeclaredMembers).
                    Distinct(MemberNameComparer.Instance).
                    Where(m =>
                        ((m is FieldInfo { } f && !f.IsStatic) ||
                         (m is PropertyInfo { } p && p.GetMethod is { } gm && !gm.IsStatic && p.GetIndexParameters().Length == 0)) &&
                        m.IsDefined(typeof(ExceptionPropertyAttribute), true)).
                    Collect(m =>
                    {
                        try
                        {
                            return new
                            {
                                name = m.GetCustomAttribute(typeof(ExceptionPropertyAttribute), true) is ExceptionPropertyAttribute epa ?
                                    (IsNullOrWhitespace(epa.Name) ?
                                        namingStrategy.GetPropertyName(m.Name, false) :
                                        namingStrategy.GetPropertyName(epa.Name!, true)) :
                                    namingStrategy.GetPropertyName(m.Name, false),
                                getter = m is FieldInfo f ?
                                    new Func<Exception, object?>(ex => f.GetValue(ex)) :
                                    new Func<Exception, object?>(ex => ((PropertyInfo)m).GetValue(ex, Empty<object>()))
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    }).
                    ToDictionary(entry => entry.name, entry => entry.getter);
#else
                var memberGetters = type.
                    Traverse(t => t.BaseType).
                    SelectMany(t => t.GetMembers(
                        BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.Instance | BindingFlags.DeclaredOnly)).
                    Distinct(MemberNameComparer.Instance).
                    Where(m =>
                        (m is FieldInfo ||
                         (m is PropertyInfo { } p && p.GetIndexParameters().Length == 0)) &&
                        m.IsDefined(typeof(ExceptionPropertyAttribute), true)).
                    Collect(m =>
                    {
                        try
                        {
                            return new
                            {
                                name = m.GetCustomAttributes(true).
                                    OfType<ExceptionPropertyAttribute>().
                                    FirstOrDefault() is { } epa ?
                                        (IsNullOrWhitespace(epa.Name) ?
                                            namingStrategy.GetPropertyName(m.Name, false) :
                                            namingStrategy.GetPropertyName(epa.Name!, true)) :
                                        namingStrategy.GetPropertyName(m.Name, false),
                                getter = m is FieldInfo f ?
                                    new Func<Exception, object?>(ex => f.GetValue(ex)) :
                                    new Func<Exception, object?>(ex => ((PropertyInfo)m).GetValue(ex, Empty<object>()))
                            };
                        }
                        catch
                        {
                            return null;
                        }
                    }).
                    ToDictionary(entry => entry.name, entry => entry.getter);
#endif
                getter = ex => memberGetters.
                    ToDictionary(mg => mg.Key, mg => mg.Value(ex));
                exceptionPropertiesGetters.Add(type, getter);
            }
        }

        return getter(ex);
    }
}
