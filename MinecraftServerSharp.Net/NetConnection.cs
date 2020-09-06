﻿using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MinecraftServerSharp.Data.IO;
using MinecraftServerSharp.Net.Packets;
using MinecraftServerSharp.Utility;

namespace MinecraftServerSharp.Net
{
    public partial class NetConnection
    {
        private Action<NetConnection>? _closeAction;

        public NetOrchestrator Orchestrator { get; }
        public Socket Socket { get; }
        public SocketAsyncEventArgs ReceiveEvent { get; }
        public SocketAsyncEventArgs SendEvent { get; }
        public IPEndPoint RemoteEndPoint { get; }

        // TODO: make better use of the streams (recycle them better or something)
        public ChunkedMemoryStream ReceiveBuffer { get; }
        public ChunkedMemoryStream SendBuffer { get; }
        public NetBinaryReader BufferReader { get; }
        public NetBinaryWriter BufferWriter { get; }

        public object WriteMutex { get; } = new object();
        public object CloseMutex { get; } = new object();

        public int ReceivedLength { get; set; } = -1;
        public int ReceivedLengthBytes { get; set; } = -1;
        public int TotalReceivedLength => ReceivedLength + ReceivedLengthBytes;

        public long BytesSent { get; set; }
        public long BytesReceived { get; set; }

        // TODO: add thread-safe protocol state propagation
        public ProtocolState State { get; set; }

        public string? UserName { get; internal set; }

        #region Constructors

        public NetConnection(
            NetOrchestrator orchestrator,
            Socket socket,
            SocketAsyncEventArgs receiveEvent,
            SocketAsyncEventArgs sendEvent,
            Action<NetConnection> closeAction)
        {
            Orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
            ReceiveEvent = receiveEvent ?? throw new ArgumentNullException(nameof(receiveEvent));
            SendEvent = sendEvent ?? throw new ArgumentNullException(nameof(sendEvent));
            _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));

            // get it here as we can't get it later if the socket gets disposed
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;

            ReceiveBuffer = Orchestrator.MemoryManager.GetStream();
            SendBuffer = Orchestrator.MemoryManager.GetStream();
            BufferReader = new NetBinaryReader(ReceiveBuffer);
            BufferWriter = new NetBinaryWriter(SendBuffer);

            State = ProtocolState.Handshaking;
        }

        #endregion

        public (OperationStatus Status, int Length) ReadPacket<TPacket>(out TPacket packet)
        {
            var reader = Orchestrator.Codec.Decoder.GetPacketReader<TPacket>();
            long oldPosition = BufferReader.Position;
            var status = reader.Invoke(BufferReader, out packet);
            int length = (int)(BufferReader.Position - oldPosition);
            return (status, length);
        }

        public void EnqueuePacket<TPacket>(TPacket packet)
        {
            Orchestrator.EnqueuePacket(this, packet);
        }

        public void TrimReceiveBufferStart(int length)
        {
            ReceiveBuffer.TrimStart(length);

            ReceivedLength = -1;
            ReceivedLengthBytes = -1;
        }

        public void TrimSendBufferStart(int length)
        {
            SendBuffer.TrimStart(length);
        }

        /// <summary>
        /// Removes the first message from the receive buffer
        /// while keeping all the to-be-processed data.
        /// </summary>
        public void TrimFirstReceivedMessage()
        {
            int offset = TotalReceivedLength;
            TrimReceiveBufferStart(offset);
        }

        public void Kick(Exception? exception)
        {
            Chat? chat = null;
            if (exception != null)
            {
                var dyn = new[]
                {
                    new { text = "Server Exception\n", bold = true },
                    new { text = exception.ToString(), bold = false }
                };
                chat = new Chat((Utf8String)JsonSerializer.Serialize(dyn));
            }
            Kick(chat);
        }

        public void Kick(string? reason = null)
        {
            Chat? chat = null;
            if (reason != null)
            {
                var dyn = new[]
                {
                    new { text = "Kicked by server\n", bold = true },
                    new { text = reason, bold = false }
                };
                chat = new Chat((Utf8String)JsonSerializer.Serialize(dyn));
            }
            Kick(chat);
        }

        public void Kick(Chat? reason = null)
        {
            if (reason != null)
            {
                if (State == ProtocolState.Play)
                {
                    var packet = new ServerPlayDisconnect(reason.Value);
                    EnqueuePacket(packet);
                }
                else if (State == ProtocolState.Login)
                {
                    var packet = new ServerLoginDisconnect(reason.Value);
                    EnqueuePacket(packet);
                }
                Orchestrator.RequestFlush();
            }

            Close(immediate: false);
        }

        public void Close(bool immediate)
        {
            if (!immediate)
            {
                State = ProtocolState.Closing;
                return;
            }

            lock (CloseMutex)
            {
                if (_closeAction == null)
                    return;

                State = ProtocolState.Disconnected;
                _closeAction.Invoke(this);
                _closeAction = null;

                //Console.WriteLine("Connection metrics; Sent: " + BytesSent + ", Received: " + BytesReceived);
            }
        }
    }
}
