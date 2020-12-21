using System;
using System.Net.Sockets;

namespace Transport
{
    public class Peer
    {
        private readonly Socket _socket;

        public Peer(Config config)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false
            };
            _socket.Bind(config.EndPoint);
            SetConnectionReset(_socket);
        }

        public void Update()
        {
        }



        

        private static void SetConnectionReset(Socket s)
        {
            try
            {
                const uint IOC_IN = 0x80000000;
                const uint IOC_VENDOR = 0x18000000;
                s.IOControl(unchecked((int)(IOC_IN | IOC_VENDOR | 12)),
                            new byte[] { Convert.ToByte(false) },
                            null);
            }
            catch { }
        }
    }
}
