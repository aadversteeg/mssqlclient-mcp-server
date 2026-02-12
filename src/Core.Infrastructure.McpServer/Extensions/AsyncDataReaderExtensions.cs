using System.Diagnostics;
using System.Text;
using Core.Application.Interfaces;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for IAsyncDataReader to produce formatted tool results
    /// </summary>
    public static class AsyncDataReaderExtensions
    {
        /// <summary>
        /// Converts an IAsyncDataReader to a formatted tool result string with execution timing
        /// </summary>
        /// <param name="reader">The IAsyncDataReader to format</param>
        /// <param name="stopwatch">Stopwatch started before query execution to measure wall-clock time</param>
        /// <returns>A formatted string for tool output</returns>
        public static async Task<string> ToToolResult(this IAsyncDataReader reader, Stopwatch stopwatch)
        {
            StringBuilder result = new StringBuilder();

            // Get column information
            int columnCount = reader.FieldCount;
            List<string> columnNames = reader.GetColumnNames().ToList();
            List<int> columnWidths = columnNames.Select(name => name.Length).ToList();

            // Create a list to store all rows for processing
            List<string[]> rows = new List<string[]>();

            // Process rows to determine optimal column widths
            while (await reader.ReadAsync())
            {
                string[] rowValues = new string[columnCount];

                for (int i = 0; i < columnCount; i++)
                {
                    bool isNull = await reader.IsDBNullAsync(i);
                    string value = isNull ? "NULL" : (await reader.GetFieldValueAsync<object>(i))?.ToString() ?? "";
                    rowValues[i] = value;
                    columnWidths[i] = Math.Max(columnWidths[i], value.Length);
                }

                rows.Add(rowValues);
            }

            // Flush remaining result sets and capture execution plan XML
            string? executionPlanXml = null;
            while (await reader.NextResultAsync())
            {
                if (reader.FieldCount == 1 && await reader.ReadAsync())
                {
                    var value = await reader.GetFieldValueAsync<string>(0);
                    if (value != null && value.TrimStart().StartsWith("<ShowPlanXML"))
                    {
                        executionPlanXml = value;
                    }
                }
            }

            // Stop the stopwatch now that all data has been read
            stopwatch.Stop();

            // Check if no rows were returned
            if (rows.Count == 0)
            {
                return "Query executed successfully. No results returned.\n" + FormatStatsLines(stopwatch, reader.InfoMessages, 0, executionPlanXml);
            }

            // Limit column width to a reasonable size
            for (int i = 0; i < columnWidths.Count; i++)
            {
                columnWidths[i] = Math.Min(columnWidths[i], 40);
            }

            // Build header row
            for (int i = 0; i < columnCount; i++)
            {
                result.Append("| ");
                result.Append(columnNames[i].PadRight(columnWidths[i]));
                result.Append(" ");
            }
            result.AppendLine("|");

            // Build separator row
            for (int i = 0; i < columnCount; i++)
            {
                result.Append("| ");
                result.Append(new string('-', columnWidths[i]));
                result.Append(" ");
            }
            result.AppendLine("|");

            // Build data rows
            foreach (var rowValues in rows)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    string displayValue = rowValues[i];

                    // Truncate value if too long
                    if (displayValue.Length > columnWidths[i])
                    {
                        displayValue = displayValue.Substring(0, columnWidths[i] - 3) + "...";
                    }

                    result.Append("| ");
                    result.Append(displayValue.PadRight(columnWidths[i]));
                    result.Append(" ");
                }
                result.AppendLine("|");
            }

            // Add row count and stats
            result.AppendLine();
            result.AppendLine($"Total rows: {rows.Count}");
            result.AppendLine(FormatStatsLines(stopwatch, reader.InfoMessages, rows.Count, executionPlanXml));

            return result.ToString();
        }

        private static string FormatStatsLines(Stopwatch stopwatch, IReadOnlyList<string> infoMessages, int rowCount, string? executionPlanXml)
        {
            var lines = new List<string>();

            // Rows affected (only for DML operations where no result rows were returned)
            if (rowCount == 0)
            {
                var rowsAffected = RowsAffectedParser.Parse(infoMessages);
                if (rowsAffected != null)
                {
                    lines.Add($"Rows affected: {rowsAffected}");
                }
            }

            // Execution timing
            var elapsed = stopwatch.ElapsedMilliseconds;
            var serverTiming = StatisticsTimeParser.Parse(infoMessages);

            if (serverTiming != null)
            {
                lines.Add($"Execution time: {elapsed}ms (server: {serverTiming.ElapsedMs}ms, CPU: {serverTiming.CpuMs}ms)");
            }
            else
            {
                lines.Add($"Execution time: {elapsed}ms");
            }

            // IO stats
            var ioStats = StatisticsIoParser.Parse(infoMessages);
            if (ioStats != null)
            {
                var tableEntries = ioStats.Select(io =>
                    $"{io.TableName} (logical: {io.LogicalReads}, physical: {io.PhysicalReads}, read-ahead: {io.ReadAheadReads})");
                lines.Add($"IO stats: {string.Join(", ", tableEntries)}");
            }

            // Execution plan
            if (executionPlanXml != null)
            {
                lines.Add("");
                lines.Add("Execution plan:");
                lines.Add(executionPlanXml);
            }

            return string.Join("\n", lines);
        }
    }
}
