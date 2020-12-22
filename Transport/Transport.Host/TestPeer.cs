using System;
using System.Net;

namespace Transport.Host
{
    internal class TestPeer
    {
        public const ushort SERVER_PORT = 25000;
        private Config _config;
        public Peer Peer;

        public bool IsServer { get; }
        public bool IsClient => !IsServer;

        public TestPeer(bool isServer)
        {
            _config = GetConfig(isServer);
            Peer = new Peer(_config);
            IsServer = isServer;
        }

        public static Config GetConfig(bool isServer)
        {
            Config config;
            if (isServer)
            {
                config = new Config(IPAddress.Loopback, SERVER_PORT);
            }
            else
            {
                config = new Config(IPAddress.Any, 0);
            }
            return config;
        }

        internal void Update()
        {
            Peer.Update();
            if (IsClient)
            {
                Peer.SendUnconnected(new IPEndPoint(IPAddress.Loopback, SERVER_PORT), new byte[4]);
            }
        }
    }
}
