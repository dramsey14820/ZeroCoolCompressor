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
    public async Task RejectsInvalidMagic()
    {
        var engine = new CompressionEngine();
        await using var input = new MemoryStream("nope"u8.ToArray());
        await using var output = new MemoryStream();

        await Assert.ThrowsAsync<InvalidDataException>(() => engine.DecompressAsync(input, output));
    }
}
