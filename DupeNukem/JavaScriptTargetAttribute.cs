﻿////////////////////////////////////////////////////////////////////////////
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

namespace DupeNukem
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JavaScriptTargetAttribute : Attribute
    {
        public readonly string? Name;

        public JavaScriptTargetAttribute()
        {
        }

        public JavaScriptTargetAttribute(string name) =>
            this.Name = name;
    }
}
