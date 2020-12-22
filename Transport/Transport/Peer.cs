﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Transport
{
    public class Peer
    {
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
            }
        }

        private void UpdateConnected(Connection connection)
        {
            Log.Info("UpdateConnected");
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

        public void Connect(IPEndPoint endPoint)
        {
            var connection = CreateConnection(endPoint);
            connection.State = ConnectionState.Connecting;
        }

        public void SendUnconnected(EndPoint target, byte[] data)
        {
            _socket.SendTo(data, target);
        }

        public void Send(Connection connection, Packet packet)
        {
            Log.Info($"[Send]: {connection}, {packet.ToString()}");
            _socket.SendTo(packet.Data.ToArray(), connection.RemoteEndPoint);
        }

        public void Send(Connection connection, byte[] data)
        {
            _socket.SendTo(data, connection.RemoteEndPoint);
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
            Log.Info($"[Received]: [{endpoint}], {packet.ToString()}");

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
                // TODO - Send server is full message as a reply
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
            }
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
                    connection.State = ConnectionState.Connected;
                    break;
            }
        }

        private void HandleConnectionRequest(Connection connection, Packet packet)
        {
            switch (connection.State)
            {
                case ConnectionState.Created:
                    connection.State = ConnectionState.Connected;
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
            var connection = new Connection(endPoint);
            _connections.Add(endPoint, connection);
            Log.Info($"[CreateConnection] Created {connection}");
            return connection;
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
