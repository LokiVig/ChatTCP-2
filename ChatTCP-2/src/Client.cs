using System.Net;
using System.Net.Sockets;

namespace ChatTCP;

/// <summary>
/// A client that can be connected to a server, has a username, etc.
/// </summary>
public class Client
{
    /// <summary>
    /// The visual username of this client.
    /// </summary>
    public string? Username;

    /// <summary>
    /// The server we're connected to.
    /// </summary>
    public IPEndPoint? ConnectedServer;

    /// <summary>
    /// The client's socket.
    /// </summary>
    private Socket? socket;

    /// <summary>
    /// The client's local endpoint.
    /// </summary>
    private IPEndPoint? localEndPoint;

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

        // Create a new client with the specified username
        Client res = new Client();
        res.Username = username;
        res.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        res.localEndPoint = new IPEndPoint(IPAddress.Any, 27015);
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
        // Call the regular ConnectToServer method with the server's address and port
        return ConnectToServer(server.GetLocalEndPoint());
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
                // Disconnect us!
                socket.Disconnect(true);
            }

            // Connect to the server
            socket.Connect(endpoint);

            // Start listening
            StartListeningAsync();

            // Send our username to the server
            socket.SendTo(Packet.FromString(Username!).Data, endpoint);

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
    public NetworkResult SendMessage(string msg)
    {
        // Send a packet containing our username and message
        return SendPacket(Packet.FromString($"\"{Username}\" - {msg}"));
    }

    /// <summary>
    /// Sends a message to a specific <see cref="Client"/>.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    /// <param name="client">The client we wish to send a message to.</param>
    public NetworkResult SendMessage(string msg, Client client)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Starts listening for incoming <see cref="Packet"/>s.
    /// </summary>
    private async Task StartListeningAsync()
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

        try
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
                        ReceivePacket(receivedData);
                    }
                }
                catch (SocketException exc)
                {
                    // Log about it and stop listening!
                    Log.Error($"{{Client}} Socket exception caught while receiving packet!\n\"{exc.Message}\"");
                    StopListening();
                }
            }
        }
        catch (Exception exc)
        {
            // Log any unexpected exceptions
            Log.Error($"{{Client}} Unexpected exception in listening loop!\n\"{exc.Message}\"");
            StopListening();
        }
    }

    /// <summary>
    /// Stops listening for incoming packets.
    /// </summary>
    private void StopListening()
    {
        isListening = false;
        socket?.Disconnect(true);
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
            GetSocket().SendTo(packet.Data, ConnectedServer);

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
    /// <param name="data">The data we received.</param>
    public NetworkResult ReceivePacket(byte[] data)
    {
        // Get a packet from the data
        Packet packet = Packet.FromData(data);

        // Do different things depending on the received packet's header
        switch (packet.GetHeader())
        {
            default:
            case PacketHeader.Invalid:
                Log.Error("Invalid packet header!");
                return NetworkResult.Error;

            // We've received a string! Write it to the console
            case PacketHeader.String:
                Console.WriteLine(Packet.ToString(Packet.FromData(data)));
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
    public ref Socket GetSocket()
    {
        return ref socket!;
    }

    /// <summary>
    /// Get this <see cref="Client"/>'s <see cref="IPEndPoint"/>.
    /// </summary>
    /// <returns>This <see cref="Client"/>'s <see cref="IPEndPoint"/>.</returns>
    public ref IPEndPoint GetEndPoint()
    {
        return ref localEndPoint!;
    }
}