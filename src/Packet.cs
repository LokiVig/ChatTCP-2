using System.Text;

namespace ChatTCP;

/// <summary>
/// A packet with a byte array for data.
/// </summary>
public struct Packet
{
    /// <summary>
    /// The data of this packet, as a byte array.
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// Creates a new <see cref="Packet"/> that only contains header information. This should be used to tell users to e.g. update.
    /// </summary>
    /// <param name="header">The <see cref="PacketHeader"/> we wish to send.</param>
    /// <returns>A new <see cref="Packet"/> containing only the specified <paramref name="header"/> for data.</returns>
    public static Packet FromHeader(PacketHeader header)
    {
        // Create a new packet containing only the header for data
        return new Packet() { Data = Encoding.UTF8.GetBytes($"{header}") };
    }

    /// <summary>
    /// Creates a new <see cref="Packet"/> with data from the provided <see langword="string"/>.
    /// </summary>
    /// <param name="str">The string we wish to parse into a <see cref="Packet"/>.</param>
    /// <returns>A new <see cref="Packet"/> with data from the provided <see langword="string"/>.</returns>
    public static Packet FromString(string str)
    {
        // Create a new packet with the header information of being a string, data being the string itself as bytes
        return new Packet() { Data = Encoding.UTF8.GetBytes($"{PacketHeader.String}|{str}") };
    }

    /// <summary>
    /// Creates a new <see cref="Packet"/> from the provided data.
    /// </summary>
    /// <param name="data">The data we've received.</param>
    /// <returns>A new <see cref="Packet"/> from the provided data.</returns>
    public static Packet FromData(byte[] data)
    {
        // Create a new packet with data from the argument
        return new Packet() { Data = data };
    }

    /// <summary>
    /// Gets the <see cref="PacketHeader"/> info from the specified <see cref="Packet"/>.
    /// </summary>
    /// <param name="packet">The <see cref="Packet"/> we wish to get the <see cref="PacketHeader"/> from.</param>
    /// <returns>The <see cref="PacketHeader"/> from the argument <see cref="Packet"/>.</returns>
    public static PacketHeader GetHeader(Packet packet)
    {
        // Get the packet's data as a string
        string data = Encoding.UTF8.GetString(packet.Data);

        // The index of the separator character
        int separationIdx = -1;

        // Check if there's a separator character...
        if ((separationIdx = data.IndexOf('|')) != -1)
        {
            // Get the first section of the text
            string header = data.Substring(0, separationIdx);

            // Check against every header type...
            foreach (PacketHeader value in Enum.GetValues(typeof(PacketHeader)))
            {
                // If the argument header matches the value...
                if (string.Compare(header, value.ToString(), true) == 0)
                {
                    // Return it!
                    return value;
                }
            }
        }
        else // Otherwise we've hopefully only got header info...
        {
            // Check against every header type...
            foreach (PacketHeader value in Enum.GetValues(typeof(PacketHeader)))
            {
                // If the data matches the value...
                if (string.Compare(data, value.ToString(), true) == 0)
                {
                    // Return it!
                    return value;
                }
            }
        }

        // We couldn't find any header info!
        Log.Error("Couldn't get header information for packet!");
        return PacketHeader.Invalid;
    }

    /// <summary>
    /// Translates a <see cref="Packet"/> to a <see langword="string"/>.
    /// </summary>
    /// <param name="packet">The packet we wish to translate from.</param>
    /// <returns>The data of the provided <see cref="Packet"/> as a <see langword="string"/>.</returns>
    public static string ToString(Packet packet)
    {
        // Encode the data to string
        string data = Encoding.UTF8.GetString(packet.Data);

        // Ensure we have a pipe differing the header to the data
        int delimiterIndex = data.IndexOf('|');

        // Get the header
        PacketHeader header = GetHeader(packet);

        // Get the content
        string content = data.Substring(delimiterIndex + 1);

        // If the header isn't a string...
        if (header != PacketHeader.String)
        {
            // We can't do shit!
            throw new Exception("Header does not match string!");
        }

        // Return the parsed content!
        return content;
    }
}