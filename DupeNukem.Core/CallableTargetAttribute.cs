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

namespace DupeNukem;

[AttributeUsage(AttributeTargets.Method)]
public class CallableTargetAttribute : Attribute
{
    public readonly string? Name;

    public readonly bool TrimAsync;

    public CallableTargetAttribute() =>
        this.TrimAsync = true;

    public CallableTargetAttribute(bool trimAsync) =>
        this.TrimAsync = trimAsync;

    public CallableTargetAttribute(string name) =>
        this.Name = name;
}
