using System;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Peer
    {
        private readonly Socket _socket;
        private readonly Config _config;

        public Peer(Config config)
        {
            _config = config;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = false
            };
            _socket.Bind(config.EndPoint);
            SetConnectionReset(_socket);
        }

        public void Update()
        {
            // Receive
            Receive();

            // Process

            // TODO - Implement threading for sending
            // Send()
        }

        private void Receive()
        {
            if (!_socket.Poll(0, SelectMode.SelectRead))
            {
                return;
            }

            var buffer = GetMtuBuffer();
            var endpoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
            var bytesReceived = _socket.ReceiveFrom(buffer, SocketFlags.None, ref endpoint);
            Log.Info($"Received {bytesReceived} bytes from {endpoint}");
        }

        public void SendUnconnected(EndPoint target, byte[] data)
        {
            _socket.SendTo(data, target);
        }

        // TODO: Reuse this buffer
        private byte[] GetMtuBuffer()
        {
            return new byte[_config.MTU];
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
