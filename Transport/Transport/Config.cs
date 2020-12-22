using System.Net;

namespace Transport
{
    /// <summary>
    /// Transport peer configuration data
    /// </summary>
    public class Config
    {
        /// <summary>
        /// EndPoint binded to the socket
        /// </summary>
        public IPEndPoint EndPoint { get; }
        /// <summary>
        /// Maximum transmission unit in bytes
        /// UDP + IP Header = 28 bytes
        /// </summary>
        public int MTU { get; } = 1280 - 28;
        /// <summary>
        /// Max connections allowed in the server
        /// </summary>
        public int MaxConnections { get; } = 10;
        /// <summary>
        /// TODO
        /// </summary>
        public int MaxConnectionAttempts { get; } = 10;
        /// <summary>
        /// TODO
        /// </summary>
        public double ConnectionAttemptInterval { get; } = 0.25f;
        
        public Config(IPAddress address, ushort port) : this(new IPEndPoint(address, port))
        {
        }
        
        public Config(IPEndPoint endPoint )
        {
            EndPoint = endPoint;
        }
    }
}