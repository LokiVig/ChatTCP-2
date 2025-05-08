using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;

namespace ChatTCP;

/// <summary>
/// A server with a list of connected users, hosted from a specified address and port.
/// </summary>
public class Server
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
    /// Initializes a new server with the provided address and port.
    /// </summary>
    /// <param name="addr">The IP Address we wish to host from.</param>
    /// <param name="port">The port we wish to host from.</param>
    /// <returns>A new <see cref="Server"/>.</returns>
    public static Server Initialize(IPAddress? addr = null, int? port = null)
    {
        // Log some information
        Log.Info($"{{Server}} Initializing server at address \"{addr}:{port}\"...");

        // Create a new server, possibly with the specified address and port
        // If either are null, they'll be their default values
        Server res = new Server();
        res.Address = addr ?? IPAddress.Any;
        res.Port = port ?? 27015;
        res.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        res.localEndPoint = new IPEndPoint(res.Address, res.Port);
        res.socket.Bind(res.localEndPoint);
        res.socket.Listen();

        // Return the result
        return res;
    }

    /// <summary>
    /// Sends a message globally to all connected sockets.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    public NetworkResult SendMessage(string msg)
    {
        // Send a packet with the message
        return SendPacket(Packet.FromString($"[SERVER] {msg}", PacketHeader.ServerMessage));
    }

    /// <summary>
    /// Sends a message to a specific client.
    /// </summary>
    /// <param name="msg">The message we wish to send.</param>
    /// <param name="client">The client to receive our message.</param>
    public NetworkResult SendMessage(string msg, Client client)
    {
        // Send a packet with the message
        return SendPacket(Packet.FromString($"[SERVER] {msg}", PacketHeader.ServerMessage), client);
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

        // Buffer socket, holds the value of the socket's acception, null otherwise
        Socket? connectedSocket = null;

        // If we just accepted a connection...
        if ((connectedSocket = socket.Accept()) != null)
        {
            // Do the special things to do!
            OnAcceptConnection(connectedSocket);
        }

        // Create a new buffer and amount of bytes read
        byte[] buffer = new byte[1024];
        int bytesRead = socket.Receive(buffer);

        // If we read any bytes...
        if (bytesRead > 0)
        {
            // Get the data, copy it into a new array, and receive it as a packet!
            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, receivedData, bytesRead);
            ReceivePacket(receivedData);
        }

        // For every client that's connected...
        foreach (Client cl in connectedClients)
        {

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
        return SendPacket(packet, recipient.GetSocket());
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
    /// <param name="data">The data we've received.</param>
    /// <returns>The result of the network function.</returns>
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

            // We should send the received message to every user in the server!
            case PacketHeader.UserMessage:
                SendMessage(packet.ToString());
                return NetworkResult.OK;

            case PacketHeader.String:
                return NetworkResult.OK;
        }
    }

    /// <summary>
    /// Things to do when we've just accepted a connection!
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
        SendMessage($"User \"{client.Username}\" has joined the server!");
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
    public IPEndPoint GetLocalEndPoint()
    {
        return localEndPoint!;
    }
}