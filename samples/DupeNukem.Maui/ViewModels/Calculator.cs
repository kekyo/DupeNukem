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

using System.Threading.Tasks;

namespace DupeNukem.ViewModels;

internal sealed class Calculator
{
    [CallableTarget]
    public async Task<int> add(int a, int b)
    {
        await Task.Delay(100);
        return a + b;
    }

    [CallableTarget("sub")]
    public async Task<int> __sub__123(int a, int b)
    {
        await Task.Delay(100);
        return a - b;
    }

    // [CallableTarget]   // couldn't invoke from JavaScript.
    public async Task<int> mult(int a, int b)
    {
        await Task.Delay(100);
        return a * b;
    }
}
