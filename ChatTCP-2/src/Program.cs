using System.Net;

namespace ChatTCP;

/// <summary>
/// A client of ChatTCP, can join servers and chat like a regular user.
/// </summary>
public class Program
{
    /// <summary>
    /// The program's local server, if we're hosting one.
    /// </summary>
    public static Server? LocalServer;

    /// <summary>
    /// The program's local client, we use this guy to connect to other servers 'n' stuff.
    /// </summary>
    public static Client? LocalClient;

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
        LocalClient = Client.Initialize("Username");

        // We should start by hosting a localhost server
        LocalServer = Server.Initialize(IPAddress.Loopback, 1337);

        // Connect to the localhost server
        LocalClient.ConnectToServer(LocalServer);

        // We're now active!
        active = true;

        // The main update loop
        while (active)
        {
            // Update the local server
            LocalServer.Update();

            // Send a debug message
            if (LocalClient.SendMessage("Debug!") != NetworkResult.OK)
            {
                throw new Exception();
            }
        }

        // Dispose of the server
        LocalServer.Close();
    }
}