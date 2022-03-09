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
        public static T ConvertTo<T>(object? value) =>
            value is T tv ?
                tv :
                (T)Convert.ChangeType(value, typeof(T))!;

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
