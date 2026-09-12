namespace FastTextCompressor.Core;

public interface ICompressor
{
    Task<CompressionResult> CompressAsync(
        Stream input,
        Stream output,
        IProgress<CompressionProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public interface IDecompressor
{
    Task DecompressAsync(Stream input, Stream output, CancellationToken cancellationToken = default);
}

public readonly record struct CompressionProgress(long ProcessedBytes, long? TotalBytes);

public readonly record struct CompressionResult(long OriginalBytes, long CompressedBytes, int DictionaryEntries)
{
    public double CompressionRatio => OriginalBytes == 0 ? 1d : (double)CompressedBytes / OriginalBytes;
}
