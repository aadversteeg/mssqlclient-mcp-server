using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetTableConstraintsTool
    {
        private readonly string? _connectionString;

        public GetTableConstraintsTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetTableConstraintsTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_table_constraints"), Description("Get constraints (primary keys, foreign keys, unique, check) for a specific table.")]
        public string GetTableConstraints(string tableName)
        {
            Console.Error.WriteLine($"GetTableConstraints called with tableName: {tableName}");
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
                
                StringBuilder result = new StringBuilder();
                result.AppendLine($"Constraints for table: {tableName}");
                result.AppendLine();
                
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
                
                // Get primary key constraints
                string pkQuery = @"
                    SELECT 
                        i.name AS ConstraintName,
                        COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
                        i.is_unique AS IsUnique,
                        i.is_primary_key AS IsPrimaryKey
                    FROM 
                        sys.indexes i
                    INNER JOIN 
                        sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    INNER JOIN 
                        sys.tables t ON i.object_id = t.object_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                        AND i.is_primary_key = 1
                    ORDER BY 
                        i.name, ic.key_ordinal";
                
                using SqlCommand pkCommand = new SqlCommand(pkQuery, connection);
                pkCommand.Parameters.AddWithValue("@TableName", tableName);
                pkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader pkReader = pkCommand.ExecuteReader();
                
                result.AppendLine("Primary Key Constraints:");
                result.AppendLine("Constraint Name | Column Name");
                result.AppendLine("--------------- | -----------");
                
                bool hasPk = false;
                while (pkReader.Read())
                {
                    hasPk = true;
                    string constraintName = pkReader["ConstraintName"].ToString() ?? "";
                    string columnName = pkReader["ColumnName"].ToString() ?? "";
                    
                    result.AppendLine($"{constraintName} | {columnName}");
                }
                
                if (!hasPk)
                {
                    result.AppendLine("No primary key constraints defined.");
                }
                
                pkReader.Close();
                
                // Get foreign key constraints
                result.AppendLine();
                result.AppendLine("Foreign Key Constraints:");
                result.AppendLine("Constraint Name | Column Name | Referenced Table | Referenced Column | Delete Action | Update Action");
                result.AppendLine("--------------- | ----------- | ---------------- | ----------------- | ------------- | -------------");
                
                string fkQuery = @"
                    SELECT 
                        fk.name AS ConstraintName,
                        COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                        OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
                        COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn,
                        CASE fk.delete_referential_action
                            WHEN 0 THEN 'No Action'
                            WHEN 1 THEN 'Cascade'
                            WHEN 2 THEN 'Set Null'
                            WHEN 3 THEN 'Set Default'
                            ELSE 'Unknown'
                        END AS DeleteAction,
                        CASE fk.update_referential_action
                            WHEN 0 THEN 'No Action'
                            WHEN 1 THEN 'Cascade'
                            WHEN 2 THEN 'Set Null'
                            WHEN 3 THEN 'Set Default'
                            ELSE 'Unknown'
                        END AS UpdateAction
                    FROM 
                        sys.foreign_keys fk
                    INNER JOIN 
                        sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                    INNER JOIN 
                        sys.tables t ON fk.parent_object_id = t.object_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                    ORDER BY 
                        fk.name, fkc.constraint_column_id";
                
                using SqlCommand fkCommand = new SqlCommand(fkQuery, connection);
                fkCommand.Parameters.AddWithValue("@TableName", tableName);
                fkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader fkReader = fkCommand.ExecuteReader();
                
                bool hasFk = false;
                while (fkReader.Read())
                {
                    hasFk = true;
                    string constraintName = fkReader["ConstraintName"].ToString() ?? "";
                    string columnName = fkReader["ColumnName"].ToString() ?? "";
                    string referencedTable = fkReader["ReferencedTable"].ToString() ?? "";
                    string referencedColumn = fkReader["ReferencedColumn"].ToString() ?? "";
                    string deleteAction = fkReader["DeleteAction"].ToString() ?? "";
                    string updateAction = fkReader["UpdateAction"].ToString() ?? "";
                    
                    result.AppendLine($"{constraintName} | {columnName} | {referencedTable} | {referencedColumn} | {deleteAction} | {updateAction}");
                }
                
                if (!hasFk)
                {
                    result.AppendLine("No foreign key constraints defined.");
                }
                
                fkReader.Close();
                
                // Get unique constraints (excluding primary keys)
                result.AppendLine();
                result.AppendLine("Unique Constraints:");
                result.AppendLine("Constraint Name | Column Name");
                result.AppendLine("--------------- | -----------");
                
                string uniqueQuery = @"
                    SELECT 
                        i.name AS ConstraintName,
                        COL_NAME(ic.object_id, ic.column_id) AS ColumnName
                    FROM 
                        sys.indexes i
                    INNER JOIN 
                        sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                    INNER JOIN 
                        sys.tables t ON i.object_id = t.object_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                        AND i.is_unique = 1
                        AND i.is_primary_key = 0
                    ORDER BY 
                        i.name, ic.key_ordinal";
                
                using SqlCommand uniqueCommand = new SqlCommand(uniqueQuery, connection);
                uniqueCommand.Parameters.AddWithValue("@TableName", tableName);
                uniqueCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader uniqueReader = uniqueCommand.ExecuteReader();
                
                bool hasUnique = false;
                while (uniqueReader.Read())
                {
                    hasUnique = true;
                    string constraintName = uniqueReader["ConstraintName"].ToString() ?? "";
                    string columnName = uniqueReader["ColumnName"].ToString() ?? "";
                    
                    result.AppendLine($"{constraintName} | {columnName}");
                }
                
                if (!hasUnique)
                {
                    result.AppendLine("No unique constraints defined (excluding primary keys).");
                }
                
                uniqueReader.Close();
                
                // Get check constraints
                result.AppendLine();
                result.AppendLine("Check Constraints:");
                result.AppendLine("Constraint Name | Definition");
                result.AppendLine("--------------- | ----------");
                
                string checkQuery = @"
                    SELECT 
                        cc.name AS ConstraintName,
                        cc.definition AS Definition
                    FROM 
                        sys.check_constraints cc
                    INNER JOIN 
                        sys.tables t ON cc.parent_object_id = t.object_id
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                    ORDER BY 
                        cc.name";
                
                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@TableName", tableName);
                checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader checkReader = checkCommand.ExecuteReader();
                
                bool hasCheck = false;
                while (checkReader.Read())
                {
                    hasCheck = true;
                    string constraintName = checkReader["ConstraintName"].ToString() ?? "";
                    string definition = checkReader["Definition"].ToString() ?? "";
                    
                    // Truncate long check constraint definitions for readability
                    if (definition.Length > 50)
                    {
                        definition = definition.Substring(0, 47) + "...";
                    }
                    
                    result.AppendLine($"{constraintName} | {definition}");
                }
                
                if (!hasCheck)
                {
                    result.AppendLine("No check constraints defined.");
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