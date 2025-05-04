using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ListRelationshipsTool
    {
        private readonly string? _connectionString;

        public ListRelationshipsTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListRelationshipsTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_relationships"), Description("List all foreign key relationships between tables in the database.")]
        public string ListRelationships()
        {
            Console.Error.WriteLine($"ListRelationships called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all relationships
                string query = @"
                    SELECT 
                        fk.name AS ConstraintName,
                        SCHEMA_NAME(pt.schema_id) AS ParentSchema,
                        pt.name AS ParentTable,
                        pc.name AS ParentColumn,
                        SCHEMA_NAME(rt.schema_id) AS ReferencedSchema,
                        rt.name AS ReferencedTable,
                        rc.name AS ReferencedColumn,
                        CASE fk.delete_referential_action
                            WHEN 0 THEN 'NO_ACTION'
                            WHEN 1 THEN 'CASCADE'
                            WHEN 2 THEN 'SET_NULL'
                            WHEN 3 THEN 'SET_DEFAULT'
                            ELSE 'UNKNOWN'
                        END AS OnDelete,
                        CASE fk.update_referential_action
                            WHEN 0 THEN 'NO_ACTION'
                            WHEN 1 THEN 'CASCADE'
                            WHEN 2 THEN 'SET_NULL'
                            WHEN 3 THEN 'SET_DEFAULT'
                            ELSE 'UNKNOWN'
                        END AS OnUpdate,
                        fk.is_disabled AS IsDisabled
                    FROM 
                        sys.foreign_keys fk
                    INNER JOIN 
                        sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN 
                        sys.tables pt ON fk.parent_object_id = pt.object_id
                    INNER JOIN 
                        sys.columns pc ON fkc.parent_column_id = pc.column_id AND fkc.parent_object_id = pc.object_id
                    INNER JOIN 
                        sys.tables rt ON fk.referenced_object_id = rt.object_id
                    INNER JOIN 
                        sys.columns rc ON fkc.referenced_column_id = rc.column_id AND fkc.referenced_object_id = rc.object_id
                    ORDER BY 
                        ParentSchema, ParentTable, ReferencedSchema, ReferencedTable";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder relationships = new StringBuilder();
                relationships.AppendLine("Database Relationships:");
                relationships.AppendLine();
                relationships.AppendLine("Constraint Name | Child Table | Child Column | Parent Table | Parent Column | On Delete | On Update | Disabled");
                relationships.AppendLine("--------------- | ----------- | ------------ | ------------ | ------------- | --------- | --------- | --------");
                
                bool hasRelationships = false;
                while (reader.Read())
                {
                    hasRelationships = true;
                    string constraintName = reader["ConstraintName"].ToString() ?? "";
                    string parentSchema = reader["ParentSchema"].ToString() ?? "";
                    string parentTable = reader["ParentTable"].ToString() ?? "";
                    string parentColumn = reader["ParentColumn"].ToString() ?? "";
                    string referencedSchema = reader["ReferencedSchema"].ToString() ?? "";
                    string referencedTable = reader["ReferencedTable"].ToString() ?? "";
                    string referencedColumn = reader["ReferencedColumn"].ToString() ?? "";
                    string onDelete = reader["OnDelete"].ToString() ?? "";
                    string onUpdate = reader["OnUpdate"].ToString() ?? "";
                    bool isDisabled = reader["IsDisabled"] != DBNull.Value && Convert.ToBoolean(reader["IsDisabled"]);
                    
                    string parentTableFull = $"{parentSchema}.{parentTable}";
                    string referencedTableFull = $"{referencedSchema}.{referencedTable}";
                    
                    relationships.AppendLine($"{constraintName} | {parentTableFull} | {parentColumn} | {referencedTableFull} | {referencedColumn} | {onDelete} | {onUpdate} | {(isDisabled ? "Yes" : "No")}");
                }
                
                if (!hasRelationships)
                {
                    relationships.AppendLine("No relationships found in the database.");
                }
                
                return relationships.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}