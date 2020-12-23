using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;

namespace Transport.Host
{
    internal class TestPeer
    {
        public const ushort SERVER_PORT = 25000;
        public const int NUMBER_COUNT = 16;

        public static IPEndPoint ServerEndPoint;
        private readonly Config _config;

        public Peer? Peer { get; }
        public bool IsServer { get; }
        public bool IsClient => !IsServer;

        private Connection? _remote;
        private int _numberCounter;

        static TestPeer()
        {
            ServerEndPoint = new IPEndPoint(IPAddress.Loopback, SERVER_PORT);
        }

        public TestPeer(bool isServer)
        {
            _config = GetConfig(isServer);

            IsServer = isServer;

            Peer = new Peer(_config);
            Peer.OnConnected += OnConnected;
            Peer.OnUnreliablePacket += OnUnreliablePacket;
            Peer.OnNotifyPacketLost += OnNotifyPacketLost;
            Peer.OnNotifyPacketDelivered += OnNotifyPacketDelivered;

            if (IsClient)
            {
                Peer.Connect(ServerEndPoint);
            }
        }

        private void OnNotifyPacketDelivered(Connection connection, object? userData)
        {
            if (userData != null)
            {
                Log.Info($"Delivered: {userData}");
            }
        }

        private void OnNotifyPacketLost(Connection connection, object? userData)
        {
            if (userData != null)
            {
                Log.Info($"Resend: {userData}");
                Peer.SendNotify(_remote, BitConverter.GetBytes((int)userData), userData);
            }
        }

        private void OnUnreliablePacket(Connection connection, Packet packet)
        {
            var value = BitConverter.ToUInt32(packet.Data);
            Log.Info($"[OnUnreliablePacket]: {value}");
        }

        private void OnConnected(Connection connection)
        {
            Debug.Assert(Peer != null);
            _remote = connection;
        }

        public static Config GetConfig(bool isServer)
        {
            Config config;
            if (isServer)
            {
                config = new Config(ServerEndPoint);
            }
            else
            {
                config = new Config(IPAddress.Any, 0);
            }
            return config;
        }

        internal void Update()
        {
            Peer?.Update();

            if (_remote != null)
            {
                if (IsClient  && _numberCounter < NUMBER_COUNT)
                {
                    Peer.SendNotify(_remote, BitConverter.GetBytes(++_numberCounter), _numberCounter);
                }
                else
                {
                    Peer.SendNotify(_remote, new byte[0], null);
                }
            }
        }
    }
}
