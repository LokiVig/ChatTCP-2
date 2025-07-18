﻿using System.Net;
using System.Net.Sockets;

namespace ChatTCP;

/// <summary>
/// A client that can be connected to a server, has a username, etc.
/// </summary>
public class Client : IDisposable
{
    /// <summary>
    /// The visual username of this client.
    /// </summary>
    public string Username = string.Empty;

    /// <summary>
    /// The server we're connected to.
    /// </summary>
    public IPEndPoint? ConnectedServer;

    /// <summary>
    /// This client's list of received messages.
    /// </summary>
    public List<string> ReceivedMessages = new List<string>();

    /// <summary>
    /// The client's socket.
    /// </summary>
    private Socket? socket;

    /// <summary>
    /// The client's local endpoint.
    /// </summary>
    private IPEndPoint? localEndPoint;

    /// <summary>
    /// The thread used for listening.
    /// </summary>
    private Thread? listeningThread;

    /// <summary>
    /// Determines whether or not we should be listening.
    /// </summary>
    private bool isListening = false;

    /// <summary>
    /// Initializes a new <see cref="Client"/> with the provided arguments.
    /// </summary>
    /// <param name="username">The username of this client.</param>
    /// <returns>A new <see cref="Client"/>.</returns>
    public static Client Initialize(string username = "User")
    {
        // Log some information
        Log.Info($"{{Client}} Initializing user \"{username}\"...");

        // Create a new client
        Client res = new Client();

        // Specify our username
        res.Username = username;

        // Set our local endpoint
        res.localEndPoint = new IPEndPoint(IPAddress.Any, 27015);

        // Create a socket
        res.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        res.socket.Bind(res.localEndPoint);

        // Return the result
        return res;
    }

    /// <summary>
    /// Disconnects this client from the server it's connected to.
    /// </summary>
    public void Disconnect()
    {
        // Stop listening
        StopListening();

        // Log that we've disconnected!
        Log.Info($"We've disconnected from \"{ConnectedServer}\"!");

        // Null our connected server
        ConnectedServer = null;
    }

    /// <summary>
    /// Connects to a server from the specified server argument.
    /// </summary>
    /// <param name="server">The server we wish to connect to.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeded, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult ConnectToServer(Server server)
    {
        // Call the ConnectToServerAsync method with the server's endpoint
        return ConnectToServer(server.GetEndPoint());
    }

    /// <summary>
    /// Connects to a server with the specified endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint of the server we wish to join.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeded, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult ConnectToServer(IPEndPoint endpoint)
    {
        try
        {
            // If the socket is null...
            if (socket == null)
            {
                // Throw a new exception!
                Log.Error("{Client} Socket is invalid!");
                return NetworkResult.Error;
            }

            // If we're already connected...
            if (socket.Connected)
            {
                // Stop listening
                StopListening(closeSocket: false);

                // Disconnect us!
                socket?.Disconnect(true);
            }

            // Connect to the server
            socket?.Connect(endpoint);

            // Start listening
            StartListening();

            // Send our username to the server
            socket?.SendTo(Packet.FromString(Username!).Data, endpoint);

            // We're now connected to this server
            ConnectedServer = endpoint;

            // Log some information and return a successful operation
            Log.Info($"{{Client}} Successfully connected to server @ {endpoint}!");
            return NetworkResult.OK;
        }
        catch (SocketException exc) // If we get a socket exception...
        {
            // Log it!
            Log.Error($"{{Client}} Socket exception when trying to connect to a server!\n\"{exc.Message}\"");
            return NetworkResult.Error;
        }
    }

    /// <summary>
    /// Sends a message to everyone in the server.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeds, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult SendMessage(string msg)
    {
        // Send a packet containing our username and message
        return SendPacket(Packet.FromString(msg));
    }

    /// <summary>
    /// Sends a message to a specific <see cref="Client"/>.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    /// <param name="client">The client we wish to send a message to.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeds, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult SendMessage(string msg, Client client)
    {
        // Send a packet containing our username, message and some metadata specifying the receiving client
        return SendPacket(Packet.FromString($"{msg}&{Username}&{client.Username}"));
    }

    /// <summary>
    /// Sends a message with specific metadata.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    /// <param name="metadata">The metadata of the message we wish to send.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeds, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult SendMessage(string msg, List<string> metadata)
    {
        // The result string that we wish to send to the server
        string result = msg;

        // For every piece of metadata...
        for (int i = 0; i < metadata.Count; i++)
        {
            // Add it to the result string!
            result += $"&{metadata[i]}";
        }

        // Send a packet containing our resulting string
        return SendPacket(Packet.FromString(result));
    }

    /// <summary>
    /// Starts listening for incoming <see cref="Packet"/>s.
    /// </summary>
    private void StartListening()
    {
        // If the socket's invalid...
        if (socket == null || !socket.Connected)
        {
            // Log an error and return!
            Log.Error("{Client} Socket is invalid!");
            return;
        }

        // We're now listening!
        isListening = true;

        // A new thread, specifically for listening for new packets
        listeningThread = new Thread(async () =>
        {
            // Run the listening loop asynchronously
            while (isListening)
            {
                // Buffer to store incoming data
                byte[] buffer = new byte[1024];
                try
                {
                    // Asynchronously receive data
                    int bytesRead = await socket.ReceiveAsync(buffer, SocketFlags.None);

                    if (bytesRead > 0)
                    {
                        // Pass the received data to ReceivePacket
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, receivedData, bytesRead);
                        ReceivePacket(Packet.FromData(receivedData));
                    }
                }
                catch (SocketException exc)
                {
                    // Log about it and stop listening!
                    Log.Error($"{{Client}} Socket exception caught while receiving packet!\n\"{exc.Message}\"");
                    StopListening();
                }

                // Delay the task at the end, we don't wanna overload the CPU!
                await Task.Delay(25);
            }
        });

        // Start the listening thread!
        listeningThread.IsBackground = true;
        listeningThread.Start();
    }

    /// <summary>
    /// Stops listening for incoming packets.
    /// </summary>
    private void StopListening(bool closeSocket = true)
    {
        // Immediately stop listening!
        isListening = false;

        // If the listening thread is active...
        if (listeningThread != null)
        {
            // Wait for the thread to close
            listeningThread.Join();
        }

        // If we should close the socket...
        if (closeSocket)
        {
            // Tell it to close!
            socket?.Close();
        }
    }

    /// <summary>
    /// Sends a packet to the server.
    /// </summary>
    /// <param name="packet">The packet we wish to send to the server.</param>
    public NetworkResult SendPacket(Packet packet)
    {
        try
        {
            // Ensure the connected server is valid...
            if (ConnectedServer == null)
            {
                return NetworkResult.Error;
            }

            // Send the packet's data to the server
            GetSocket()?.SendTo(packet.Data, ConnectedServer);

            // Return okay!
            return NetworkResult.OK;
        }
        catch (Exception exc)
        {
            Log.Error($"{{Client}} Error occurred when trying to send packet!\n\"{exc.Message}\"");
            return NetworkResult.Error;
        }
    }

    /// <summary>
    /// Receive information from a packet.
    /// </summary>
    /// <param name="packet">The packet we received.</param>
    public NetworkResult ReceivePacket(Packet packet)
    {
        // Do different things depending on the received packet's header
        switch (packet.GetHeader())
        {
            // Invalid packet received!
            default:
            case PacketHeader.Invalid:
                Log.Error("Invalid packet header!");
                return NetworkResult.Error;

            // We've received a string!
            case PacketHeader.String:
                ReceivedMessages.Add(packet.ToString()); // Add it to the list of received messages
                return NetworkResult.OK;
        }
    }

    /// <summary>
    /// Sets this client's socket.
    /// </summary>
    /// <param name="socket">The socket we wish to set it to.</param>
    public void SetSocket(Socket socket)
    {
        this.socket = socket;
    }

    /// <summary>
    /// Gets this <see cref="Client"/>'s <see cref="Socket"/>.
    /// </summary>
    /// <returns>This <see cref="Client"/>'s <see cref="Socket"/>.</returns>
    public Socket? GetSocket()
    {
        return socket;
    }

    /// <summary>
    /// Get this <see cref="Client"/>'s <see cref="IPEndPoint"/>.
    /// </summary>
    /// <returns>This <see cref="Client"/>'s <see cref="IPEndPoint"/>.</returns>
    public IPEndPoint? GetEndPoint()
    {
        return localEndPoint;
    }

    public void Dispose()
    {
        // Disconnect from the current server
        Disconnect();

        // Close the socket
        socket?.Close();
    }
}