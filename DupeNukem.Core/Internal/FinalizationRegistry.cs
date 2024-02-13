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
using System.Linq;
using System.Threading;

namespace DupeNukem.Internal;

// Simulate JavaScript FinalizationRegistry.
internal sealed class FinalizationRegistry
{
    private static readonly TimeSpan period = TimeSpan.FromSeconds(5);
    private static readonly int checkWhenReached = 1000;

    private readonly Action<string> discarded;
    private readonly Dictionary<string, WeakReference> objects = new();
    private readonly Timer timer;

    private bool waitForCollectedThreshold;

    public FinalizationRegistry(Action<string> discarded)
    {
        this.discarded = discarded;
        this.timer = new(_ =>
        {
            lock (this.objects)
            {
                this.Collect();
            }
        }, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Register(object obj, string id)
    {
        lock (this.objects)
        {
            this.objects.Add(id, new(obj));

            if (this.objects.Count == 1)
            {
                this.timer.Change(period, period);
            }
            else if (!waitForCollectedThreshold &&
                this.objects.Count >= checkWhenReached)
            {
                this.waitForCollectedThreshold = true;
                this.Collect();
            }
        }
    }

    public void ForceClear()
    {
        lock (this.objects)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.objects.Clear();
            this.waitForCollectedThreshold = false;
        }
    }

    private void Collect()
    {
        var targetKeys = this.objects.Keys.
            Where(key => !this.objects[key].IsAlive).
            ToArray();

        foreach (var key in targetKeys)
        {
            this.objects.Remove(key);
            this.discarded(key);
        }

        if (this.objects.Count == 0)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        if (this.objects.Count < checkWhenReached)
        {
            this.waitForCollectedThreshold = false;
        }
    }
}
