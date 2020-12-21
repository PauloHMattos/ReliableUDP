using System.Net;

namespace Transport
{
    public class Config
    {
        public IPEndPoint EndPoint;

        public Config(IPAddress address, ushort port)
        {
            EndPoint = new IPEndPoint(address, port);
        }
    }
}