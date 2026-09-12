using FastTextCompressor.Core;
using System.Diagnostics;
using System.Text;

if (args.Length != 3 || (args[0] is not ("-c" or "--compress" or "-d" or "--decompress")))
{
	Console.Error.WriteLine("Usage: FastTextCompressor.Cli -c|--compress <input.txt> <output.ftc>");
	Console.Error.WriteLine("       FastTextCompressor.Cli -d|--decompress <input.ftc> <output.txt>");
	return 2;
}

var mode = args[0] is "-c" or "--compress" ? "compress" : "decompress";
var inputPath = Path.GetFullPath(args[1]);
var outputPath = Path.GetFullPath(args[2]);
if (!File.Exists(inputPath))
{
	Console.Error.WriteLine($"Input file was not found: {inputPath}");
	return 1;
}

if (string.Equals(inputPath, outputPath, StringComparison.OrdinalIgnoreCase))
{
	Console.Error.WriteLine("Input and output paths must be different.");
	return 1;
}

var stopwatch = Stopwatch.StartNew();
var engine = new CompressionEngine();
try
{
	await using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
	await using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.SequentialScan);

	if (mode == "compress")
	{
		var progress = new Progress<CompressionProgress>(value =>
		{
			if (value.TotalBytes is > 0)
			{
				var percent = value.ProcessedBytes * 100d / value.TotalBytes.Value;
				Console.Write($"\rCompressing: {percent,6:0.0}%");
			}
		});
		var result = await engine.CompressAsync(input, output, progress);
		Console.WriteLine();
		stopwatch.Stop();
		Console.WriteLine($"Compressed {result.OriginalBytes:N0} bytes to {result.CompressedBytes:N0} bytes.");
		Console.WriteLine($"Dictionary entries: {result.DictionaryEntries:N0}; ratio: {result.CompressionRatio:P1}; elapsed: {stopwatch.Elapsed}.");
	}
	else
	{
		await engine.DecompressAsync(input, output);
		stopwatch.Stop();
		Console.WriteLine($"Decompressed {new FileInfo(inputPath).Length:N0} bytes in {stopwatch.Elapsed}.");
	}

	return 0;
}
catch (Exception exception) when (exception is IOException or InvalidDataException or DecoderFallbackException)
{
	Console.Error.WriteLine($"Operation failed: {exception.Message}");
	return 1;
}
