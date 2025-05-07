using System.Net;
using System.Net.Sockets;

namespace ChatTCP;

/// <summary>
/// A client that can be connected to a server, with a username, etc.
/// </summary>
public class Client
{
    /// <summary>
    /// The visual username of this client.
    /// </summary>
    public string? Username;

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
    /// Starts listening for incoming <see cref="Packet"/>s.
    /// </summary>
    public void StartListening()
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

        // Run the listening loop in a separate thread
        Thread listenerThread = new Thread(() =>
        {
            while (isListening)
            {
                try
                {
                    // Buffer to store incoming data
                    byte[] buffer = new byte[1024];
                    int bytesRead = socket.Receive(buffer);

                    if (bytesRead > 0)
                    {
                        // Pass the received data to ReceivePacket
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, receivedData, bytesRead);
                        ReceivePacket(receivedData);
                    }
                }
                catch (SocketException exc) // If we catch an exception...
                {
                    // Log about it and stop listening!
                    Log.Error($"{{Client}} Socket exception caught while receiving packet!\n\"{exc.Message}\"");
                    StopListening();
                }
            }
        });

        listenerThread.IsBackground = true;
        listenerThread.Start();
    }

    /// <summary>
    /// Stops listening for incoming packets.
    /// </summary>
    public void StopListening()
    {
        isListening = false;
        socket?.Close();
    }

    /// <summary>
    /// Connects to a server from the specified server argument.
    /// </summary>
    /// <param name="server">The server we wish to connect to.</param>
    public NetworkResult ConnectToServer(Server server)
    {
        // Call the regular ConnectToServer method with the server's address and port
        return ConnectToServer(server.GetLocalEndPoint());
    }

    /// <summary>
    /// Connects to a server with the specified address and port.
    /// </summary>
    /// <param name="addr">The IP address of the server we wish to connect to.</param>
    /// <param name="port">The port we wish to connect to.</param>
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
            StartListening();

            // Send our username to the server
            socket.SendTo(Packet.FromString(Username!).Data, endpoint);

            // Log some information and return a successful operation
            Log.Info($"{{Client}} Successfully connected to server \"{endpoint}\"!");
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
    /// Receive information from a packet.
    /// </summary>
    /// <param name="data">The data we received.</param>
    public void ReceivePacket(byte[] data)
    {
        // Get a packet from the data
        Packet packet = Packet.FromData(data);

        // Do different actions dependant on the received packet's header
        switch (Packet.GetHeader(packet))
        {
            case PacketHeader.String:
                Console.WriteLine(Packet.ToString(packet)); // Log the message
                return;
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
    /// Returns this <see cref="Client"/>'s <see cref="Socket"/>.
    /// </summary>
    /// <returns>This <see cref="Client"/>'s <see cref="Socket"/>.</returns>
    public Socket GetSocket()
    {
        return socket!;
    }
}