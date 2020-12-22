namespace Transport.Notify
{
    public struct Sequencer
    {
        // TODO - Implement tests for this code

        public int Bytes { get; }

        private ulong _sequence;
        private readonly int _shift;
        private readonly ulong _mask;

        public Sequencer(int bytes)
        {
            Bytes = bytes;
            _sequence = 0;
            _mask = (1UL << (bytes * 8)) - 1UL;
            _shift = (sizeof(ulong) - bytes) * 8;
        }

        public ulong Next()
        {
            _sequence = NextAfter(_sequence);
            return _sequence;
        }

        public ulong NextAfter(ulong sequence)
        {
            return (sequence + 1UL) & _mask;
        }

        public long Distance(ulong from, ulong to)
        {
            to <<= _shift;
            from <<= _shift;
            return ((long) (from - to)) >> _shift;
        }
    }
}