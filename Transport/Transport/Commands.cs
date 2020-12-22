namespace Transport
{
    /// <summary>
    /// Enumeration with the available commands for the socket library
    /// </summary>
    public enum Commands : byte
    {
        /// <summary>
        /// Sent by the client to the server when connecting for the first time
        /// </summary>
        ConnectionRequest = 1,
        
        /// <summary>
        /// Sent by the server to the client when a connection request is accepted
        /// </summary>
        ConnectionAccepted,
    }
}
