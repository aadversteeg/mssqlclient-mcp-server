using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Core.Infrastructure.McpServer.Configuration;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Tool to list all databases on a SQL Server instance with their properties
    /// </summary>
    public class ListDatabasesTool
    {
        private readonly DatabaseConfiguration _configuration;

        public ListDatabasesTool(DatabaseConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets a list of all databases on the SQL Server instance with their properties
        /// </summary>
        /// <returns>Markdown formatted string with database information</returns>
        public async Task<string> GetDatabasesAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# SQL Server Databases");
            sb.AppendLine();

            try
            {
                // Verify we're connected to master or have permissions to query server-level information
                bool isMasterContext = await VerifyMasterDatabaseContextAsync();
                if (!isMasterContext)
                {
                    sb.AppendLine("⚠️ **Warning**: Not connected to master database. Database listing may be incomplete.");
                    sb.AppendLine();
                }

                using (var connection = new SqlConnection(_configuration.ConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Query to get database information
                    string query = @"
                    SELECT 
                        d.name as DatabaseName,
                        d.database_id as DatabaseId,
                        d.create_date as CreateDate,
                        d.compatibility_level as CompatibilityLevel,
                        d.collation_name as Collation,
                        d.state_desc as State,
                        d.recovery_model_desc as RecoveryModel,
                        CASE WHEN d.is_read_only = 1 THEN 'Yes' ELSE 'No' END as IsReadOnly,
                        CASE WHEN d.is_auto_close_on = 1 THEN 'Yes' ELSE 'No' END as IsAutoClose,
                        CASE WHEN d.is_auto_shrink_on = 1 THEN 'Yes' ELSE 'No' END as IsAutoShrink,
                        CASE WHEN d.is_published = 1 THEN 'Yes' ELSE 'No' END as IsPublished,
                        CASE WHEN d.is_subscribed = 1 THEN 'Yes' ELSE 'No' END as IsSubscribed,
                        CASE WHEN d.is_encrypted = 1 THEN 'Yes' ELSE 'No' END as IsEncrypted,
                        (SELECT SUM(size * 8 / 1024) FROM sys.master_files WHERE database_id = d.database_id AND type_desc = 'ROWS') as DataSizeMB,
                        (SELECT SUM(size * 8 / 1024) FROM sys.master_files WHERE database_id = d.database_id AND type_desc = 'LOG') as LogSizeMB
                    FROM sys.databases d
                    ORDER BY d.name";
                    
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Create a table header
                        sb.AppendLine("| Database Name | Size (MB) | Recovery Model | State | Created | Compatibility |");
                        sb.AppendLine("|--------------|-----------|----------------|-------|---------|--------------|");
                        
                        // Populate table rows
                        while (await reader.ReadAsync())
                        {
                            string dbName = reader["DatabaseName"].ToString();
                            double? dataSizeMB = reader["DataSizeMB"] != DBNull.Value ? Convert.ToDouble(reader["DataSizeMB"]) : null;
                            double? logSizeMB = reader["LogSizeMB"] != DBNull.Value ? Convert.ToDouble(reader["LogSizeMB"]) : null;
                            double totalSize = (dataSizeMB ?? 0) + (logSizeMB ?? 0);
                            string recoveryModel = reader["RecoveryModel"].ToString();
                            string state = reader["State"].ToString();
                            DateTime createDate = Convert.ToDateTime(reader["CreateDate"]);
                            string compatibility = reader["CompatibilityLevel"].ToString();
                            
                            sb.AppendLine($"| {dbName} | {totalSize:N2} | {recoveryModel} | {state} | {createDate:yyyy-MM-dd} | {compatibility} |");
                        }
                    }

                    // Get database file information
                    sb.AppendLine();
                    sb.AppendLine("## Database Files");
                    sb.AppendLine();
                    
                    query = @"
                    SELECT 
                        d.name as DatabaseName,
                        mf.name as FileName,
                        mf.physical_name as PhysicalName,
                        mf.type_desc as FileType,
                        mf.state_desc as State,
                        mf.size * 8 / 1024 as SizeMB,
                        CASE WHEN mf.is_percent_growth = 1 
                            THEN CAST(mf.growth AS VARCHAR) + '%' 
                            ELSE CAST(mf.growth * 8 / 1024 AS VARCHAR) + ' MB' 
                        END as Growth
                    FROM sys.master_files mf
                    JOIN sys.databases d ON mf.database_id = d.database_id
                    ORDER BY d.name, mf.type_desc";
                    
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        sb.AppendLine("| Database | File Name | File Type | Size (MB) | Growth | Physical Path |");
                        sb.AppendLine("|----------|-----------|-----------|-----------|--------|--------------|");
                        
                        while (await reader.ReadAsync())
                        {
                            string dbName = reader["DatabaseName"].ToString();
                            string fileName = reader["FileName"].ToString();
                            string fileType = reader["FileType"].ToString();
                            double sizeMB = Convert.ToDouble(reader["SizeMB"]);
                            string growth = reader["Growth"].ToString();
                            string physicalPath = reader["PhysicalName"].ToString();
                            
                            sb.AppendLine($"| {dbName} | {fileName} | {fileType} | {sizeMB:N2} | {growth} | {physicalPath} |");
                        }
                    }
                    
                    // Get database status summary
                    sb.AppendLine();
                    sb.AppendLine("## Database Status Summary");
                    sb.AppendLine();
                    
                    query = @"
                    SELECT 
                        SUM(CASE WHEN state_desc = 'ONLINE' THEN 1 ELSE 0 END) as OnlineCount,
                        SUM(CASE WHEN state_desc = 'OFFLINE' THEN 1 ELSE 0 END) as OfflineCount,
                        SUM(CASE WHEN state_desc = 'RESTORING' THEN 1 ELSE 0 END) as RestoringCount,
                        SUM(CASE WHEN state_desc = 'RECOVERING' THEN 1 ELSE 0 END) as RecoveringCount,
                        SUM(CASE WHEN state_desc = 'SUSPECT' THEN 1 ELSE 0 END) as SuspectCount,
                        SUM(CASE WHEN is_read_only = 1 THEN 1 ELSE 0 END) as ReadOnlyCount,
                        SUM(CASE WHEN is_encrypted = 1 THEN 1 ELSE 0 END) as EncryptedCount,
                        COUNT(*) as TotalCount
                    FROM sys.databases";
                    
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int onlineCount = Convert.ToInt32(reader["OnlineCount"]);
                            int offlineCount = Convert.ToInt32(reader["OfflineCount"]);
                            int restoringCount = Convert.ToInt32(reader["RestoringCount"]);
                            int recoveringCount = Convert.ToInt32(reader["RecoveringCount"]);
                            int suspectCount = Convert.ToInt32(reader["SuspectCount"]);
                            int readOnlyCount = Convert.ToInt32(reader["ReadOnlyCount"]);
                            int encryptedCount = Convert.ToInt32(reader["EncryptedCount"]);
                            int totalCount = Convert.ToInt32(reader["TotalCount"]);
                            
                            sb.AppendLine($"- **Total Databases**: {totalCount}");
                            sb.AppendLine($"- **Online**: {onlineCount}");
                            
                            if (offlineCount > 0)
                                sb.AppendLine($"- **Offline**: {offlineCount} ⚠️");
                                
                            if (restoringCount > 0)
                                sb.AppendLine($"- **Restoring**: {restoringCount}");
                                
                            if (recoveringCount > 0)
                                sb.AppendLine($"- **Recovering**: {recoveringCount}");
                                
                            if (suspectCount > 0)
                                sb.AppendLine($"- **Suspect**: {suspectCount} ⚠️");
                                
                            sb.AppendLine($"- **Read-Only**: {readOnlyCount}");
                            sb.AppendLine($"- **Encrypted**: {encryptedCount}");
                        }
                    }
                    
                    // Get system databases vs user databases
                    sb.AppendLine();
                    sb.AppendLine("## Database Types");
                    sb.AppendLine();
                    
                    query = @"
                    SELECT 
                        SUM(CASE WHEN database_id <= 4 THEN 1 ELSE 0 END) as SystemDatabases,
                        SUM(CASE WHEN database_id > 4 THEN 1 ELSE 0 END) as UserDatabases
                    FROM sys.databases";
                    
                    using (var command = new SqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int systemDbCount = Convert.ToInt32(reader["SystemDatabases"]);
                            int userDbCount = Convert.ToInt32(reader["UserDatabases"]);
                            
                            sb.AppendLine($"- **System Databases**: {systemDbCount}");
                            sb.AppendLine($"- **User Databases**: {userDbCount}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("## Error Getting Database Information");
                sb.AppendLine();
                sb.AppendLine($"```");
                sb.AppendLine($"{ex.Message}");
                sb.AppendLine();
                sb.AppendLine($"{ex.StackTrace}");
                sb.AppendLine($"```");
                
                sb.AppendLine();
                sb.AppendLine("### Possible Causes");
                sb.AppendLine();
                sb.AppendLine("- Not connected to master database");
                sb.AppendLine("- Insufficient permissions to view server-level information");
                sb.AppendLine("- Connection issues with the SQL Server");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Verifies the connection is to the master database
        /// </summary>
        private async Task<bool> VerifyMasterDatabaseContextAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.ConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                    {
                        string dbName = (string)await command.ExecuteScalarAsync();
                        return string.Equals(dbName, "master", StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
