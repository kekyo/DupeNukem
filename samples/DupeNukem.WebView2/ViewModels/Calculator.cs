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
using Newtonsoft.Json;

namespace DupeNukem.ViewModels;

internal abstract class CalculatorBase1
{
    [CallableTarget]
    public Task<int> add(int a, int b) =>
        throw new NotImplementedException("BUG: Fake add is called.");

    [CallableTarget("sub")]
    public Task<int> __sub__123(int a, int b) =>
        throw new NotImplementedException("BUG: Fake sub is called.");
}

internal abstract class CalculatorBase2 : CalculatorBase1
{
    [CallableTarget]
    public new async Task<int> add(int a, int b)
    {
        await Task.Delay(100);
        return a + b;
    }
}

internal sealed class CalculationModel
{
    public int a;
    public int b;
    public CancellationToken ct;

    [JsonConstructor]
    public CalculationModel(int a, int b, CancellationToken ct)
    {
        this.a = a;
        this.b = b;
        this.ct = ct;
    }
}

internal sealed class Calculator : CalculatorBase2
{
    [CallableTarget("sub")]
    public new async Task<int> __sub__123(int a, int b)
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

    [CallableTarget]
    public async Task<int> add_cancellable(int a, int b, CancellationToken token)
    {
        await Task.Delay(2000, token);
        return a + b;
    }

    [CallableTarget]
    [Obsolete]
    public async Task<int> add_obsoleted1(int a, int b)
    {
        await Task.Delay(100);
        return a + b;
    }

    [CallableTarget]
    [Obsolete("Obsoleted test")]
    public async Task<int> add_obsoleted2(int a, int b)
    {
        await Task.Delay(100);
        return a + b;
    }

    [CallableTarget]
    [Obsolete("Obsoleted test", true)]
    public async Task<int> add_obsoleted3(int a, int b)
    {
        await Task.Delay(100);
        return a + b;
    }

    [CallableTarget]
    public async Task<int> mulAsync(int a, int b)   // mul(a, b)
    {
        await Task.Delay(100);
        return a * b;
    }

    [CallableTarget]
    public async Task<int> willBeThrowAsync(int a, int b)
    {
        await Task.Delay(100);
        throw new WillBeThrowException(a, b);
    }

    [CallableTarget]
    public async Task<int> nested_cancellable(CalculationModel model)
    {
        try
        {
            await Task.Delay(2000, model.ct);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        return model.a + model.b;
    }
}

public sealed class WillBeThrowException : Exception
{
    [ExceptionProperty]
    public readonly int A;

    [ExceptionProperty]
    public int B { get; }

    public WillBeThrowException(int a, int b)
    {
        this.A = a;
        this.B = b;
    }
}
