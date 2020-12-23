namespace Transport
{
    public enum DisconnectReason : byte
    {
        Timeout = 1,
        UserRequest,
        SequenceOutOfBounds,
    }
}
