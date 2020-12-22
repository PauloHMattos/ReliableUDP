using System;
using System.Diagnostics;
using System.Net;

namespace Transport.Host
{
    internal class TestPeer
    {
        public const ushort SERVER_PORT = 25000;
        public static IPEndPoint ServerEndPoint;
        private readonly Config _config;

        public Peer? Peer { get; }
        public bool IsServer { get; }
        public bool IsClient => !IsServer;

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
            if (IsClient)
            {
                Peer.Connect(ServerEndPoint);
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
            if (IsClient)
            {
                Peer.SendUnreliable(connection, BitConverter.GetBytes(uint.MaxValue));
                // Peer.Disconnect(connection);
            }
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
        }
    }
}
