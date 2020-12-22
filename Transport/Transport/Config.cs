using System.Net;

namespace Transport
{
    public class Config
    {
        public IPEndPoint EndPoint;
        public int MTU = 1280 - 28; // UDP + IP Header = 28 bytes

        public Config(IPAddress address, ushort port)
        {
            EndPoint = new IPEndPoint(address, port);
        }
    }
}