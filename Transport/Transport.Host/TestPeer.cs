using System;
using System.Net;

namespace Transport.Host
{
    internal class TestPeer
    {
        public const ushort SERVER_PORT = 25000;
        public static IPEndPoint ServerEndPoint;
        private Config _config;
        public Peer? Peer;

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
            if (IsClient)
            {
                Peer.Connect(ServerEndPoint);
            }
        }

        private void OnConnected(Connection connection)
        {
        }

        public static Config GetConfig(bool isServer)
        {
            Config config;
            if (isServer)
            {
                config = new Config(ServerEndPoint)
                {
                    //MaxConnections = 0
                };
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
