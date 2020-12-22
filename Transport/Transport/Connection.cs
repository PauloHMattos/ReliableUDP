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
            set
            {
                SetState(value);
            }
        }
        public IPEndPoint RemoteEndPoint { get; }

        public int ConnectionAttempts { get; set; }
        public double LastConnectionAttemptTime { get; set; }

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
                case ConnectionState.Connecting:
                    Debug.Assert(State == ConnectionState.Created || State == ConnectionState.Connecting);
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
