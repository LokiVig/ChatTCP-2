namespace ChatTCP;

/// <summary>
/// The different headers for a <see cref="Packet"/>.
/// </summary>
public enum PacketHeader
{
    /// <summary>
    /// This is an invalid packet header, nothing should have this!
    /// </summary>
    Invalid = -1,

    /// <summary>
    /// Unknown packet.
    /// </summary>
    Unknown,

    /// <summary>
    /// A string-filled packet.
    /// </summary>
    String,

    /// <summary>
    /// An integer-filled packet.
    /// </summary>
    Integer
}