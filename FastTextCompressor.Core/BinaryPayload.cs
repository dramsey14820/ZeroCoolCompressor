namespace FastTextCompressor.Core;

internal static class BinaryPayload
{
    public static void Write7BitEncodedInt(Span<byte> buffer, ref int offset, int value)
    {
        uint remaining = (uint)value;
        while (remaining >= 0x80)
        {
            buffer[offset++] = (byte)(remaining | 0x80);
            remaining >>= 7;
        }

        buffer[offset++] = (byte)remaining;
    }

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

            if (shift == 28 && (next & 0xf0) != 0)
            {
                throw new InvalidDataException("Invalid 7-bit encoded integer.");
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

            if (shift == 28 && (first & 0xf0) != 0)
            {
                throw new InvalidDataException("Invalid 7-bit encoded integer.");
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
