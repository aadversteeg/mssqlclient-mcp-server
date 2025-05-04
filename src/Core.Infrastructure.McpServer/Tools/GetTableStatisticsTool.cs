using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetTableStatisticsTool
    {
        private readonly string? _connectionString;

        public GetTableStatisticsTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetTableStatisticsTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_table_statistics"), Description("Get statistics about a specific table such as row count, data size, index size, etc.")]
        public string GetTableStatistics(string tableName)
        {
            Console.Error.WriteLine($"GetTableStatistics called with tableName: {tableName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get table schema and name properly
                string schemaQuery = @"
                    SELECT
                        s.name AS SchemaName,
                        t.name AS TableName
                    FROM
                        sys.tables t
                    INNER JOIN
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE
                        t.name = @TableName";

                using SqlCommand schemaCommand = new SqlCommand(schemaQuery, connection);
                schemaCommand.Parameters.AddWithValue("@TableName", tableName);
                
                string schemaName = "dbo"; // Default schema
                using (SqlDataReader schemaReader = schemaCommand.ExecuteReader())
                {
                    if (schemaReader.Read())
                    {
                        schemaName = schemaReader["SchemaName"].ToString() ?? "dbo";
                    }
                    else
                    {
                        return $"Error: Table '{tableName}' not found in the database.";
                    }
                }
                
                StringBuilder result = new StringBuilder();
                result.AppendLine($"Statistics for table: {schemaName}.{tableName}");
                result.AppendLine();
                
                // Query to get table statistics
                string statsQuery = @"
                    SELECT
                        t.name AS TableName,
                        s.name AS SchemaName,
                        p.rows AS RowCount,
                        CONVERT(VARCHAR(20), t.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), t.modify_date, 120) AS ModifiedDate,
                        (SELECT COUNT(*) FROM sys.columns WHERE object_id = t.object_id) AS ColumnCount,
                        (SELECT COUNT(*) FROM sys.indexes WHERE object_id = t.object_id) AS IndexCount,
                        (SELECT COUNT(*) FROM sys.triggers WHERE parent_id = t.object_id) AS TriggerCount,
                        (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = t.object_id) AS OutboundFKCount,
                        (SELECT COUNT(*) FROM sys.foreign_keys WHERE referenced_object_id = t.object_id) AS InboundFKCount
                    FROM 
                        sys.tables t
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    INNER JOIN 
                        sys.partitions p ON t.object_id = p.object_id
                    LEFT JOIN 
                        sys.dm_db_partition_stats ps ON t.object_id = ps.object_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                        AND p.index_id IN (0, 1)
                    GROUP BY 
                        t.name, s.name, p.rows, t.create_date, t.modify_date, t.object_id";
                
                using SqlCommand statsCommand = new SqlCommand(statsQuery, connection);
                statsCommand.Parameters.AddWithValue("@TableName", tableName);
                statsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader statsReader = statsCommand.ExecuteReader();
                
                if (statsReader.Read())
                {
                    string rowCount = statsReader["RowCount"].ToString() ?? "0";
                    string createdDate = statsReader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = statsReader["ModifiedDate"].ToString() ?? "";
                    string columnCount = statsReader["ColumnCount"].ToString() ?? "0";
                    string indexCount = statsReader["IndexCount"].ToString() ?? "0";
                    string triggerCount = statsReader["TriggerCount"].ToString() ?? "0";
                    string outboundFKCount = statsReader["OutboundFKCount"].ToString() ?? "0";
                    string inboundFKCount = statsReader["InboundFKCount"].ToString() ?? "0";
                    
                    result.AppendLine("Basic Information:");
                    result.AppendLine($"Row Count: {rowCount:N0}");
                    result.AppendLine($"Column Count: {columnCount}");
                    result.AppendLine($"Index Count: {indexCount}");
                    result.AppendLine($"Trigger Count: {triggerCount}");
                    result.AppendLine($"Outbound Foreign Keys: {outboundFKCount}");
                    result.AppendLine($"Inbound Foreign Keys: {inboundFKCount}");
                    result.AppendLine($"Created Date: {createdDate}");
                    result.AppendLine($"Last Modified Date: {modifiedDate}");
                }
                
                statsReader.Close();
                
                // Query to get table and index size information
                string sizeQuery = @"
                    SELECT
                        t.NAME AS TableName,
                        s.Name AS SchemaName,
                        p.rows AS RowCount,
                        SUM(a.total_pages) * 8 AS TotalSpaceKB, 
                        SUM(a.used_pages) * 8 AS UsedSpaceKB, 
                        (SUM(a.total_pages) - SUM(a.used_pages)) * 8 AS UnusedSpaceKB,
                        (SUM(CASE WHEN i.index_id IN (0, 1) THEN a.used_pages ELSE 0 END)) * 8 AS DataSizeKB,
                        (SUM(CASE WHEN i.index_id NOT IN (0, 1) THEN a.used_pages ELSE 0 END)) * 8 AS IndexSizeKB
                    FROM 
                        sys.tables t
                    INNER JOIN      
                        sys.indexes i ON t.OBJECT_ID = i.object_id
                    INNER JOIN 
                        sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                    INNER JOIN 
                        sys.allocation_units a ON p.partition_id = a.container_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.NAME = @TableName
                        AND s.Name = @SchemaName
                        AND t.is_ms_shipped = 0
                    GROUP BY 
                        t.Name, s.Name, p.Rows";
                
                using SqlCommand sizeCommand = new SqlCommand(sizeQuery, connection);
                sizeCommand.Parameters.AddWithValue("@TableName", tableName);
                sizeCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader sizeReader = sizeCommand.ExecuteReader();
                
                if (sizeReader.Read())
                {
                    double totalSpaceMB = Convert.ToDouble(sizeReader["TotalSpaceKB"]) / 1024.0;
                    double usedSpaceMB = Convert.ToDouble(sizeReader["UsedSpaceKB"]) / 1024.0;
                    double unusedSpaceMB = Convert.ToDouble(sizeReader["UnusedSpaceKB"]) / 1024.0;
                    double dataSizeMB = Convert.ToDouble(sizeReader["DataSizeKB"]) / 1024.0;
                    double indexSizeMB = Convert.ToDouble(sizeReader["IndexSizeKB"]) / 1024.0;
                    
                    result.AppendLine();
                    result.AppendLine("Size Information:");
                    result.AppendLine($"Total Space: {totalSpaceMB:N2} MB");
                    result.AppendLine($"Used Space: {usedSpaceMB:N2} MB");
                    result.AppendLine($"Unused Space: {unusedSpaceMB:N2} MB");
                    result.AppendLine($"Data Size: {dataSizeMB:N2} MB");
                    result.AppendLine($"Index Size: {indexSizeMB:N2} MB");
                }
                
                sizeReader.Close();
                
                // Query to get fragmentation information for indexes
                string fragQuery = @"
                    SELECT
                        i.name AS IndexName,
                        stats.avg_fragmentation_in_percent AS FragmentationPercent,
                        stats.page_count AS PageCount,
                        stats.avg_page_space_used_in_percent AS PageDensityPercent
                    FROM
                        sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID(@TableWithSchema), NULL, NULL, 'SAMPLED') stats
                    INNER JOIN
                        sys.indexes i ON stats.object_id = i.object_id AND stats.index_id = i.index_id
                    WHERE
                        stats.index_id > 0 -- Skip heaps
                    ORDER BY
                        stats.avg_fragmentation_in_percent DESC";
                
                using SqlCommand fragCommand = new SqlCommand(fragQuery, connection);
                fragCommand.Parameters.AddWithValue("@TableWithSchema", $"{schemaName}.{tableName}");
                
                using SqlDataReader fragReader = fragCommand.ExecuteReader();
                
                result.AppendLine();
                result.AppendLine("Index Fragmentation:");
                result.AppendLine("Index Name | Fragmentation % | Page Count | Page Density %");
                result.AppendLine("---------- | --------------- | ---------- | -------------");
                
                bool hasFragInfo = false;
                while (fragReader.Read())
                {
                    hasFragInfo = true;
                    string indexName = fragReader["IndexName"].ToString() ?? "";
                    double fragPercent = Convert.ToDouble(fragReader["FragmentationPercent"]);
                    long pageCount = Convert.ToInt64(fragReader["PageCount"]);
                    double densityPercent = fragReader["PageDensityPercent"] == DBNull.Value ? 0 : Convert.ToDouble(fragReader["PageDensityPercent"]);
                    
                    result.AppendLine($"{indexName} | {fragPercent:N2}% | {pageCount:N0} | {densityPercent:N2}%");
                }
                
                if (!hasFragInfo)
                {
                    result.AppendLine("No fragmentation information available.");
                }
                
                fragReader.Close();
                
                // Query to get table usage statistics
                string usageQuery = @"
                    SELECT
                        last_user_seek,
                        last_user_scan,
                        last_user_lookup,
                        last_user_update,
                        user_seeks,
                        user_scans,
                        user_lookups,
                        user_updates
                    FROM
                        sys.dm_db_index_usage_stats
                    WHERE
                        database_id = DB_ID()
                        AND object_id = OBJECT_ID(@TableWithSchema)
                        AND index_id = 0 OR index_id = 1 -- Heap or clustered index (the actual table)";
                
                using SqlCommand usageCommand = new SqlCommand(usageQuery, connection);
                usageCommand.Parameters.AddWithValue("@TableWithSchema", $"{schemaName}.{tableName}");
                
                using SqlDataReader usageReader = usageCommand.ExecuteReader();
                
                result.AppendLine();
                result.AppendLine("Table Usage Statistics:");
                
                if (usageReader.Read())
                {
                    DateTime? lastSeek = usageReader["last_user_seek"] == DBNull.Value ? null : 
                        Convert.ToDateTime(usageReader["last_user_seek"]);
                    DateTime? lastScan = usageReader["last_user_scan"] == DBNull.Value ? null : 
                        Convert.ToDateTime(usageReader["last_user_scan"]);
                    DateTime? lastLookup = usageReader["last_user_lookup"] == DBNull.Value ? null : 
                        Convert.ToDateTime(usageReader["last_user_lookup"]);
                    DateTime? lastUpdate = usageReader["last_user_update"] == DBNull.Value ? null : 
                        Convert.ToDateTime(usageReader["last_user_update"]);
                    
                    int userSeeks = Convert.ToInt32(usageReader["user_seeks"]);
                    int userScans = Convert.ToInt32(usageReader["user_scans"]);
                    int userLookups = Convert.ToInt32(usageReader["user_lookups"]);
                    int userUpdates = Convert.ToInt32(usageReader["user_updates"]);
                    
                    result.AppendLine($"Seeks (direct index access): {userSeeks:N0}");
                    result.AppendLine($"Scans (full table scans): {userScans:N0}");
                    result.AppendLine($"Lookups (row lookups): {userLookups:N0}");
                    result.AppendLine($"Updates (inserts, updates, deletes): {userUpdates:N0}");
                    result.AppendLine();
                    result.AppendLine($"Last Seek: {(lastSeek.HasValue ? lastSeek.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}");
                    result.AppendLine($"Last Scan: {(lastScan.HasValue ? lastScan.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}");
                    result.AppendLine($"Last Lookup: {(lastLookup.HasValue ? lastLookup.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}");
                    result.AppendLine($"Last Update: {(lastUpdate.HasValue ? lastUpdate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}");
                }
                else
                {
                    result.AppendLine("No usage statistics available. This could be because the table hasn't been accessed since the server was restarted.");
                }
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}