using System.Buffers;
using System.Text;

namespace FastTextCompressor.Core;

public sealed class CompressionEngine : ICompressor, IDecompressor
{
    private static readonly byte[] Magic = "FTCX"u8.ToArray();
    private const byte Version = 1;
    private const int BufferSize = 64 * 1024;

    public async Task<CompressionResult> CompressAsync(
        Stream input,
        Stream output,
        IProgress<CompressionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        if (!input.CanRead || !output.CanWrite)
        {
            throw new ArgumentException("Input must be readable and output must be writable.");
        }

        var dictionary = new DictionaryManager();
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"ftc-{Guid.NewGuid():N}.payload");
        long processedBytes = 0;
        try
        {
            await using (var payload = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan))
            using (var reader = new StreamReader(input, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true, BufferSize, leaveOpen: true))
            {
                var buffer = ArrayPool<char>.Shared.Rent(BufferSize);
                try
                {
                    var tokenizer = new ZeroAllocTokenizer();
                    int read;
                    while ((read = await reader.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken)) > 0)
                    {
                        tokenizer.Process(buffer.AsSpan(0, read), isFinal: false, token =>
                        {
                            BinaryPayload.Write7BitEncodedInt(payload, dictionary.GetOrAdd(token));
                        });
                        processedBytes = input.CanSeek ? input.Position : processedBytes + Encoding.UTF8.GetByteCount(buffer, 0, read);
                        progress?.Report(new CompressionProgress(processedBytes, input.CanSeek ? input.Length : null));
                    }

                    tokenizer.Process(ReadOnlySpan<char>.Empty, isFinal: true, token =>
                    {
                        BinaryPayload.Write7BitEncodedInt(payload, dictionary.GetOrAdd(token));
                    });
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(buffer);
                }
            }

            await using (var payload = new FileStream(temporaryPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                output.Write(Magic);
                output.WriteByte(Version);
                BinaryPayload.Write7BitEncodedInt(output, dictionary.Count);
                foreach (var token in dictionary.Tokens)
                {
                    var bytes = Encoding.UTF8.GetBytes(token);
                    BinaryPayload.Write7BitEncodedInt(output, bytes.Length);
                    output.Write(bytes);
                }

                await payload.CopyToAsync(output, BufferSize, cancellationToken);
            }

            if (output.CanSeek)
            {
                return new CompressionResult(processedBytes, output.Position, dictionary.Count);
            }

            return new CompressionResult(processedBytes, 0, dictionary.Count);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
        }
    }

    public async Task DecompressAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        var magic = new byte[Magic.Length];
        await ReadExactlyAsync(input, magic, cancellationToken);
        if (!magic.AsSpan().SequenceEqual(Magic) || input.ReadByte() != Version)
        {
            throw new InvalidDataException("The input is not a supported FTCX file.");
        }

        var count = BinaryPayload.Read7BitEncodedInt(input);
        var dictionary = new string[count];
        for (var index = 0; index < count; index++)
        {
            var byteCount = BinaryPayload.Read7BitEncodedInt(input);
            var bytes = new byte[byteCount];
            await ReadExactlyAsync(input, bytes, cancellationToken);
            dictionary[index] = Encoding.UTF8.GetString(bytes);
        }

        await using var writer = new StreamWriter(output, new UTF8Encoding(false), BufferSize, leaveOpen: true);
        while (BinaryPayload.TryRead7BitEncodedInt(input, out var id))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if ((uint)id >= (uint)dictionary.Length)
            {
                throw new InvalidDataException("Payload references an invalid dictionary entry.");
            }

            await writer.WriteAsync(dictionary[id].AsMemory(), cancellationToken);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(offset), cancellationToken);
            if (read == 0)
            {
                throw new InvalidDataException("Unexpected end of compressed data.");
            }

            offset += read;
        }
    }
}
