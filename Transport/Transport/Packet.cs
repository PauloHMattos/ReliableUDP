using System;
using System.Diagnostics;

namespace Transport
{
    public ref struct Packet
    {
        public ReadOnlySpan<byte> Data { get; }
        public PacketType Type { get; }

        public Packet(byte[] data, int offset, int length)
        {
            Debug.Assert(length > 0);

            Data = data.AsSpan(offset, length);
            Type = (PacketType)Data[0];
        }

        public override string ToString()
        {
            if (Type == PacketType.Command)
            {
                return $"[Packet Type={Type}, Id={(Commands)Data[0]}, Length={Data.Length}]";
            }
            return $"[Packet Type={Type}, Length={Data.Length}]";
        }

        public static Packet Command(Commands commandId, byte? data = null)
        {
            byte[] buffer;
            if (data.HasValue)
            {
                buffer = new byte[3] { (byte)PacketType.Command, (byte)commandId, data.Value };
            }
            else
            {
                buffer = new byte[2] { (byte)PacketType.Command, (byte)commandId };
            }
            return new Packet(buffer, 0, buffer.Length);
        }
    }
}
