using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Transport.Notify;

namespace Transport
{
    public class Peer
    {
        public delegate void OnConnectionFailedDelegate(Connection connection, ConnectionFailedReason reason);
        public delegate void OnConnectedDelegate(Connection connection);
        public delegate void OnDisconnectedDelegate(Connection connection, DisconnectReason reason);
        public delegate void OnUnreliablePacketDelegate(Connection connection, Packet packet);
        public delegate void OnNotifyPacketDelegate(Connection connection, object? userData);
        public delegate void OnNotifyPacketDelegate2(Connection connection, Packet packet);

        public event OnConnectionFailedDelegate? OnConnectionFailed;
        public event OnConnectedDelegate? OnConnected;
        public event OnDisconnectedDelegate? OnDisconnected;
        public event OnUnreliablePacketDelegate? OnUnreliablePacket;
        public event OnNotifyPacketDelegate? OnNotifyPacketLost;
        public event OnNotifyPacketDelegate? OnNotifyPacketDelivered;
        public event OnNotifyPacketDelegate2? OnNotityPacketReceived;

        private readonly Socket _socket;
        private readonly Config _config;
        private readonly Timer _timer;
        private readonly Random _random;
        private readonly Dictionary<IPEndPoint, Connection> _connections;

        public Peer(Config config)
        {
            _random = new Random(Environment.TickCount);

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

        public void Disconnect(Connection connection)
        {
            if (connection.State != ConnectionState.Connected)
            {
                Log.Error($"[Disconnect]: Can't disconnect {connection}, state is {connection.State}");
                return;
            }

            DisconnectConnection(connection, DisconnectReason.UserRequest);
        }

        public void SendUnconnected(EndPoint target, Packet packet)
        {
            Log.Info($"[SendUnconnected]: Target {target}, {packet.ToString()}");
            SendInternal(target, packet.Data.ToArray());
        }

        public void SendUnreliable(Connection connection, byte[] data)
        {
            if (data.Length > (_config.MTU - 1))
            {
                Log.Error($"[SendUnreliable]: Data too large, above MTU-1 {data.Length}");
                return;
            }

            var buffer = GetMtuBuffer();
            Buffer.BlockCopy(data, 0, buffer, 1, data.Length);
            buffer[0] = (byte)PacketType.Unreliable;
            SendInternal(connection.RemoteEndPoint, buffer, data.Length + 1);
        }

        private int NotifyPacketHeaderSize => 1 + (2 * _config.SequenceNumberBytes) + sizeof(ulong);

        public bool SendNotify(Connection connection, byte[] data, object? userObject)
        {
            if (connection.SendWindow.IsFull)
            {
                return false;
            }


            if (data.Length > (_config.MTU - NotifyPacketHeaderSize))
            {
                Log.Error($"[SendNotify]: Data too large, above MTU-Header_Size({NotifyPacketHeaderSize}) {data.Length}");
                return false;
            }

            var sequenceNumberForPacket = connection.SendSequencer.Next();

            var buffer = GetMtuBuffer();
            Buffer.BlockCopy(data, 0, buffer, NotifyPacketHeaderSize, data.Length);

            // Fill header data
            buffer[0] = (byte)PacketType.Notify;
            var offset = 1;
            ByteUtils.WriteULong(buffer, offset, _config.SequenceNumberBytes, sequenceNumberForPacket);
            offset += _config.SequenceNumberBytes;
            ByteUtils.WriteULong(buffer, offset, _config.SequenceNumberBytes, connection.LastReceivedSequence);
            offset += _config.SequenceNumberBytes;
            ByteUtils.WriteULong(buffer, offset, sizeof(ulong), connection.ReceiveMask);

            connection.SendWindow.Push(new SendEnvelope()
            {
                Sequence = sequenceNumberForPacket,
                SendTime = _timer.Now,
                UserData = userObject
            });

            SendInternal(connection, buffer, NotifyPacketHeaderSize + data.Length);
            return true;
        }

        public void SendPacket(Connection connection, Packet packet)
        {
            //Log.Info($"[Send]: Target {connection}, {packet.ToString()}");

            SendInternal(connection, packet.Data.ToArray());
        }

        private void UpdateConnections()
        {
            foreach (var (_, connection) in _connections)
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

            if (connection.LastSentPacketTime + _config.KeepAliveInterval < _timer.Now)
            {
                SendPacket(connection, Packet.KeepAlive());
            }
        }

        private void DisconnectConnection(Connection connection, DisconnectReason reason, bool sendToOtherPeer = true)
        {
            Log.Info($"[DisconnectConnection]: {connection}, Reason={reason}");

            if (sendToOtherPeer)
            {
                SendPacket(connection, Packet.Command(Commands.Disconnected, (byte)reason));
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
            SendPacket(connection, Packet.Command(Commands.ConnectionRequest));
        }

        private void SendInternal(Connection connection, byte[] data, int? length = null)
        {
            Debug.Assert(connection.State < ConnectionState.Disconnected);
            connection.LastSentPacketTime = _timer.Now;
            SendInternal(connection.RemoteEndPoint, data, length);
        }

        private void SendInternal(EndPoint target, byte[] data, int? length = null)
        {
            if (length.HasValue)
            {
                _socket.SendTo(data, 0, length.Value, SocketFlags.None, target);
            }
            else
            {
                _socket.SendTo(data, target);
            }
        }

        private void Receive()
        {
            while (_socket.Poll(0, SelectMode.SelectRead))
            {

                var buffer = GetMtuBuffer();
                var endpoint = (EndPoint)new IPEndPoint(IPAddress.Any, 0);
                var bytesReceived = _socket.ReceiveFrom(buffer, SocketFlags.None, ref endpoint);

                if (_random.NextDouble() <= _config.SimulatedLoss)
                {
                    Log.Info($"Simulated loss of {bytesReceived} bytes");
                    continue;
                }

                var packet = new Packet(buffer, 0, bytesReceived);
                //Log.Info($"[Received]: From [{endpoint}], {packet.ToString()}");

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

                case PacketType.Unreliable:
                    HandleUnreliablePacket(connection, packet);
                    break;

                case PacketType.KeepAlive:
                    // Only used to keep connections alive
                    // Don't need to do anything
                    break;

                case PacketType.Notify:
                    HandleNotifyPacket(connection, packet);
                    break;

                default:
                    throw new NotImplementedException($"Not implemented for packet type: {packet.Type}");
            }
        }

        private void HandleNotifyPacket(Connection connection, Packet packet)
        {
            if (packet.Data.Length < NotifyPacketHeaderSize)
            {
                return;
            }

            var offset = 1;
            var packetSequenceNumber = ByteUtils.ReadULong(packet.Data, offset, _config.SequenceNumberBytes);
            offset += _config.SequenceNumberBytes;
            var remoteRecvSequence = ByteUtils.ReadULong(packet.Data, offset, _config.SequenceNumberBytes);
            offset += _config.SequenceNumberBytes;
            var remoteRecvMask = ByteUtils.ReadULong(packet.Data, offset, sizeof(ulong));

            var sequenceDistance = connection.SendSequencer.Distance(packetSequenceNumber, connection.LastReceivedSequence);

            // Sequence so out of bounds we can't save, just disconnect
            if (Math.Abs(sequenceDistance) > _config.SendWindowSize)
            {
                DisconnectConnection(connection, DisconnectReason.SequenceOutOfBounds);
                return;
            }

            // Sequence is old, so duplicate or re-ordered packet
            if (sequenceDistance <= 0)
            {
                return;
            }

            // Update recv sequence for ou local connection object
            connection.LastReceivedSequence = packetSequenceNumber;

            if (sequenceDistance >= ACK_MASK_BITS)
            {
                connection.ReceiveMask = 1; // 0000 0000 0000 0000 0000 0000 0000 0001
            }
            else
            {
                connection.ReceiveMask = (connection.ReceiveMask << (int)sequenceDistance) | 1;
            }

            AckPackets(connection, remoteRecvSequence, remoteRecvMask);

            // Only a ack packet
            if (packet.Data.Length == NotifyPacketHeaderSize)
            {
                return;
            }

            var trimedPacket = packet.Slice(NotifyPacketHeaderSize);
            OnNotityPacketReceived?.Invoke(connection, trimedPacket);
        }

        const int ACK_MASK_BITS = sizeof(ulong) * 8;

        private void AckPackets(Connection connection, ulong recvSequenceFromRemote, ulong recvMaskFromRemote)
        {
            while (connection.SendWindow.Count > 0)
            {
                var envelope = connection.SendWindow.Peek();
                var distance = (int)connection.SendSequencer.Distance(envelope.Sequence, recvSequenceFromRemote);

                if (distance > 0)
                {
                    break;
                }

                // remove envelope from send window
                connection.SendWindow.Pop();

                // If this is the same as the latest sequence remove received from us, we can use thus to calculate RTT
                if (distance == 0)
                {
                    connection.Rtt = _timer.Now - envelope.SendTime;
                }

                // If any of this cases trigger, packet is most likely lost
                if ((distance <= -ACK_MASK_BITS) || ((recvMaskFromRemote & (1UL << -distance)) == 0UL))
                {
                    OnNotifyPacketLost?.Invoke(connection, envelope.UserData);
                }
                else
                {
                    OnNotifyPacketDelivered?.Invoke(connection, envelope.UserData);
                }
            }
        }

        private void HandleUnreliablePacket(Connection connection, Packet packet)
        {
            // Remove the unreliable header and pass to the user code
            var trimedPacket = packet.Slice(1);
            OnUnreliablePacket?.Invoke(connection, trimedPacket);
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
                    SendPacket(connection, Packet.Command(Commands.ConnectionAccepted));
                    break;

                case ConnectionState.Connected:
                    SendPacket(connection, Packet.Command(Commands.ConnectionAccepted));
                    break;

                case ConnectionState.Connecting:
                    Debug.Fail("Cannot be in connecting state while a connection request has been sent");
                    break;
            }
        }

        private Connection CreateConnection(IPEndPoint endPoint)
        {
            var connection = new Connection(_config, endPoint)
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
