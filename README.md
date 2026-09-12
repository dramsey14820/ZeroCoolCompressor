# FastTextCompressor

A .NET 10 streaming text compressor with a reusable library and command-line driver.

## Build

```powershell
dotnet build FastTextCompressor.slnx
```

## Use

```powershell
dotnet run --project FastTextCompressor.Cli -- -c input.txt output.ftc
dotnet run --project FastTextCompressor.Cli -- -d output.ftc restored.txt
```

The compressor reads UTF-8 text in bounded buffers. It builds a per-file token dictionary while spooling variable-length token IDs to a temporary file, orders dictionary entries by frequency, and writes a Brotli-compressed dictionary and payload to the `.ftc` output. Length-prefixed UTF-8 dictionary entries keep embedded NUL characters lossless. The library can still read version 1 files created before Brotli compression was added.
