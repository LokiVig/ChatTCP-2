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

            // Draw our messages
            DrawMessages();

            // Handle our inputs
            HandleInput();

            // Make the while loop take it easy! Don't wanna overload the CPU
            await Task.Delay(25);
        }

        // Dispose of the server
        LocalServer.Close();
    }

    /// <summary>
    /// Draws all of the messages from the server.
    /// </summary>
    private static void DrawMessages()
    {
        // Clear the console
        Console.Clear();

        // Get our list of messages
        List<string> messages = LocalClient!.ReceivedMessages;

        // For every message in our received messages...
        for (int i = 0; i < messages.Count; i++)
        {
            Console.SetCursorPosition(0, i);
            Console.WriteLine(messages[i]);
        }
    }

    /// <summary>
    /// Handles user inputs.
    /// </summary>
    private static void HandleInput()
    {
        // Set our console cursor to the bottom of the screen
        Console.SetCursorPosition(0, Console.BufferHeight - 1);

        // Show a lil' '>' for the user input
        Console.Write("> ");

        // Offset our cursor from the '>'
        Console.SetCursorPosition(2, Console.BufferHeight - 1);

        // Take the user's input
        string input = Console.ReadLine() ?? string.Empty;

        // Depending on the input...
        switch (input)
        {
            // If we didn't do anything special, just write a message...
            default:
                LocalClient?.SendMessage($"{LocalClient?.Username}: {input}"); // Send our input as a message!
                break;

            // Gets connection information about the server
            case "/connection":
            case "/conn":
                // Send a message to ourselves as though we're the server
                LocalClient?.SendMessage($"Connected to: {LocalServer?.GetEndPoint()}", ["SERVER", LocalClient?.Username]);
                break;
        }
    }
}