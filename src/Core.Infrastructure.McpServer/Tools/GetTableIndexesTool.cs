using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetTableIndexesTool
    {
        private readonly string? _connectionString;

        public GetTableIndexesTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetTableIndexesTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_table_indexes"), Description("Get all indexes defined on a specific table.")]
        public string GetTableIndexes(string tableName)
        {
            Console.Error.WriteLine($"GetTableIndexes called with tableName: {tableName}");
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
                result.AppendLine($"Indexes for table: {schemaName}.{tableName}");
                result.AppendLine();
                
                // Query to get indexes for the table
                string indexQuery = @"
                    SELECT 
                        i.name AS IndexName,
                        i.type_desc AS IndexType,
                        i.is_unique AS IsUnique,
                        i.is_primary_key AS IsPrimaryKey,
                        i.fill_factor AS FillFactor,
                        p.data_compression_desc AS Compression,
                        ds.name AS DataSpace,
                        i.is_disabled AS IsDisabled,
                        CASE 
                            WHEN i.filter_definition IS NULL THEN 'No' 
                            ELSE 'Yes'
                        END AS IsFiltered,
                        i.filter_definition AS FilterDefinition,
                        (SELECT COUNT(*) FROM sys.index_columns ic 
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id 
                         AND ic.is_included_column = 0) AS KeyColumns,
                        (SELECT COUNT(*) FROM sys.index_columns ic 
                         WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id 
                         AND ic.is_included_column = 1) AS IncludedColumns
                    FROM 
                        sys.indexes i
                    LEFT JOIN 
                        sys.data_spaces ds ON i.data_space_id = ds.data_space_id
                    LEFT JOIN 
                        sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id
                    INNER JOIN 
                        sys.tables t ON i.object_id = t.object_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                        AND i.type > 0  -- Skip heaps
                    ORDER BY 
                        i.name";
                
                using SqlCommand indexCommand = new SqlCommand(indexQuery, connection);
                indexCommand.Parameters.AddWithValue("@TableName", tableName);
                indexCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader indexReader = indexCommand.ExecuteReader();
                
                result.AppendLine("Index Name | Type | Unique | PK | Key Cols | Included Cols | Filtered | Disabled | Compression");
                result.AppendLine("---------- | ---- | ------ | -- | -------- | ------------- | -------- | -------- | -----------");
                
                bool hasIndexes = false;
                while (indexReader.Read())
                {
                    hasIndexes = true;
                    string indexName = indexReader["IndexName"].ToString() ?? "";
                    string indexType = indexReader["IndexType"].ToString() ?? "";
                    string isUnique = Convert.ToBoolean(indexReader["IsUnique"]) ? "Yes" : "No";
                    string isPrimaryKey = Convert.ToBoolean(indexReader["IsPrimaryKey"]) ? "Yes" : "No";
                    string keyColumns = indexReader["KeyColumns"].ToString() ?? "0";
                    string includedColumns = indexReader["IncludedColumns"].ToString() ?? "0";
                    string isFiltered = indexReader["IsFiltered"].ToString() ?? "No";
                    string isDisabled = Convert.ToBoolean(indexReader["IsDisabled"]) ? "Yes" : "No";
                    string compression = indexReader["Compression"].ToString() ?? "NONE";
                    
                    result.AppendLine($"{indexName} | {indexType} | {isUnique} | {isPrimaryKey} | {keyColumns} | {includedColumns} | {isFiltered} | {isDisabled} | {compression}");
                }
                
                if (!hasIndexes)
                {
                    result.AppendLine("No indexes found for this table.");
                }
                
                indexReader.Close();
                
                // Get index columns for each index
                result.AppendLine();
                result.AppendLine("Index Columns Detail:");
                result.AppendLine("Index Name | Column Name | Key Position | Is Included | Sort Direction");
                result.AppendLine("---------- | ----------- | ------------ | ----------- | -------------");
                
                string columnQuery = @"
                    SELECT 
                        i.name AS IndexName,
                        c.name AS ColumnName,
                        ic.key_ordinal AS KeyPosition,
                        ic.is_included_column AS IsIncluded,
                        CASE ic.is_descending_key
                            WHEN 0 THEN 'ASC'
                            WHEN 1 THEN 'DESC'
                            ELSE 'N/A'
                        END AS SortDirection
                    FROM 
                        sys.indexes i
                    INNER JOIN 
                        sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    INNER JOIN 
                        sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                    INNER JOIN 
                        sys.tables t ON i.object_id = t.object_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                        AND i.type > 0  -- Skip heaps
                    ORDER BY 
                        i.name, ic.key_ordinal, ic.is_included_column";
                
                using SqlCommand columnCommand = new SqlCommand(columnQuery, connection);
                columnCommand.Parameters.AddWithValue("@TableName", tableName);
                columnCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader columnReader = columnCommand.ExecuteReader();
                
                bool hasColumns = false;
                while (columnReader.Read())
                {
                    hasColumns = true;
                    string indexName = columnReader["IndexName"].ToString() ?? "";
                    string columnName = columnReader["ColumnName"].ToString() ?? "";
                    int keyPosition = Convert.ToInt32(columnReader["KeyPosition"]);
                    bool isIncluded = Convert.ToBoolean(columnReader["IsIncluded"]);
                    string sortDirection = isIncluded ? "N/A" : columnReader["SortDirection"].ToString() ?? "";
                    
                    result.AppendLine($"{indexName} | {columnName} | {(isIncluded ? "N/A" : keyPosition.ToString())} | {(isIncluded ? "Yes" : "No")} | {sortDirection}");
                }
                
                if (!hasColumns)
                {
                    result.AppendLine("No index columns found.");
                }
                
                // Get index usage statistics
                result.AppendLine();
                result.AppendLine("Index Usage Statistics:");
                result.AppendLine("Index Name | User Seeks | User Scans | User Lookups | User Updates | Last User Seek | Last User Scan | Last User Lookup | Last User Update");
                result.AppendLine("---------- | ---------- | ---------- | ------------ | ------------ | -------------- | -------------- | ---------------- | ----------------");
                
                string usageQuery = @"
                    SELECT 
                        i.name AS IndexName,
                        s.user_seeks AS UserSeeks,
                        s.user_scans AS UserScans,
                        s.user_lookups AS UserLookups,
                        s.user_updates AS UserUpdates,
                        s.last_user_seek AS LastUserSeek,
                        s.last_user_scan AS LastUserScan,
                        s.last_user_lookup AS LastUserLookup,
                        s.last_user_update AS LastUserUpdate
                    FROM 
                        sys.dm_db_index_usage_stats s
                    INNER JOIN 
                        sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
                    INNER JOIN 
                        sys.tables t ON i.object_id = t.object_id
                    INNER JOIN 
                        sys.schemas sch ON t.schema_id = sch.schema_id
                    WHERE 
                        t.name = @TableName
                        AND sch.name = @SchemaName
                        AND s.database_id = DB_ID()
                    ORDER BY 
                        i.name";
                
                using SqlCommand usageCommand = new SqlCommand(usageQuery, connection);
                usageCommand.Parameters.AddWithValue("@TableName", tableName);
                usageCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader usageReader = usageCommand.ExecuteReader();
                
                bool hasUsage = false;
                while (usageReader.Read())
                {
                    hasUsage = true;
                    string indexName = usageReader["IndexName"].ToString() ?? "";
                    string userSeeks = usageReader["UserSeeks"].ToString() ?? "0";
                    string userScans = usageReader["UserScans"].ToString() ?? "0";
                    string userLookups = usageReader["UserLookups"].ToString() ?? "0";
                    string userUpdates = usageReader["UserUpdates"].ToString() ?? "0";
                    
                    string lastSeek = usageReader["LastUserSeek"] == DBNull.Value ? "Never" : 
                        Convert.ToDateTime(usageReader["LastUserSeek"]).ToString("yyyy-MM-dd HH:mm:ss");
                    string lastScan = usageReader["LastUserScan"] == DBNull.Value ? "Never" : 
                        Convert.ToDateTime(usageReader["LastUserScan"]).ToString("yyyy-MM-dd HH:mm:ss");
                    string lastLookup = usageReader["LastUserLookup"] == DBNull.Value ? "Never" : 
                        Convert.ToDateTime(usageReader["LastUserLookup"]).ToString("yyyy-MM-dd HH:mm:ss");
                    string lastUpdate = usageReader["LastUserUpdate"] == DBNull.Value ? "Never" : 
                        Convert.ToDateTime(usageReader["LastUserUpdate"]).ToString("yyyy-MM-dd HH:mm:ss");
                    
                    result.AppendLine($"{indexName} | {userSeeks} | {userScans} | {userLookups} | {userUpdates} | {lastSeek} | {lastScan} | {lastLookup} | {lastUpdate}");
                }
                
                if (!hasUsage)
                {
                    result.AppendLine("No index usage statistics available. This could be because the indexes haven't been used since the server was restarted.");
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