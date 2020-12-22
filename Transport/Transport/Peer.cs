using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Peer
    {
        public event Action<Connection, ConnectionFailedReason>? OnConnectionFailed;
        public event Action<Connection>? OnConnected;
        public event Action<Connection, DisconnectReason>? OnDisconnected;

        private readonly Socket _socket;
        private readonly Config _config;
        private readonly Timer _timer;
        private readonly Dictionary<IPEndPoint, Connection> _connections;

        public Peer(Config config)
        {
            _config = config;
            _timer = Timer.StartNew();
            _connections = new Dictionary<IPEndPoint, Connection>();
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
            UpdateConnections();

            // TODO - Implement threading for sending
            // Send()
        }
        
        public void Connect(IPEndPoint endPoint)
        {
            var connection = CreateConnection(endPoint);
            connection.State = ConnectionState.Connecting;
        }

        public void SendUnconnected(EndPoint target, Packet packet)
        {
            Log.Info($"[SendUnconnected]: Target {target}, {packet.ToString()}");
            SendInternal(target, packet.Data.ToArray());
        }

        public void Send(Connection connection, Packet packet)
        {
            Debug.Assert(connection.State < ConnectionState.Disconnected);

            Log.Info($"[Send]: Target {connection}, {packet.ToString()}");

            connection.LastSentPacketTime = _timer.Now;
            
            SendInternal(connection.RemoteEndPoint, packet.Data.ToArray());
        }

        private void UpdateConnections()
        {
            foreach(var (_, connection) in _connections)
            {
                UpdateConnection(connection);
            }
        }

        private void UpdateConnection(Connection connection)
        {
            switch (connection.State)
            {
                case ConnectionState.Connecting:
                    UpdateConnecting(connection);
                    break;
                case ConnectionState.Connected:
                    UpdateConnected(connection);
                    break;
                case ConnectionState.Disconnected:
                    UpdateDisconnected(connection);
                    break;
            }
        }

        private void UpdateDisconnected(Connection connection)
        {
            if (connection.DisconnectTime + _config.DisconnectIdleTime < _timer.Now)
            {
                RemoveConnection(connection);
            }
        }

        private void UpdateConnected(Connection connection)
        {
            if (connection.LastReceivedPacketTime + _config.ConnectionTimeout < _timer.Now)
            {
                Log.Info($"[Timeout]: {connection}");
                DisconnectConnection(connection, DisconnectReason.Timeout);
            }
        }

        private void DisconnectConnection(Connection connection, DisconnectReason reason, bool sendToOtherPeer = true)
        {
            Log.Info($"[DisconnectConnection]: {connection}, Reason={reason}");

            if (sendToOtherPeer)
            {
                Send(connection, Packet.Command(Commands.Disconnected, (byte)reason));
            }
            connection.State = ConnectionState.Disconnected;
            connection.DisconnectTime = _timer.Now;
            OnDisconnected?.Invoke(connection, reason);
        }

        private void UpdateConnecting(Connection connection)
        {
            if (connection.LastConnectionAttemptTime + _config.ConnectionAttemptInterval > _timer.Now)
            {
                return;
            }

            if (connection.ConnectionAttempts == _config.MaxConnectionAttempts)
            {
                // TODO - Use callback for this
                Debug.Fail("Max attempts reached: use callback on this");
                return;
            }

            connection.ConnectionAttempts++;
            connection.LastConnectionAttemptTime = _timer.Now;
            Send(connection, Packet.Command(Commands.ConnectionRequest));
        }

        private void SendInternal(EndPoint target, byte[] data)
        {
            _socket.SendTo(data, target);
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

            var packet = new Packet(buffer, 0, bytesReceived);
            Log.Info($"[Received]: From [{endpoint}], {packet.ToString()}");

            var ipEndPoint = (IPEndPoint)endpoint;
            if (_connections.TryGetValue(ipEndPoint, out var connection))
            {
                HandleConnectedPacket(connection, packet);
            }
            else
            {
                HandleUnconnectedPacket(ipEndPoint, packet);
            }
        }

        private void HandleConnectedPacket(Connection connection, Packet packet)
        {
            if (connection.State >= ConnectionState.Disconnected)
            {
                return;
            }

            connection.LastReceivedPacketTime = _timer.Now;
            switch (packet.Type)
            {
                case PacketType.Command:
                    HandleCommandPacket(connection, packet);
                    break;
                
                default:
                    throw new NotImplementedException($"Not implemented for packet type: {packet.Type}");
            }
        }

        private void HandleUnconnectedPacket(IPEndPoint endpoint, Packet packet)
        {
            if (packet.Data.Length != 2)
            {
                // Assume the packet is garbage from somewhere on the internet
                return;
            }

            if (packet.Type != PacketType.Command)
            {
                // First packet has to be a command
                return;
            }

            var commandId = (Commands)packet.Data[1];
            if (commandId != Commands.ConnectionRequest)
            {
                // First packet has to be a connect request command
                return;
            }

            if (_connections.Count >= _config.MaxConnections)
            {
                SendUnconnected(endpoint,
                                Packet.Command(Commands.ConnectionFailed,
                                               (byte)ConnectionFailedReason.ServerIsFull));
                return;
            }

            // TODO - Try detect DDOS attack?
            var connection = CreateConnection(endpoint);
            HandleCommandPacket(connection, packet);
        }

        private void HandleCommandPacket(Connection connection, Packet packet)
        {
            Debug.Assert(packet.Type == PacketType.Command);

            var commandId = (Commands)packet.Data[1];
            switch (commandId)
            {
                case Commands.ConnectionRequest:
                    HandleConnectionRequest(connection, packet);
                    break;

                case Commands.ConnectionAccepted:
                    HandleConnectionAccepted(connection, packet);
                    break;

                case Commands.ConnectionFailed:
                    HandleConnectionFailed(connection, packet);
                    break;

                case Commands.Disconnected:
                    HandleDisconnected(connection, packet);
                    break;

                default:
                    Log.Warn($"[HandleCommandPacket]: Unkown Command {commandId}");
                    break;
            }
        }

        private void HandleDisconnected(Connection connection, Packet packet)
        {
            var reason = (DisconnectReason)packet.Data[2];
            DisconnectConnection(connection, reason, false);
        }

        private void HandleConnectionFailed(Connection connection, Packet packet)
        {
            Debug.Assert(connection.State == ConnectionState.Connecting);

            var reason = (ConnectionFailedReason)packet.Data[2];
            Log.Info($"[ConnectionFailed]: {connection}, Reason={reason}");

            RemoveConnection(connection);

            // Callback to the user code
            OnConnectionFailed?.Invoke(connection, reason);
        }

        private void HandleConnectionAccepted(Connection connection, Packet packet)
        {
            switch (connection.State)
            {
                case ConnectionState.Created:
                    Debug.Fail("");
                    break;
                
                case ConnectionState.Connected:
                    // Already connected, so this is a duplicated packet, ignore
                    break;
                    
                case ConnectionState.Connecting:
                    SetAsConnected(connection);
                    break;
            }
        }

        private void HandleConnectionRequest(Connection connection, Packet packet)
        {
            switch (connection.State)
            {
                case ConnectionState.Created:
                    SetAsConnected(connection);
                    Send(connection, Packet.Command(Commands.ConnectionAccepted));
                    break;
                
                case ConnectionState.Connected:
                    Send(connection, Packet.Command(Commands.ConnectionAccepted));
                    break;
                    
                case ConnectionState.Connecting:
                    Debug.Fail("Cannot be in connecting state while a connection request has been sent");
                    break;
            }
        }

        private Connection CreateConnection(IPEndPoint endPoint)
        {
            var connection = new Connection(endPoint)
            {
                LastReceivedPacketTime = _timer.Now
            };
            _connections.Add(endPoint, connection);
            Log.Info($"[CreateConnection] Created {connection}");
            return connection;
        }

        private void RemoveConnection(Connection connection)
        {
            Debug.Assert(connection.State != ConnectionState.Removed);
            Log.Info($"[RemoveConnection]: {connection}");

            var removed = _connections.Remove(connection.RemoteEndPoint);
            Debug.Assert(removed);

            connection.State = ConnectionState.Removed;
        }

        private void SetAsConnected(Connection connection)
        {
            connection.State = ConnectionState.Connected;
            OnConnected?.Invoke(connection);
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
