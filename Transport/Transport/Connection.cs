using System.Diagnostics;
using System.Net;

namespace Transport
{

    public class Connection
    {
        public ConnectionState State
        {
            get
            {
                return _state;
            }
            internal set
            {
                SetState(value);
            }
        }
        public IPEndPoint RemoteEndPoint { get; }

        public int ConnectionAttempts { get; internal set; }
        public double LastConnectionAttemptTime { get; internal set; }
        public double LastReceivedPacketTime { get; internal set; }
        public double LastSentPacketTime { get; internal set; }
        public double DisconnectTime { get; internal set; }

        private ConnectionState _state;

        public Connection(IPEndPoint remoteEndPoint)
        {
            _state = ConnectionState.Created;
            RemoteEndPoint = remoteEndPoint;
        }

        private void SetState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    Debug.Assert(State == ConnectionState.Created || State == ConnectionState.Connecting);
                    break;

                case ConnectionState.Connecting:
                    Debug.Assert(State == ConnectionState.Created);
                    break;

                case ConnectionState.Disconnected:
                    Debug.Assert(State == ConnectionState.Connected);
                    break;
            }

            Log.Info($"[SetState]: {ToString()} changed state from [{_state}] to [{state}]");
            _state = state;
        }

        public override string ToString()
        {
            return $"[Connection RemoteEndPoint={RemoteEndPoint}]";
        }
    }
}
