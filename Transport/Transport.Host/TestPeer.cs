using System.Net;

namespace Transport.Host
{
    internal class TestPeer
    {
        public const ushort SERVER_PORT = 25000;
        public Peer Peer;

        public TestPeer(bool isServer)
        {
            Peer = new Peer(GetConfig(isServer));
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
    }
}
