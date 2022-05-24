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
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem.ViewModels
{
    internal sealed class Calculator
    {
        [JavaScriptTarget]
        public async Task<int> add(int a, int b)
        {
            await Task.Delay(100);
            return a + b;
        }

        [JavaScriptTarget("sub")]
        public async Task<int> __sub__123(int a, int b)
        {
            await Task.Delay(100);
            return a - b;
        }

        // [JavaScriptTarget]   // couldn't invoke from JavaScript.
        public async Task<int> mult(int a, int b)
        {
            await Task.Delay(100);
            return a * b;
        }

        [JavaScriptTarget]
        public async Task<int> add_cancellable(int a, int b, CancellationToken token)
        {
            await Task.Delay(2000, token);
            return a + b;
        }

        [JavaScriptTarget]
        [Obsolete]
        public async Task<int> add_obsoleted1(int a, int b)
        {
            await Task.Delay(100);
            return a + b;
        }

        [JavaScriptTarget]
        [Obsolete("Obsoleted test")]
        public async Task<int> add_obsoleted2(int a, int b)
        {
            await Task.Delay(100);
            return a + b;
        }

        [JavaScriptTarget]
        [Obsolete("Obsoleted test", true)]
        public async Task<int> add_obsoleted3(int a, int b)
        {
            await Task.Delay(100);
            return a + b;
        }
    }
}
