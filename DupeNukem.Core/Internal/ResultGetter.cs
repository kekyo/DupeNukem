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
using System.Threading.Tasks;

namespace DupeNukem.Internal;

internal abstract class TaskResultGetter
{
    private static readonly Dictionary<Type, TaskResultGetter> getters = new();
    private static readonly TaskResultGetter voidGetter = new VoidTaskResultGetter();

    public static Task<object?> GetResultAsync(Task task)
    {
        var taskType = task.GetType();
        if (taskType == typeof(Task))
        {
            return voidGetter.InternalGetResultAsync(task);
        }
        else
        {
#if NETSTANDARD1_3 || NETSTANDARD1_4 || NETSTANDARD1_5 || NETSTANDARD1_6
            var returnType = taskType.GenericTypeArguments[0]!;
#else
            var returnType = taskType.GetGenericArguments()[0]!;
#endif
            TaskResultGetter getter;
            lock (getters)
            {
                if (!getters.TryGetValue(returnType, out getter!))
                {
                    var invokerType = typeof(TypedTaskResultGetter<>).MakeGenericType(returnType);
                    getter = (TaskResultGetter)Activator.CreateInstance(invokerType)!;
                    getters.Add(returnType, getter);
                }
            }
            return getter!.InternalGetResultAsync(task);
        }
    }

    protected abstract Task<object?> InternalGetResultAsync(Task task);

    private sealed class VoidTaskResultGetter : TaskResultGetter
    {
        protected override async Task<object?> InternalGetResultAsync(Task task)
        {
            await task.ConfigureAwait(false);
            return null;
        }
    }

    private sealed class TypedTaskResultGetter<T> : TaskResultGetter
    {
        protected override async Task<object?> InternalGetResultAsync(Task task)
        {
            var result = await ((Task<T>)task).ConfigureAwait(false);
            return result;
        }
    }
}
