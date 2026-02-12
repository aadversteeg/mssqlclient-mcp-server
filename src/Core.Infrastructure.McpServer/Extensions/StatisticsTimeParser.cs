using System.Text.RegularExpressions;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Parses SQL Server SET STATISTICS TIME ON output from InfoMessage strings.
    /// </summary>
    public static class StatisticsTimeParser
    {
        /// <summary>
        /// Represents parsed SQL Server execution time statistics.
        /// </summary>
        public record SqlServerTimingInfo(long ElapsedMs, long CpuMs);

        private static readonly Regex TimingRegex = new Regex(
            @"CPU time = (\d+) ms,\s+elapsed time = (\d+) ms",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses InfoMessage strings to extract the last SQL Server Execution Times entry.
        /// Returns null if no execution times are found.
        /// </summary>
        public static SqlServerTimingInfo? Parse(IReadOnlyList<string> infoMessages)
        {
            if (infoMessages == null || infoMessages.Count == 0)
                return null;

            SqlServerTimingInfo? lastTiming = null;

            foreach (var message in infoMessages)
            {
                if (message.Contains("SQL Server Execution Times:"))
                {
                    var match = TimingRegex.Match(message);
                    if (match.Success)
                    {
                        var cpuMs = long.Parse(match.Groups[1].Value);
                        var elapsedMs = long.Parse(match.Groups[2].Value);
                        lastTiming = new SqlServerTimingInfo(elapsedMs, cpuMs);
                    }
                }
            }

            return lastTiming;
        }
    }
}
