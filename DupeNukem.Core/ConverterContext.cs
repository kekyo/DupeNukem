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
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace DupeNukem;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class ConverterContext
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

    internal static Messenger Current
    {
        get
        {
            AssertValidState();

            // If the cast fails, you need real `Messenger` that actually works.
            // Perhaps you are using mocks in your unit tests.
            // Messenger is necessary for successful DupeNukem serialization.
            if (messengers.Value!.Current is not Messenger m)
            {
                throw new InvalidOperationException(
                    "DupeNukem: You need real `Messenger` instance.");
            }
            return m;
        }
    }

    [Conditional("DEBUG")]
    internal static void AssertValidState() =>
        Debug.Assert(
            messengers.Value!.Current is Messenger,
            "Invalid state: Not called correctly.");

    internal static void Enter(IMessenger messenger) =>
        messengers.Value!.Enter(messenger);

    internal static void Exit(IMessenger messenger) =>
        messengers.Value!.Exit(messenger);

    [EditorBrowsable(EditorBrowsableState.Advanced)]
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

    [EditorBrowsable(EditorBrowsableState.Advanced)]
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

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static IDisposable Begin(IMessenger messenger)
    {
        messengers.Value!.Enter(messenger);
        return new Disposer(messenger);
    }

    private sealed class Disposer : IDisposable
    {
        private IMessenger? messenger;

        public Disposer(IMessenger messenger) =>
            this.messenger = messenger;

        public void Dispose()
        {
            if (this.messenger is { } messenger)
            {
                this.messenger = null!;
                messengers.Value!.Exit(messenger);
            }
        }
    }
}
