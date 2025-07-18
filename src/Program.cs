﻿using System.Net;

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
        // Create our local client with a specified username
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

            // If our input handling didn't have an okay network result...
            if (HandleInput() != NetworkResult.OK)
            {
                // Throw an exception!
                throw new Exception("A network function while handling inputs resulted in an error!");
            }

            // Make the while loop take it easy! Don't wanna overload the CPU
            await Task.Delay(25);
        }

        // Dispose of the local client
        LocalClient.Dispose();

        // Dispose of the server
        LocalServer.Close();
    }

    /// <summary>
    /// Draws all of the messages from the server.
    /// </summary>
    private static void DrawMessages()
    {
        // Get the list of messages
        List<string> messages = LocalClient!.ReceivedMessages;
        
        // Clear the console
        Console.Clear();

        // For every message...
        foreach (string message in messages)
        {
            // Write it!
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Handles user inputs.
    /// </summary>
    private static NetworkResult? HandleInput()
    {
        // Set our console cursor to the bottom of the screen
        Console.SetCursorPosition(0, Console.WindowHeight - 1);

        // Show a lil' '>' for the user input
        Console.Write("> ");

        // Offset our cursor from the '>'
        Console.SetCursorPosition(2, Console.WindowHeight - 1);

        // Take the user's input
        string input = Console.ReadLine() ?? string.Empty;

        // Connects to a new server with the specified address and port
        if (input.Contains("/connect") ||
            input.Contains("/conn"))
        {
            // Get the variables
            List<string> variables = input.Split(" ").ToList();
            variables.RemoveAt(0); // Remove the first value, that's just the command

            // If we have an invalid amount of arguments for this command...
            if (variables.Count < 2 || variables.Count > 2)
            {
                // Send a local message telling us we passed an invalid count!
                return LocalClient?.SendMessage($"Failed to connect to a server; Invalid argument count passed!", ["CLIENT", LocalClient?.Username]);
            }

            // If we failed parsing the IP address...
            if (!IPAddress.TryParse(variables[0], out IPAddress? addr))
            {
                // Send a local message telling us we failed!
                return LocalClient?.SendMessage($"Failed connecting to {variables[0]}:{variables[1]}; IP address failed to be parsed.", ["CLIENT", LocalClient?.Username]);
            }

            // If we failed parsing the port...
            if (!int.TryParse(variables[1], out int port))
            {
                // Send a local message telling us we failed!
                return LocalClient?.SendMessage($"Failed connecting to {variables[0]}:{variables[1]}; Port failed to be parsed.", ["CLIENT", LocalClient?.Username]);
            }

            // Try to connect to a new server with these specifics!
            return LocalClient?.ConnectToServer(new IPEndPoint(addr, port));
        }

        // Gets connection information about the server we're on right now
        if (input.Contains("/connectioninfo") ||
            input.Contains("/conninfo"))
        {
            // Send a message to ourselves as though we're the server
            return LocalClient?.SendMessage($"Connected to: {LocalServer?.GetEndPoint()}", ["SERVER", LocalClient?.Username]);
        }

        // We should shut down!
        if (input.Contains("/exit") ||
            input.Contains("/quit"))
        {
            active = false;
            return NetworkResult.OK;
        }

        // If we didn't input emptiness...
        if (!string.IsNullOrEmpty(input))
        {
            // If the input contains an '@' symbol...
            if (input.Contains('@'))
            {
                // We're sending a DM to the person we're @'ing!
                return LocalClient?.SendMessage(input.Split('@')[0].TrimEnd(), [LocalClient?.Username, input.Split('@')[1]]);
            }

            // Send the input!
            return LocalClient?.SendMessage($"{LocalClient?.Username}: {input}");
        }

        // We didn't do anything, which is okay!
        return NetworkResult.OK;
    }
}