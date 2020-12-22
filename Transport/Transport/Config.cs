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
        public int MTU { get; init; } = 1280 - 28;

        /// <summary>
        /// Max connections allowed in the server
        /// </summary>
        public int MaxConnections { get; init; } = 10;

        /// <summary>
        /// TODO
        /// </summary>
        public int MaxConnectionAttempts { get; init; } = 10;

        /// <summary>
        /// TODO
        /// </summary>
        public double ConnectionAttemptInterval { get; } = 0.25;

        /// <summary>
        /// TODO
        /// </summary>
        public double ConnectionTimeout { get; } = 5;

        /// <summary>
        /// TODO
        /// </summary>
        public double DisconnectIdleTime { get; } = 2;
        
        public Config(IPAddress address, ushort port) : this(new IPEndPoint(address, port))
        {
        }
        
        public Config(IPEndPoint endPoint )
        {
            EndPoint = endPoint;
        }
    }
}