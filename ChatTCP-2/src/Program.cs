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
    public static async Task Main()
    {
        // Create our local client
        LocalClient = Client.Initialize("Lokiv");

        // We should start by hosting a localhost server
        LocalServer = Server.Initialize(IPAddress.Loopback, 1337);

        // If we failed to connect to the local server...
        if (LocalClient.ConnectToServer(LocalServer) != NetworkResult.OK)
        {
            // Throw an exception!
            throw new Exception("Local client failed to connect to the local server!");
        }

        // We're now active!
        active = true;

        // The main update loop
        while (active)
        {
            // Update the local server
            LocalServer.Update();

            // Send a debug message!
            if (LocalClient.SendMessage("Debug!") != NetworkResult.OK)
            {
                throw new Exception();
            }

            // Make the while loop take it easy! Don't wanna overload the CPU
            await Task.Delay(10);
        }

        // Dispose of the server
        LocalServer.Close();
    }
}