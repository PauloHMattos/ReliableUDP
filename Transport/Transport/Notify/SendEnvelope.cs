namespace Transport.Notify
{
    public readonly struct SendEnvelope
    {
        public ulong Sequence { get; init; }
        public double SendTime { get; init; }
        public object? UserData { get; init; }
    }
}
