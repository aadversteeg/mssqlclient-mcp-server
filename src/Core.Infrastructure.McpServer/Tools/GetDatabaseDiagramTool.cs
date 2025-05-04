using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetDatabaseDiagramTool
    {
        private readonly string? _connectionString;

        public GetDatabaseDiagramTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetDatabaseDiagramTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_database_diagram"), Description("Generate a text-based entity relationship diagram (ERD) for specified tables.")]
        public string GetDatabaseDiagram(string tables, bool includeColumns = true, bool includeIndexes = false, bool includeDataTypes = true)
        {
            Console.Error.WriteLine($"GetDatabaseDiagram called with tables: {tables}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(tables))
            {
                return "Error: Please provide a comma-separated list of tables to include in the diagram.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Parse the tables parameter
                string[] tableList = tables.Split(',').Select(t => t.Trim()).ToArray();
                if (tableList.Length == 0)
                {
                    return "Error: No valid tables specified.";
                }
                
                StringBuilder mermaidDiagram = new StringBuilder();
                mermaidDiagram.AppendLine("```mermaid");
                mermaidDiagram.AppendLine("erDiagram");
                
                // Dictionary to store table relationships
                Dictionary<string, List<string>> relationships = new Dictionary<string, List<string>>();
                
                // Process each table
                foreach (string tableName in tableList)
                {
                    // Extract schema.table_name if specified
                    string schemaName = "dbo"; // Default schema
                    string tableNameOnly = tableName;
                    
                    if (tableName.Contains('.'))
                    {
                        string[] parts = tableName.Split('.', 2);
                        schemaName = parts[0];
                        tableNameOnly = parts[1];
                    }
                    
                    // Check if the table exists
                    string checkQuery = @"
                        SELECT 
                            COUNT(*) 
                        FROM 
                            sys.tables t
                        INNER JOIN 
                            sys.schemas s ON t.schema_id = s.schema_id
                        WHERE 
                            t.name = @TableName
                            AND s.name = @SchemaName";
                    
                    using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                    checkCommand.Parameters.AddWithValue("@TableName", tableNameOnly);
                    checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    
                    int tableCount = (int)checkCommand.ExecuteScalar();
                    
                    if (tableCount == 0)
                    {
                        return $"Error: Table '{schemaName}.{tableNameOnly}' not found in the database.";
                    }
                    
                    // Get table columns
                    string columnsQuery = @"
                        SELECT 
                            c.name AS ColumnName,
                            t.name AS DataType,
                            c.max_length AS MaxLength,
                            c.precision AS Precision,
                            c.scale AS Scale,
                            c.is_nullable AS IsNullable,
                            CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                            CASE WHEN fk.parent_column_id IS NOT NULL THEN 1 ELSE 0 END AS IsForeignKey
                        FROM 
                            sys.tables tbl
                        INNER JOIN 
                            sys.schemas s ON tbl.schema_id = s.schema_id
                        INNER JOIN 
                            sys.columns c ON tbl.object_id = c.object_id
                        INNER JOIN 
                            sys.types t ON c.system_type_id = t.system_type_id AND c.user_type_id = t.user_type_id
                        LEFT JOIN 
                            sys.index_columns ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                        LEFT JOIN 
                            sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND i.is_primary_key = 1
                        LEFT JOIN 
                            sys.index_columns pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id AND pk.index_id = i.index_id
                        LEFT JOIN 
                            sys.foreign_key_columns fk ON c.object_id = fk.parent_object_id AND c.column_id = fk.parent_column_id
                        WHERE 
                            tbl.name = @TableName
                            AND s.name = @SchemaName
                        ORDER BY 
                            c.column_id";
                    
                    using SqlCommand columnsCommand = new SqlCommand(columnsQuery, connection);
                    columnsCommand.Parameters.AddWithValue("@TableName", tableNameOnly);
                    columnsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    
                    using SqlDataReader columnsReader = columnsCommand.ExecuteReader();
                    
                    // Build table entity for diagram
                    string tableEntity = $"{schemaName}_{tableNameOnly}";
                    mermaidDiagram.Append($"    {tableEntity} {{");
                    
                    if (includeColumns && columnsReader.HasRows)
                    {
                        mermaidDiagram.AppendLine();
                        
                        while (columnsReader.Read())
                        {
                            string columnName = columnsReader["ColumnName"].ToString() ?? "";
                            string dataType = columnsReader["DataType"].ToString() ?? "";
                            bool isPrimaryKey = Convert.ToBoolean(columnsReader["IsPrimaryKey"]);
                            bool isForeignKey = Convert.ToBoolean(columnsReader["IsForeignKey"]);
                            
                            // Format data type with additional info if needed
                            if (includeDataTypes && dataType.ToLower() is "nvarchar" or "varchar" or "char" or "nchar")
                            {
                                int maxLength = Convert.ToInt32(columnsReader["MaxLength"]);
                                if (dataType.StartsWith("n")) maxLength /= 2; // Unicode types
                                if (maxLength == -1) // MAX
                                {
                                    dataType += "(MAX)";
                                }
                                else
                                {
                                    dataType += $"({maxLength})";
                                }
                            }
                            else if (includeDataTypes && dataType.ToLower() is "decimal" or "numeric")
                            {
                                byte precision = Convert.ToByte(columnsReader["Precision"]);
                                byte scale = Convert.ToByte(columnsReader["Scale"]);
                                dataType += $"({precision},{scale})";
                            }
                            
                            // Add icons for primary and foreign keys
                            string keyIndicator = "";
                            if (isPrimaryKey) keyIndicator += "PK ";
                            if (isForeignKey) keyIndicator += "FK ";
                            
                            mermaidDiagram.AppendLine($"        {dataType} {columnName} {keyIndicator}");
                        }
                        
                        mermaidDiagram.Append("    }");
                    }
                    else
                    {
                        mermaidDiagram.Append("}");
                    }
                    
                    mermaidDiagram.AppendLine();
                    
                    columnsReader.Close();
                    
                    // Get indexes if requested
                    if (includeIndexes)
                    {
                        // Implementation for indexes would go here
                        // Currently not implemented to keep complexity manageable
                    }
                    
                    // Get foreign key relationships
                    string relationshipsQuery = @"
                        SELECT 
                            fs.name AS ForeignSchema,
                            ft.name AS ForeignTable,
                            fc.name AS ForeignColumn,
                            ps.name AS PrimarySchema,
                            pt.name AS PrimaryTable,
                            pc.name AS PrimaryColumn,
                            fk.name AS ForeignKeyName,
                            CASE 
                                WHEN fk.delete_referential_action = 1 THEN 'CASCADE'
                                WHEN fk.delete_referential_action = 2 THEN 'SET NULL'
                                WHEN fk.delete_referential_action = 3 THEN 'SET DEFAULT'
                                ELSE 'NO ACTION'
                            END AS DeleteAction
                        FROM 
                            sys.foreign_key_columns fkc
                        INNER JOIN 
                            sys.tables ft ON fkc.parent_object_id = ft.object_id
                        INNER JOIN 
                            sys.schemas fs ON ft.schema_id = fs.schema_id
                        INNER JOIN 
                            sys.tables pt ON fkc.referenced_object_id = pt.object_id
                        INNER JOIN 
                            sys.schemas ps ON pt.schema_id = ps.schema_id
                        INNER JOIN 
                            sys.columns fc ON fkc.parent_object_id = fc.object_id AND fkc.parent_column_id = fc.column_id
                        INNER JOIN 
                            sys.columns pc ON fkc.referenced_object_id = pc.object_id AND fkc.referenced_column_id = pc.column_id
                        INNER JOIN 
                            sys.foreign_keys fk ON fkc.constraint_object_id = fk.object_id
                        WHERE 
                            (fs.name = @SchemaName AND ft.name = @TableName)
                            OR (ps.name = @SchemaName AND pt.name = @TableName)";
                    
                    using SqlCommand relationshipsCommand = new SqlCommand(relationshipsQuery, connection);
                    relationshipsCommand.Parameters.AddWithValue("@TableName", tableNameOnly);
                    relationshipsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    
                    using SqlDataReader relationshipsReader = relationshipsCommand.ExecuteReader();
                    
                    while (relationshipsReader.Read())
                    {
                        string foreignSchema = relationshipsReader["ForeignSchema"].ToString() ?? "";
                        string foreignTable = relationshipsReader["ForeignTable"].ToString() ?? "";
                        string foreignColumn = relationshipsReader["ForeignColumn"].ToString() ?? "";
                        string primarySchema = relationshipsReader["PrimarySchema"].ToString() ?? "";
                        string primaryTable = relationshipsReader["PrimaryTable"].ToString() ?? "";
                        string primaryColumn = relationshipsReader["PrimaryColumn"].ToString() ?? "";
                        string deleteAction = relationshipsReader["DeleteAction"].ToString() ?? "";
                        
                        string primaryEntity = $"{primarySchema}_{primaryTable}";
                        string foreignEntity = $"{foreignSchema}_{foreignTable}";
                        
                        // Only include relationships between tables in our list
                        if (tableList.Any(t => t.Contains('.') 
                                ? t.Trim() == $"{primarySchema}.{primaryTable}" || t.Trim() == $"{foreignSchema}.{foreignTable}"
                                : t.Trim() == primaryTable || t.Trim() == foreignTable))
                        {
                            string relationshipKey = $"{foreignEntity}||--o{{ {primaryEntity} : \"{foreignColumn} -> {primaryColumn}\"";
                            
                            if (!relationships.ContainsKey(foreignEntity))
                            {
                                relationships[foreignEntity] = new List<string>();
                            }
                            
                            if (!relationships[foreignEntity].Contains(relationshipKey))
                            {
                                relationships[foreignEntity].Add(relationshipKey);
                            }
                        }
                    }
                    
                    relationshipsReader.Close();
                }
                
                // Add relationships to the diagram
                foreach (var relationship in relationships)
                {
                    foreach (string rel in relationship.Value)
                    {
                        mermaidDiagram.AppendLine($"    {rel}");
                    }
                }
                
                mermaidDiagram.AppendLine("```");
                
                return mermaidDiagram.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}