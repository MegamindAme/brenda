namespace Brenda.Core.Models;

/// <summary>Progress information for a download/installation operation.</summary>
public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes, string Stage)
{
    public double? Percentage => TotalBytes is > 0 ? (double)BytesReceived / TotalBytes.Value * 100.0 : null;
}
