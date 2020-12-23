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
        public double ConnectionAttemptInterval { get; init; } = 0.25;

        /// <summary>
        /// TODO
        /// </summary>
        public double ConnectionTimeout { get; init; } = 5;

        /// <summary>
        /// TODO
        /// </summary>
        public double DisconnectIdleTime { get; init; } = 2;
        public double KeepAliveInterval { get; init; } = 1;
        public int SequenceNumberBytes { get; init; } = 2;
        public int SendWindowSize { get; init; } = 512;
        public float SimulatedLoss { get; init; } = 0.25f;

        public Config(IPAddress address, ushort port) : this(new IPEndPoint(address, port))
        {
        }
        
        public Config(IPEndPoint endPoint )
        {
            EndPoint = endPoint;
        }
    }
}