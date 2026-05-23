using System.Security.Cryptography;

namespace MeOrg.Extensions;

public static class FileStreamExtensions
{
    private const int SegmentSize = 400;
    private const int TotalHashBytes = SegmentSize * 3;

    public static string GenerateSampledHash(this FileStream stream)
    {
        long streamOriginalPosition = stream.Position;
        int bytesToHash = stream.Length < TotalHashBytes ?
            (int)stream.Length :
            TotalHashBytes;
        byte[] byteSamples = new byte[bytesToHash];

        try
        {
            stream.Seek(0, SeekOrigin.Begin);

            if (bytesToHash < TotalHashBytes)
            {
                stream.ReadExactly(buffer: byteSamples, offset: 0, bytesToHash);
            }
            else
            {
                stream.ReadExactly(buffer: byteSamples, offset: 0, count: SegmentSize);
                stream.Seek(stream.Length / 2, SeekOrigin.Begin);
                stream.ReadExactly(buffer: byteSamples, offset: SegmentSize, count: SegmentSize);
                stream.Seek(-SegmentSize, SeekOrigin.End);
                stream.ReadExactly(buffer: byteSamples, offset: SegmentSize * 2, count: SegmentSize);
            }
        }
        finally
        {
            stream.Seek(streamOriginalPosition, SeekOrigin.Begin);
        }

        string hash = Convert.ToHexString(MD5.HashData(byteSamples));

        return hash;
    }
}