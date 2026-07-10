using System.IO.Compression;
using System.Text;
using Brenda.Core.Abstractions;
using ZstdSharp;

namespace Brenda.Infrastructure.Blender;

/// <summary>
/// Reads the 12-byte .blend header ("BLENDER" + pointer-size + endianness + 3-digit version)
/// to determine which Blender series saved the file. Handles plain, gzip- and zstd-compressed files.
/// </summary>
public sealed class BlendFileInspector : IBlendFileInspector
{
    private const int HeaderLength = 12;

    public async Task<string?> TryGetSeriesAsync(string blendFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(blendFilePath))
        {
            return null;
        }

        try
        {
            await using var file = new FileStream(blendFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var magic = new byte[4];
            var read = await file.ReadAtLeastAsync(magic, 4, throwOnEndOfStream: false, ct);
            if (read < 4)
            {
                return null;
            }

            file.Position = 0;

            Stream source = magic switch
            {
                [0x1F, 0x8B, ..] => new GZipStream(file, CompressionMode.Decompress),
                [0x28, 0xB5, 0x2F, 0xFD] => new DecompressionStream(file),
                _ => file
            };

            var header = new byte[HeaderLength];
            read = await source.ReadAtLeastAsync(header, HeaderLength, throwOnEndOfStream: false, ct);
            if (source != file)
            {
                await source.DisposeAsync();
            }

            if (read < HeaderLength)
            {
                return null;
            }

            return ParseHeader(header);
        }
        catch (IOException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (ZstdException)
        {
            return null;
        }
    }

    internal static string? ParseHeader(ReadOnlySpan<byte> header)
    {
        if (header.Length < HeaderLength || !header[..7].SequenceEqual("BLENDER"u8))
        {
            return null;
        }

        var versionText = Encoding.ASCII.GetString(header[9..12]);
        if (!int.TryParse(versionText, out var packed) || packed <= 0)
        {
            return null;
        }

        var major = packed / 100;
        var minor = packed % 100;
        return $"{major}.{minor}";
    }
}
