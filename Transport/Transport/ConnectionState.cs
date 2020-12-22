namespace Transport
{
    public enum ConnectionState
    {
        Created = 1,
        Connecting,
        Connected,

        Disconnected = 9,
        Removed,
    }
}
