namespace Transport
{
    /// <summary>
    /// Available types for the packets on the transport library
    /// </summary>
    public enum PacketType : byte
    {
        /// <summary>
        /// Socket library commands
        /// </summary>
        Command = 1, 
        /// <summary>
        /// Completly unreliable data
        /// </summary>
        Unreliable,
        /// <summary>
        /// Packet used to mantain a connection alive
        /// </summary>
        KeepAlive,
        Notify,
    }
}
