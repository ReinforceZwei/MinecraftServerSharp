﻿using System;
using System.Collections.Concurrent;

namespace MinecraftServerSharp.Net
{
    /// <summary>
    /// Used to hold a thread-safe queue of packets to a client.
    /// </summary>
    public class NetPacketSendQueue
    {
        public NetConnection Connection { get; }

        public object EngageMutex { get; } = new object();
        public ConcurrentQueue<PacketHolder> SendQueue { get; } = new ConcurrentQueue<PacketHolder>();

        public bool IsEngaged { get; set; }

        public NetPacketSendQueue(NetConnection connection)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}
