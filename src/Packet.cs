using System.Text;

namespace ChatTCP;

/// <summary>
/// A packet with a <see langword="byte"/> array for data.
/// </summary>
public struct Packet
{
    /// <summary>
    /// The data of this <see cref="Packet"/>, as a <see langword="byte"/> array.
    /// </summary>
    public byte[] Data;

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
    /// Creates a new <see cref="Packet"/> with data from the provided <see langword="int"/>.
    /// </summary>
    /// <param name="integer">The <see langword="int"/> we wish to parse into a <see cref="Packet"/>.</param>
    /// <returns>A new <see cref="Packet"/> with data from the provided <see langword="int"/>.</returns>
    public static Packet FromInteger(int integer)
    {
        // Create a new packet with the header information of being an integer, data being the integer itself as bytes
        return new Packet() { Data = Encoding.UTF8.GetBytes($"{PacketHeader.Integer}|{integer}") };
    }

    /// <summary>
    /// Creates a new <see cref="Packet"/> with data from the provided <see langword="string"/>.
    /// </summary>
    /// <param name="str">The <see langword="string"/> we wish to parse into a <see cref="Packet"/>.</param>
    /// <returns>A new <see cref="Packet"/> with data from the provided <see langword="string"/>.</returns>
    public static Packet FromString(string str)
    {
        // Create a new packet with the header information of being a string, data being the string itself as bytes
        return new Packet() { Data = Encoding.UTF8.GetBytes($"{PacketHeader.String}|{str}") };
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
        int separatorIdx = -1;

        // Check if there's a separator character...
        if ((separatorIdx = data.IndexOf('|')) != -1)
        {
            // Get the first section of the text
            string header = data.Substring(0, separatorIdx);

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
    /// Gets the <see cref="PacketHeader"/> of this <see cref="Packet"/>.
    /// </summary>
    /// <returns>The <see cref="PacketHeader"/> of this <see cref="Packet"/>.</returns>
    public PacketHeader GetHeader()
    {
        // Simply call the static method with this as its argument
        return GetHeader(this);
    }

    /// <summary>
    /// Gets extra metadata from a <see cref="Packet"/>.
    /// </summary>
    /// <param name="packet">The <see cref="Packet"/> we wish to get metadata from.</param>
    /// <returns>The list of metadata variables in the specified <see cref="Packet"/>, or <see langword="null"/>.</returns>
    public static List<string> GetMetadata(Packet packet)
    {
        // The metadata we're to fill
        List<string>? metadata = null;

        // If the packet doesn't have metadata...
        if (!HasMetadata(packet, out metadata))
        {
            // We have an exception!
            throw new Exception();
        }

        // Return the found metadata!
        return metadata;
    }

    /// <summary>
    /// Gets extra metadata from this <see cref="Packet"/>.
    /// </summary>
    /// <returns>The list of metadata variables in this <see cref="Packet"/>, or <see langword="null"/>.</returns>
    public List<string> GetMetadata()
    {
        // Simply call the static method with this as its argument
        return GetMetadata(this);
    }

    /// <summary>
    /// Checks whether or not the specified <see cref="Packet"/> has any metadata.
    /// </summary>
    /// <param name="packet">The <see cref="Packet"/> we wish to check for metadata within.</param>
    /// <param name="metadata">The list of metadata objects we gather.</param>
    /// <returns><see langword="true"/> if it does, <see langword="false"/> otherwise.</returns>
    public static bool HasMetadata(Packet packet, out List<string> metadata)
    {
        // Get the data of the packet
        string data = packet.ToString(true);

        // For every '&' we should separate it
        metadata = data.Split('&').ToList();
        metadata.RemoveAt(0); // Remove the first index, that's just our regular packet info

        // If the amount of items in the array is > 0 or it's not null...
        if (metadata != null && 
            metadata?.Count > 0)
        {
            // Return true!
            return true;
        }

        // We couldn't find any metadata! Get outta here
        return false;
    }

    /// <inheritdoc cref="HasMetadata(Packet, out List{string}?)"/>
    public static bool HasMetadata(Packet packet)
    {
        // Simply call the regular method with nothing out'd
        return HasMetadata(packet, out _);
    }

    /// <summary>
    /// Checks whether or not this <see cref="Packet"/> has any metadata.
    /// </summary>
    /// <returns><see langword="true"/> if it does, <see langword="false"/> otherwise.</returns>
    public bool HasMetadata()
    {
        // Simply call the static method with this as its argument
        return HasMetadata(this);
    }

    /// <summary>
    /// Translates a <see cref="Packet"/> to <see langword="int"/>.
    /// </summary>
    /// <param name="packet">The <see cref="Packet"/> we wish to translate from.</param>
    /// <returns>The data of the provided <see cref="Packet"/> as <see langword="int"/>.</returns>
    public static int ToInteger(Packet packet)
    {
        // Encode the data to string
        string data = Encoding.UTF8.GetString(packet.Data);

        // Get the header
        PacketHeader header = packet.GetHeader();

        // Ensure we have a pipe differing the header to the data
        int separatorIdx = data.IndexOf('|');

        // Get the content
        string content = data.Substring(separatorIdx + 1);

        // If the header isn't an integer...
        if (header != PacketHeader.Integer)
        {
            // We can't do shit!
            throw new Exception("This packet is not an integer, as per its header!");
        }

        // Return the parsed content!
        return int.Parse(content);
    }

    /// <summary>
    /// Translates this <see cref="Packet"/> to a <see langword="int"/>.
    /// </summary>
    /// <returns>The data of this <see cref="Packet"/> as <see langword="int"/>.</returns>
    public int ToInteger()
    {
        // Simply call the static method with this as its argument
        return ToInteger(this);
    }

    /// <summary>
    /// Translates a <see cref="Packet"/> to <see langword="string"/>.
    /// </summary>
    /// <param name="packet">The <see cref="Packet"/> we wish to translate from.</param>
    /// <returns>The data of the provided <see cref="Packet"/> as <see langword="string"/>.</returns>
    public static string ToString(Packet packet, bool wantsMetadata = false)
    {
        // Encode the data to string
        string data = Encoding.UTF8.GetString(packet.Data);

        // Get the header
        PacketHeader header = packet.GetHeader();

        // Ensure we have a pipe differing the header to the data
        int separatorIdx = data.IndexOf('|');

        // Get the content
        string content = data.Substring(separatorIdx + 1);

        // If the header isn't a string...
        if (header != PacketHeader.String)
        {
            // We can't do shit!
            throw new Exception("This packet is not a string, as per its header!");
        }

        // If we don't want metadata...
        if (!wantsMetadata)
        {
            // If the packet has metadata...
            if (packet.HasMetadata())
            {
                // Remove any metadata tags from the content!
                content = content.Split('&')[0];
            }
        }

        // Return the parsed content!
        return content;
    }

    /// <summary>
    /// Translates this <see cref="Packet"/> to a <see langword="string"/>.
    /// </summary>
    /// <returns>The data of this <see cref="Packet"/> as <see langword="string"/>.</returns>
    public string ToString(bool wantsMetadata = false)
    {
        // Simply call the static method with this as its argument
        return ToString(this, wantsMetadata);
    }
}