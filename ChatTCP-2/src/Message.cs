namespace ChatTCP;

/// <summary>
/// A message, with a username of the user who sent it, time it was sent and its contents.
/// </summary>
public struct Message
{
    /// <summary>
    /// The content of this message.
    /// </summary>
    public string Content;

    public override string ToString()
    {
        return Content;
    }
}