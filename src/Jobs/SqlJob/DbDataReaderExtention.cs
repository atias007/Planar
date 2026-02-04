namespace SqlJob;

using System.Data.Common;
using System.Text;

public static class DbDataReaderExtensions
{
#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high

    public static string ToAsciiTable(this DbDataReader reader, out object? scalar)
#pragma warning restore S3776 // Cognitive Complexity of methods should not be too high
    {
        const int maxColumns = 5;
        const int maxRows = 1000;
        const int maxCellLength = 40;

        ArgumentNullException.ThrowIfNull(reader);

        var rows = new List<string[]>();
        var actualColumnCount = Math.Min(reader.FieldCount, maxColumns);
        var columnNames = new string[actualColumnCount];
        var columnWidths = new int[actualColumnCount];

        // Get column names and initialize widths
        for (int i = 0; i < actualColumnCount; i++)
        {
            columnNames[i] = TruncateString(reader.GetName(i), maxCellLength);
            columnWidths[i] = columnNames[i].Length;
        }

        // Read data and calculate column widths (up to maxRows)
        int rowCount = 0;
        scalar = null;
        while (reader.Read() && rowCount < maxRows)
        {
            if (scalar == null && reader.FieldCount > 0)
            {
                scalar = reader.IsDBNull(0) ? null : reader.GetValue(0);
            }

            var row = new string[actualColumnCount];
            for (int i = 0; i < actualColumnCount; i++)
            {
                var value = reader.IsDBNull(i) ? "NULL" : reader.GetValue(i)?.ToString() ?? "NULL";
                row[i] = TruncateString(value, maxCellLength);
                columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
            }
            rows.Add(row);
            rowCount++;
        }

        // Build the ASCII table
        var sb = new StringBuilder();

        // Top border
        sb.AppendLine(CreateBorder(columnWidths));

        // Header row
        sb.AppendLine(CreateRow(columnNames, columnWidths));

        // Header separator
        sb.AppendLine(CreateBorder(columnWidths));

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(CreateRow(row, columnWidths));
        }

        // Bottom border
        sb.AppendLine(CreateBorder(columnWidths));

        // Add note if data was truncated
        if (reader.FieldCount > maxColumns)
        {
            sb.AppendLine($"Note: Showing {actualColumnCount} of {reader.FieldCount} columns");
        }

        if (rowCount >= maxRows && reader.Read())
        {
            sb.AppendLine($"Note: Showing first {maxRows} rows (more data available)");
        }

        return sb.ToString();
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        if (value.Length <= maxLength)
            return value;

        return string.Concat(value.AsSpan(0, maxLength - 3), "…");
    }

    private static string CreateBorder(int[] columnWidths)
    {
        var parts = columnWidths.Select(w => new string('-', w + 2));
        return "+" + string.Join("+", parts) + "+";
    }

    private static string CreateRow(string[] values, int[] columnWidths)
    {
        var paddedValues = values.Select((v, i) => $" {v.PadRight(columnWidths[i])} ");
        return "|" + string.Join("|", paddedValues) + "|";
    }
}