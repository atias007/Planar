using System;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Planar.Service.General;

internal static class DataExtensions
{
    public static async Task<Stream> GetStreamFromText(this DbDataReader reader, int ordinal)
    {
        var memoryStream = new MemoryStream();

        if (await reader.IsDBNullAsync(0)) { return memoryStream; }

        var bufferSize = 8 * 1024; // 8KB
        using var textReader = reader.GetTextReader(0);
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, bufferSize, leaveOpen: true);
        var buffer = new char[bufferSize];
        int charsRead;
        while ((charsRead = await textReader.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await writer.WriteAsync(buffer, 0, charsRead);
        }

        writer.Flush(); // Ensure all buffered data is written to the underlying stream.
        memoryStream.Position = 0; // Reset position for subsequent reads from the MemoryStream.
        GC.Collect();
        return memoryStream;
    }

    public static async Task<Stream> GetStreamFromText(this DbDataReader reader, string name)
    {
        int i = reader.GetOrdinal(name);
        return await reader.GetStreamFromText(i);
    }
}