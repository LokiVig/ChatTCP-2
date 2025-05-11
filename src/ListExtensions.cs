namespace ChatTCP;

/// <summary>
/// Extensions for the base list class.
/// </summary>
public static class ListExtensions
{
    /// <summary>
    /// Finds a <see cref="Client"/> from a specified <paramref name="username"/>.
    /// </summary>
    /// <param name="list">This list.</param>
    /// <param name="username">The username of the <see cref="Client"/> we wish to find.</param>
    /// <returns>The <see cref="Client"/> with the appropriate username, or <see langword="null"/> if none was found.</returns>
    public static Client? FindClient(this List<Client> list, string username)
    {
        // For every client...
        foreach (Client cl in list)
        {
            // If their username matches the argument...
            if (cl.Username == username)
            {
                // Return this client!
                return cl;
            }
        }

        // No client was found with the argument username! Return null
        return null;
    }

    /// <summary>
    /// Tries to find a <see cref="Client"/> from a specified <paramref name="username"/>.
    /// </summary>
    /// <param name="list">This list.</param>
    /// <param name="username">The username of the client we wish to find.</param>
    /// <param name="client">The resulting <see cref="Client"/> we've found, or <see langword="null"/> if none was found.</param>
    /// <returns><see langword="true"/> if we successfully found a <see cref="Client"/> with the appropriate username, <see langword="false"/> otherwise.</returns>
    public static bool TryFindClient(this List<Client> list, string username, out Client? client)
    {
        // Get the client, if it isn't null, return true
        return (client = FindClient(list, username)) != null;
    }
}