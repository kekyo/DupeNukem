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

        //private abstract class ChangeType<T>
        //{
        //    public static readonly ChangeType<T> Instance;

        //    static ChangeType() =>
        //        Instance = typeof(T).IsEnum ?
        //            new ChangeTypeForEnum<T>() :
        //            new ChangeTypeByConverter<T>();

        //    public abstract T Change(object? value);
        //}

        //private sealed class ChangeTypeForEnum<T> : ChangeType<T>
        //{
        //    private readonly Type underlyingType;

        //    public ChangeTypeForEnum() =>
        //        this.underlyingType = Enum.GetUnderlyingType(typeof(T));

        //    public override T Change(object? value) =>
        //        value is string str ?
        //            (T)Enum.Parse(typeof(T), str) :
        //            (T)(object)Convert.ChangeType(value, this.underlyingType)!;
        //}

        //private sealed class ChangeTypeByConverter<T> : ChangeType<T>
        //{
        //    public override T Change(object? value) =>
        //        (T)Convert.ChangeType(value, typeof(T))!;
        //}

        //public static T ConvertTo<T>(object? value) =>
        //    value is T tv ? tv : ChangeType<T>.Instance.Change(value);

        public static string GetMethodName(Delegate dlg) =>
            $"{dlg.Method.DeclaringType?.FullName ?? "global"}.{dlg.Method.Name}";

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
    }
}
