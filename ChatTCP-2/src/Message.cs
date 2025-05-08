namespace ChatTCP;

/// <summary>
/// A message, with a username of the user who sent it, time it was sent and its contents.
/// </summary>
public struct Message
{
    /// <summary>
    /// The time this message was sent.
    /// </summary>
    public DateTime TimeSent;

    /// <summary>
    /// The content of this message.
    /// </summary>
    public string Content;

    public override string ToString()
    {
        return @$"[{TimeSent:HH\:mm\:ss}]" + $" - {Content}";
    }
}