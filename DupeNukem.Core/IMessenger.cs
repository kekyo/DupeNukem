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

using DupeNukem.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace DupeNukem
{
    public sealed class InvalidMessageEventArgs : EventArgs
    {
        public readonly Exception Exception;

        public InvalidMessageEventArgs(Exception ex) =>
            this.Exception = ex;

        public override string ToString() =>
            this.Exception.Message;

        public void Deconstruct(out Exception exception) =>
            exception = this.Exception;
    }

    public sealed class SpriousMessageEventArgs : EventArgs
    {
        public readonly string Json;

        public SpriousMessageEventArgs(string json) =>
            this.Json = json;

        public override string ToString() =>
            $"Sprious message: {this.Json}";

        public void Deconstruct(out string json) =>
            json = this.Json;
    }

    [Serializable]
    public sealed class PeerInvocationException : Exception
    {
        public readonly string Name;
        public readonly string Detail;

        public PeerInvocationException(string name, string message, string detail) :
            base(message)
        {
            this.Name = name;
            this.Detail = detail;
        }

        public void Deconstruct(out string name, out string detail)
        {
            name = this.Name;
            detail = this.Detail;
        }
    }

    public interface IMessenger
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        NamingStrategy MemberAccessNamingStrategy { get; }
        [EditorBrowsable(EditorBrowsableState.Never)]
        JsonSerializer Serializer { get; }

        string[] RegisteredMethods { get; }

        event EventHandler? ErrorDetected;

        [EditorBrowsable(EditorBrowsableState.Never)]
        string RegisterMethod(
            string name, MethodDescriptor method, bool hasSpecifiedName);

        [EditorBrowsable(EditorBrowsableState.Never)]
        void UnregisterMethod(string name, bool hasSpecifiedName);

        Task InvokePeerMethodAsync(
            CancellationToken ct, string methodName, params object?[] args);

        Task<TR> InvokePeerMethodAsync<TR>(
            CancellationToken ct, string methodName, params object?[] args);
    }
}
