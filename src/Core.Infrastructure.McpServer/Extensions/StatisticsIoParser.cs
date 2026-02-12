using System.Text.RegularExpressions;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Parses SQL Server SET STATISTICS IO ON output from InfoMessage strings.
    /// </summary>
    public static class StatisticsIoParser
    {
        /// <summary>
        /// Represents parsed per-table IO statistics from SQL Server.
        /// </summary>
        public record TableIoInfo(string TableName, long LogicalReads, long PhysicalReads, long ReadAheadReads);

        private static readonly Regex IoRegex = new Regex(
            @"Table '(\w+)'\. Scan count \d+, logical reads (\d+), physical reads (\d+),.*?read-ahead reads (\d+)",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses InfoMessage strings to extract per-table IO statistics.
        /// Filters out worktable entries and aggregates duplicate table names.
        /// Returns null if no IO statistics are found.
        /// </summary>
        public static IReadOnlyList<TableIoInfo>? Parse(IReadOnlyList<string>? infoMessages)
        {
            if (infoMessages == null || infoMessages.Count == 0)
                return null;

            var tableStats = new Dictionary<string, (long LogicalReads, long PhysicalReads, long ReadAheadReads)>();

            foreach (var message in infoMessages)
            {
                var match = IoRegex.Match(message);
                if (match.Success)
                {
                    var tableName = match.Groups[1].Value;

                    // Filter out internal worktable entries
                    if (tableName.StartsWith("Worktable", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var logicalReads = long.Parse(match.Groups[2].Value);
                    var physicalReads = long.Parse(match.Groups[3].Value);
                    var readAheadReads = long.Parse(match.Groups[4].Value);

                    if (tableStats.TryGetValue(tableName, out var existing))
                    {
                        tableStats[tableName] = (
                            existing.LogicalReads + logicalReads,
                            existing.PhysicalReads + physicalReads,
                            existing.ReadAheadReads + readAheadReads);
                    }
                    else
                    {
                        tableStats[tableName] = (logicalReads, physicalReads, readAheadReads);
                    }
                }
            }

            if (tableStats.Count == 0)
                return null;

            return tableStats
                .Select(kvp => new TableIoInfo(kvp.Key, kvp.Value.LogicalReads, kvp.Value.PhysicalReads, kvp.Value.ReadAheadReads))
                .ToList();
        }
    }
}
