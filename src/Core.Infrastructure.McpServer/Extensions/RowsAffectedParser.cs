using System.Text.RegularExpressions;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Parses SQL Server (N rows affected) messages from InfoMessage strings.
    /// </summary>
    public static class RowsAffectedParser
    {
        private static readonly Regex RowsAffectedRegex = new Regex(
            @"\((\d+) rows? affected\)",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses InfoMessage strings to extract total rows affected.
        /// Sums all occurrences (stored procedures may emit multiple messages).
        /// Returns null if no rows-affected messages are found.
        /// </summary>
        public static long? Parse(IReadOnlyList<string>? infoMessages)
        {
            if (infoMessages == null || infoMessages.Count == 0)
                return null;

            long? total = null;

            foreach (var message in infoMessages)
            {
                var match = RowsAffectedRegex.Match(message);
                if (match.Success)
                {
                    var count = long.Parse(match.Groups[1].Value);
                    total = (total ?? 0) + count;
                }
            }

            return total;
        }
    }
}
