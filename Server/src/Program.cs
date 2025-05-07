using System.Net;

namespace ChatTCP;

/// <summary>
/// A dedicated server-type program, only hosting a server from an IP address without doing much more than that.
/// </summary>
public class Program
{
    /// <summary>
    /// The server we're hosting.
    /// </summary>
    private static Server? server;

    /// <summary>
    /// Determines whether or not the program is fully active.
    /// </summary>
    private static bool active = false;

    /// <summary>
    /// Open a new server and initialize the program.
    /// </summary>
    public static void Main()
    {
        // Initialize a new server
        server = Server.Initialize(IPAddress.Loopback, 27015);

        // We're now a fully running program!
        active = true;

        // While we're running...
        while (active)
        {
            // Call the server's update method
            server.Update();
        }
    }
}