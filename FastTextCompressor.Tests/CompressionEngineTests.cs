using FastTextCompressor.Core;
using System.Text;

namespace FastTextCompressor.Tests;

public sealed class CompressionEngineTests
{
    [Fact]
    public void TokenizerPreservesBoundariesAndGroupsNonWords()
    {
        var tokenizer = new ZeroAllocTokenizer();
        var tokens = new List<string>();
        tokenizer.Process("Hello,  world!\n42".AsSpan(), isFinal: true, token => tokens.Add(token.ToString()));

        Assert.Equal(["Hello", ",  ", "world", "!\n", "42"], tokens);
    }

    [Fact]
    public async Task CompressionRoundTripsUnicodeAndEmbeddedNull()
    {
        const string original = "Hello,  world!\r\nUnicode: café — こんにちは\0\nHello, world!";
        var engine = new CompressionEngine();
        await using var compressed = new MemoryStream();
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes(original));

        var result = await engine.CompressAsync(input, compressed);

        compressed.Position = 0;
        await using var restored = new MemoryStream();
        await engine.DecompressAsync(compressed, restored);

        Assert.Equal(original, Encoding.UTF8.GetString(restored.ToArray()));
        Assert.True(result.DictionaryEntries > 0);
        Assert.True(result.CompressionRatio > 0d);
    }

    [Fact]
    public async Task CompressionUsesBrotliFormatVersionTwo()
    {
        var engine = new CompressionEngine();
        await using var compressed = new MemoryStream();
        await using var input = new MemoryStream(Encoding.UTF8.GetBytes("repeat repeat repeat"));

        await engine.CompressAsync(input, compressed);

        Assert.Equal("FTCX"u8.ToArray(), compressed.ToArray()[..4]);
        Assert.Equal(2, compressed.ToArray()[4]);
    }

    [Fact]
    public async Task DecompressesLegacyVersionOnePayload()
    {
        await using var input = new MemoryStream();
        input.Write("FTCX"u8);
        input.WriteByte(1);
        Write7BitEncodedInt(input, 2);
        WriteToken(input, "Hello");
        WriteToken(input, " world");
        Write7BitEncodedInt(input, 0);
        Write7BitEncodedInt(input, 1);
        input.Position = 0;

        var engine = new CompressionEngine();
        await using var output = new MemoryStream();
        await engine.DecompressAsync(input, output);

        Assert.Equal("Hello world", Encoding.UTF8.GetString(output.ToArray()));
    }

    [Fact]
    public async Task RejectsInvalidMagic()
    {
        var engine = new CompressionEngine();
        await using var input = new MemoryStream("nope"u8.ToArray());
        await using var output = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(() => engine.DecompressAsync(input, output));
    }

    private static void WriteToken(Stream stream, string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        Write7BitEncodedInt(stream, bytes.Length);
        stream.Write(bytes);
    }

    private static void Write7BitEncodedInt(Stream stream, int value)
    {
        var remaining = (uint)value;
        while (remaining >= 0x80)
        {
            stream.WriteByte((byte)(remaining | 0x80));
            remaining >>= 7;
        }

        stream.WriteByte((byte)remaining);
    }
}
