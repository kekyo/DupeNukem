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
using System.Diagnostics;
using System.Threading;

namespace DupeNukem.Internal;

internal static class ConverterContext
{
    // JsonSerializer cannot pass application-specific context information
    // to the Converter during serialization runs.
    // The ConverterContext class is used to identify the corresponding Messenger instance
    // during serialization.

    private sealed class MessengerContext
    {
        private IMessenger? messenger;
        private int count;

        public IMessenger Current
        {
            get
            {
                Debug.Assert(this.messenger != null);
                Debug.Assert(this.count >= 1);
                return this.messenger!;
            }
        }

        public void Enter(IMessenger messenger)
        {
            if (this.count <= 0)
            {
                Debug.Assert(this.messenger == null);
                this.messenger = messenger;
                this.count = 1;
            }
            else
            {
                Debug.Assert(this.messenger == messenger);
                this.count++;
            }
        }

        public void Exit(IMessenger messenger)
        {
            Debug.Assert(this.messenger == messenger);
            Debug.Assert(this.count >= 1);
            this.count--;
            if (this.count <= 0)
            {
                this.messenger = null!;
                this.count = 0;
            }
        }
    }

    private static readonly ThreadLocal<MessengerContext> messengers =
        new(() => new MessengerContext());

    public static Messenger Current
    {
        get
        {
            AssertValidState();
            return (Messenger)messengers.Value!.Current;
        }
    }

    [Conditional("DEBUG")]
    public static void AssertValidState() =>
        Debug.Assert(
            messengers.Value!.Current is Messenger,
            "Invalid state: Not called correctly.");

    public static void Enter(IMessenger messenger) =>
        messengers.Value!.Enter(messenger);

    public static void Exit(IMessenger messenger) =>
        messengers.Value!.Exit(messenger);

    public static void Run(IMessenger messenger, Action action)
    {
        messengers.Value!.Enter(messenger);
        try
        {
            action();
        }
        finally
        {
            messengers.Value!.Exit(messenger);
        }
    }

    public static T Run<T>(IMessenger messenger, Func<T> action)
    {
        messengers.Value!.Enter(messenger);
        try
        {
            return action();
        }
        finally
        {
            messengers.Value!.Exit(messenger);
        }
    }
}
