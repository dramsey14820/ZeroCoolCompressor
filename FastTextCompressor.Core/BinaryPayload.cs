namespace FastTextCompressor.Core;

internal static class BinaryPayload
{
    public static void Write7BitEncodedInt(Stream stream, int value)
    {
        uint remaining = (uint)value;
        while (remaining >= 0x80)
        {
            stream.WriteByte((byte)(remaining | 0x80));
            remaining >>= 7;
        }

        stream.WriteByte((byte)remaining);
    }

    public static int Read7BitEncodedInt(Stream stream)
    {
        var result = 0;
        var shift = 0;
        while (shift < 35)
        {
            var next = stream.ReadByte();
            if (next < 0)
            {
                throw new InvalidDataException("Unexpected end of compressed data.");
            }

            result |= (next & 0x7f) << shift;
            if ((next & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
        }

        throw new InvalidDataException("Invalid 7-bit encoded integer.");
    }

    public static bool TryRead7BitEncodedInt(Stream stream, out int value)
    {
        var first = stream.ReadByte();
        if (first < 0)
        {
            value = 0;
            return false;
        }

        value = first & 0x7f;
        var shift = 7;
        while ((first & 0x80) != 0 && shift < 35)
        {
            first = stream.ReadByte();
            if (first < 0)
            {
                throw new InvalidDataException("Unexpected end of compressed data.");
            }

            value |= (first & 0x7f) << shift;
            shift += 7;
        }

        if ((first & 0x80) != 0)
        {
            throw new InvalidDataException("Invalid 7-bit encoded integer.");
        }

        return true;
    }
}
