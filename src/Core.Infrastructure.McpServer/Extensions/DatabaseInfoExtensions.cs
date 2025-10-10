using System.Text;
using Core.Application.Models;

namespace Core.Infrastructure.McpServer.Extensions
{
    /// <summary>
    /// Extension methods for SQL database operations to produce formatted tool results
    /// </summary>
    public static class DatabaseInfoExtensions
    {
        /// <summary>
        /// Converts a collection of DatabaseInfo objects to a formatted tool result string
        /// </summary>
        /// <param name="databases">The collection of DatabaseInfo objects</param>
        /// <returns>A formatted string for tool output</returns>
        public static string ToToolResult(this IEnumerable<DatabaseInfo> databases)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# SQL Server Databases");
            sb.AppendLine();

            // Create a table header for databases
            sb.AppendLine("| Database Name | Size (MB) | Recovery Model | State | Created | Owner |");
            sb.AppendLine("|--------------|-----------|----------------|-------|---------|-------|");
            
            // Group databases into system and user databases
            var systemDatabases = databases.Where(db => db.Name.Equals("master", StringComparison.OrdinalIgnoreCase) ||
                                                      db.Name.Equals("model", StringComparison.OrdinalIgnoreCase) ||
                                                      db.Name.Equals("msdb", StringComparison.OrdinalIgnoreCase) ||
                                                      db.Name.Equals("tempdb", StringComparison.OrdinalIgnoreCase)).ToList();
            var userDatabases = databases.Except(systemDatabases).ToList();
            
            // Add system databases first
            foreach (var db in systemDatabases)
            {
                sb.AppendLine($"| **{db.Name}** | {db.SizeMB:N2} | {db.RecoveryModel} | {db.State} | {db.CreateDate:yyyy-MM-dd} | {db.Owner} |");
            }
            
            // Add user databases
            foreach (var db in userDatabases)
            {
                sb.AppendLine($"| {db.Name} | {db.SizeMB:N2} | {db.RecoveryModel} | {db.State} | {db.CreateDate:yyyy-MM-dd} | {db.Owner} |");
            }
            
            // Database statistics
            sb.AppendLine();
            sb.AppendLine("## Database Summary");
            sb.AppendLine();
            
            // Calculate statistics
            int totalDatabases = databases.Count();
            int onlineDatabases = databases.Count(db => db.State.Equals("ONLINE", StringComparison.OrdinalIgnoreCase));
            int offlineDatabases = databases.Count(db => db.State.Equals("OFFLINE", StringComparison.OrdinalIgnoreCase));
            int readOnlyDatabases = databases.Count(db => db.IsReadOnly);
            
            sb.AppendLine($"- **Total Databases**: {totalDatabases}");
            sb.AppendLine($"- **System Databases**: {systemDatabases.Count}");
            sb.AppendLine($"- **User Databases**: {userDatabases.Count}");
            sb.AppendLine($"- **Online**: {onlineDatabases}");
            
            if (offlineDatabases > 0)
                sb.AppendLine($"- **Offline**: {offlineDatabases} ⚠️");
            
            sb.AppendLine($"- **Read-Only**: {readOnlyDatabases}");
            
            // Additional information about usage
            sb.AppendLine();
            sb.AppendLine("## Usage Information");
            sb.AppendLine();
            sb.AppendLine("To see tables in a specific database, use:");
            sb.AppendLine("```");
            sb.AppendLine("list_tables_in_database <database_name>");
            sb.AppendLine("```");
            
            return sb.ToString();
        }
    }
}