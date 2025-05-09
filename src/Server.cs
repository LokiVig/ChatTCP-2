using System.Net;
using System.Net.Sockets;

namespace ChatTCP;

/// <summary>
/// A server with a list of connected clients, hosted from a specified address and port.
/// </summary>
public class Server : IDisposable
{
    /// <summary>
    /// The IP address this server is hosting from.
    /// </summary>
    public IPAddress? Address;

    /// <summary>
    /// The port this server is hosting from.
    /// </summary>
    public int Port;

    /// <summary>
    /// The actual networking socket of this server.
    /// </summary>
    private Socket? socket;

    /// <summary>
    /// A local end point for this server.
    /// </summary>
    private IPEndPoint? localEndPoint;

    /// <summary>
    /// The list of clients connected to this server.
    /// </summary>
    private List<Client> connectedClients = new();

    /// <summary>
    /// Initializes a new <see cref="Server"/> with the provided IP address and port.
    /// </summary>
    /// <param name="addr">The IP address we wish to host from.</param>
    /// <param name="port">The port we wish to host from.</param>
    /// <returns>A new <see cref="Server"/>.</returns>
    public static Server Initialize(IPAddress? addr = null, int? port = null)
    {
        // Log some information
        Log.Info($"{{Server}} Initializing server @ {addr}:{port}...");

        // Create a new server
        Server res = new Server();

        // Set our address and port
        // Default, if either are null, is {IPAddress.Any}:27015
        res.Address = addr ?? IPAddress.Any;
        res.Port = port ?? 27015;

        // Set our local endpoint
        res.localEndPoint = new IPEndPoint(res.Address, res.Port);

        // Create a socket
        res.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        res.socket.Blocking = false; // We shouldn't be in blocking mode
        res.socket.Bind(res.localEndPoint); // Bind us to our local endpoint
        res.socket.Listen(); // Start listening!

        // Return the result
        return res;
    }

    /// <summary>
    /// Closes and disposes of this server.
    /// </summary>
    public void Close()
    {
        // For every client connected...
        foreach (Client cl in connectedClients)
        {
            // Tell them to disconnect!
            cl.GetSocket()?.Disconnect(true);
        }

        // Dispose of ourselves
        Dispose();
    }

    /// <summary>
    /// Sends a message globally to all connected sockets.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    public NetworkResult SendMessage(string msg, bool server = false)
    {
        // Send a packet with the message
        return SendPacket(Packet.FromString($@"[{DateTime.Now:HH\:mm}] " + $"{(server ? "[SERVER] " : "")}{msg}"));
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    /// <param name="recipient">The client to receive our message.</param>
    public NetworkResult SendMessage(string msg, Client recipient, bool server = false)
    {
        // Send a packet with the message
        return SendPacket(Packet.FromString($@"[{DateTime.Now:HH\:mm}] " + $"{(server ? "[SERVER] " : "")}{msg}"), recipient);
    }

    /// <summary>
    /// Updates this server.
    /// </summary>
    public void Update()
    {
        // If the socket is invalid...
        if (socket == null || !socket.IsBound)
        {
            // Throw an exception
            throw new Exception("Socket is invalid!");
        }

        // If our socket is polling...
        if (socket.Poll(0, SelectMode.SelectRead))
        {
            // Accept the newly connected socket!
            Socket connectedSocket = socket.Accept();
            OnAcceptConnection(connectedSocket);
        }

        // For every client that's connected...
        foreach (Client cl in connectedClients)
        {
            // If the client's socket is invalid...
            if (cl.GetSocket() == null || !cl.GetSocket()!.Connected)
            {
                // They're disconnected! Send a message to everyone of such and remove them from the list of clients
                connectedClients.Remove(cl);
                SendMessage($"User \"{cl.Username}\" has disconnected!", server: true);
                continue;
            }

            if (cl.GetSocket()!.Available > 0)
            {
                // Create a new buffer and amount of bytes read
                byte[] buffer = new byte[1024];
                int read = cl.GetSocket()!.Receive(buffer);

                // If we read any bytes...
                if (read > 0)
                {
                    // Get the data, copy it into a new array, and receive it as a packet!
                    byte[] data = new byte[read];
                    Array.Copy(buffer, data, read);
                    ReceivePacket(Packet.FromData(data), cl);
                }
            }
        }
    }

    /// <summary>
    /// Sends a packet to all clients.
    /// </summary>
    /// <param name="packet">The packet we wish to send.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeded, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult SendPacket(Packet packet)
    {
        // For every connected client...
        foreach (Client cl in connectedClients)
        {
            // If we failed sending a packet...
            if (SendPacket(packet, cl) != NetworkResult.OK)
            {
                // Return an error response!
                return NetworkResult.Error;
            }
        }

        // Successful operation!
        return NetworkResult.OK;
    }

    /// <summary>
    /// Sends a packet to a specific client.
    /// </summary>
    /// <param name="packet">The packet we wish to send.</param>
    /// <param name="recipient">The client who should receive the packet.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeded, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult SendPacket(Packet packet, Client recipient)
    {
        // Send the packet to the recipient's socket
        return SendPacket(packet, recipient.GetSocket()!);
    }

    /// <summary>
    /// Sends a packet to a specific socket.
    /// </summary>
    /// <param name="packet">The packet we wish to send.</param>
    /// <param name="recipient">The socket of the reciever.</param>
    /// <returns><see cref="NetworkResult.OK"/> if it succeeded, <see cref="NetworkResult.Error"/> otherwise.</returns>
    public NetworkResult SendPacket(Packet packet, Socket recipient)
    {
        try
        {
            // Ensure the recipient is valid...
            if (recipient == null || !recipient.Connected)
            {
                return NetworkResult.Error;
            }

            // Send the packet to the recipient
            recipient.Send(packet.Data);

            // Return a successful operation!
            return NetworkResult.OK;
        }
        catch (Exception exc) // If we get an exception...
        {
            // Write to the console the error and return an unsuccessful network interaction!
            Log.Error($"{{Server}} Error occurred when trying to send packet!\n\"{exc.Message}\"");
            return NetworkResult.Error;
        }
    }

    /// <summary>
    /// Receives a packet from a client.
    /// </summary>
    /// <param name="packet">The <see cref="Packet"/> we've received.</param>
    /// <returns>The result of the network function.</returns>
    public NetworkResult ReceivePacket(Packet packet, Client? client = null)
    {
        // Do different things depending on the received packet's header
        switch (packet.GetHeader())
        {
            // Invalid packets
            default:
            case PacketHeader.Invalid:
                Log.Error("Invalid packet header!");
                return NetworkResult.Error;

            // We received a message!
            case PacketHeader.String:
                // If the packet has metadata...
                if (packet.HasMetadata())
                {
                    // For every client...
                    foreach (Client cl in connectedClients)
                    {
                        // If the second metadata variable is a connected client's username...
                        if (packet.GetMetadata()?[1] == cl.Username)
                        {
                            // Send a private message to this client!
                            return SendMessage($"[From {packet.GetMetadata()?[0]}, To {cl.Username}] {packet.ToString()}", cl);
                        }
                    }

                    // We couldn't find any applicable information to do with this metadata-filled message!
                    return NetworkResult.Error;
                }

                // Send a message to every client
                return SendMessage(packet.ToString());
        }
    }

    /// <summary>
    /// Things to do when we've just accepted a connection.
    /// </summary>
    private void OnAcceptConnection(Socket connectedSocket)
    {
        // The username of the connection
        string? username = null;

        // Ask the just connected socket for some information about themselves
        byte[] buffer = new byte[256];
        int bytesRead = connectedSocket.Receive(buffer);

        // If we have read any bytes...
        if (bytesRead > 0)
        {
            // Parse them from bytes to string
            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, receivedData, bytesRead);
            username = Packet.FromData(receivedData).ToString();
        }

        Log.Info($"{{Server}} Accepted connection of user \"{username}\"!");

        // Create a new client from the information we just gathered
        Client client = new Client()
        {
            Username = username ?? "Unknown User"
        };

        // Set the client's socket appropriately
        client.SetSocket(connectedSocket);

        // Add it to the list of connected clients and log a new join!
        connectedClients.Add(client);
        SendMessage($"User \"{client.Username}\" has joined the server!", server: true);
    }

    /// <summary>
    /// Sets this <see cref="Server"/>'s <see cref="Socket"/>.
    /// </summary>
    /// <param name="socket">The <see cref="Socket"/> we wish this <see cref="Server"/> to use.</param>
    public void SetSocket(Socket socket)
    {
        this.socket = socket;
    }

    /// <summary>
    /// Gets this <see cref="Server"/>'s <see cref="Socket"/>.
    /// </summary>
    /// <returns>This <see cref="Server"/>'s <see cref="Socket"/>.</returns>
    public Socket GetSocket()
    {
        return socket!;
    }

    /// <summary>
    /// Gets this <see cref="Server"/>'s <see cref="IPEndPoint"/>.
    /// </summary>
    /// <returns>This <see cref="Server"/>'s <see cref="IPEndPoint"/>.</returns>
    public IPEndPoint GetEndPoint()
    {
        return localEndPoint!;
    }

    public void Dispose()
    {
        // Dispose of variables!
        socket?.Close();
        connectedClients.Clear();
    }
}