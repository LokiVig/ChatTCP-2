using System.Net;

namespace ChatTCP;

/// <summary>
/// The application in of itself.
/// </summary>
public class Program
{
    /// <summary>
    /// The program's local server, if we're hosting one.
    /// </summary>
    public static Server? localServer;

    /// <summary>
    /// The program's local client, we use this guy to connect to other servers 'n' stuff.
    /// </summary>
    public static Client? localClient;

    /// <summary>
    /// Determines whether or not the program is actually active.
    /// </summary>
    private static bool active = false;
    
    /// <summary>
    /// Initializes the program.
    /// </summary>
    public static void Main()
    {
        // Create our local client
        localClient = Client.Initialize("Weird Guy");

        // We should start by hosting a localhost server
        localServer = Server.Initialize(IPAddress.Loopback, 1337);

        // Connect to the localhost server
        localClient.ConnectToServer(localServer);

        // We're now active!
        active = true;

        // The main update loop
        while (active)
        {
            // Update the local server
            localServer.Update();
        }
    }
}