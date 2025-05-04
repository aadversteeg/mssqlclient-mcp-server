using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetComputedColumnDefinitionTool
    {
        private readonly string? _connectionString;

        public GetComputedColumnDefinitionTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetComputedColumnDefinitionTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_computed_column_definition"), Description("Get the definition of a computed column.")]
        public string GetComputedColumnDefinition(string tableName, string columnName)
        {
            Console.Error.WriteLine($"GetComputedColumnDefinition called with tableName: {tableName}, columnName: {columnName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                return "Error: Table name cannot be empty";
            }

            if (string.IsNullOrWhiteSpace(columnName))
            {
                return "Error: Column name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema and table name
                string schemaName = "dbo"; // Default schema
                string tblName = tableName;
                
                // If there's a schema specifier in the table name
                if (tableName.Contains('.'))
                {
                    string[] parts = tableName.Split('.', 2);
                    schemaName = parts[0];
                    tblName = parts[1];
                }
                
                // Check if the table exists
                string checkTableQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.tables t
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName";
                
                using SqlCommand checkTableCommand = new SqlCommand(checkTableQuery, connection);
                checkTableCommand.Parameters.AddWithValue("@TableName", tblName);
                checkTableCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                int tableCount = (int)checkTableCommand.ExecuteScalar();
                
                if (tableCount == 0)
                {
                    return $"Error: Table '{schemaName}.{tblName}' not found in the database.";
                }
                
                // Check if the column exists and is computed
                string checkColumnQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.tables t
                    INNER JOIN 
                        sys.schemas s ON t.schema_id = s.schema_id
                    INNER JOIN 
                        sys.columns c ON t.object_id = c.object_id
                    WHERE 
                        t.name = @TableName
                        AND s.name = @SchemaName
                        AND c.name = @ColumnName
                        AND c.is_computed = 1";
                
                using SqlCommand checkColumnCommand = new SqlCommand(checkColumnQuery, connection);
                checkColumnCommand.Parameters.AddWithValue("@TableName", tblName);
                checkColumnCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                checkColumnCommand.Parameters.AddWithValue("@ColumnName", columnName);
                
                int computedColumnCount = (int)checkColumnCommand.ExecuteScalar();
                
                if (computedColumnCount == 0)
                {
                    // Check if column exists but is not computed
                    string checkRegularColumnQuery = @"
                        SELECT 
                            COUNT(*) 
                        FROM 
                            sys.tables t
                        INNER JOIN 
                            sys.schemas s ON t.schema_id = s.schema_id
                        INNER JOIN 
                            sys.columns c ON t.object_id = c.object_id
                        WHERE 
                            t.name = @TableName
                            AND s.name = @SchemaName
                            AND c.name = @ColumnName";
                    
                    using SqlCommand checkRegularColumnCommand = new SqlCommand(checkRegularColumnQuery, connection);
                    checkRegularColumnCommand.Parameters.AddWithValue("@TableName", tblName);
                    checkRegularColumnCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    checkRegularColumnCommand.Parameters.AddWithValue("@ColumnName", columnName);
                    
                    int regularColumnCount = (int)checkRegularColumnCommand.ExecuteScalar();
                    
                    if (regularColumnCount > 0)
                    {
                        return $"Error: Column '{columnName}' in table '{schemaName}.{tblName}' exists but is not a computed column.";
                    }
                    else
                    {
                        return $"Error: Column '{columnName}' not found in table '{schemaName}.{tblName}'.";
                    }
                }
                
                // Get the computed column details
                string columnDetailsQuery = @"
                    SELECT 
                        c.name AS ColumnName,
                        t.name AS DataType,
                        c.max_length AS MaxLength,
                        c.precision AS Precision,
                        c.scale AS Scale,
                        c.is_nullable AS IsNullable,
                        cc.definition AS ComputedDefinition,
                        cc.is_persisted AS IsPersisted,
                        CONVERT(VARCHAR(20), c.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), c.modify_date, 120) AS ModifiedDate
                    FROM 
                        sys.tables tbl
                    INNER JOIN 
                        sys.schemas s ON tbl.schema_id = s.schema_id
                    INNER JOIN 
                        sys.columns c ON tbl.object_id = c.object_id
                    INNER JOIN 
                        sys.types t ON c.system_type_id = t.system_type_id AND c.user_type_id = t.user_type_id
                    INNER JOIN 
                        sys.computed_columns cc ON c.object_id = cc.object_id AND c.column_id = cc.column_id
                    WHERE 
                        tbl.name = @TableName
                        AND s.name = @SchemaName
                        AND c.name = @ColumnName";
                
                using SqlCommand columnDetailsCommand = new SqlCommand(columnDetailsQuery, connection);
                columnDetailsCommand.Parameters.AddWithValue("@TableName", tblName);
                columnDetailsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                columnDetailsCommand.Parameters.AddWithValue("@ColumnName", columnName);
                
                using SqlDataReader columnDetailsReader = columnDetailsCommand.ExecuteReader();
                
                if (columnDetailsReader.Read())
                {
                    string colName = columnDetailsReader["ColumnName"].ToString() ?? "";
                    string dataType = columnDetailsReader["DataType"].ToString() ?? "";
                    int maxLength = Convert.ToInt32(columnDetailsReader["MaxLength"]);
                    byte precision = Convert.ToByte(columnDetailsReader["Precision"]);
                    byte scale = Convert.ToByte(columnDetailsReader["Scale"]);
                    bool isNullable = Convert.ToBoolean(columnDetailsReader["IsNullable"]);
                    string computedDefinition = columnDetailsReader["ComputedDefinition"].ToString() ?? "";
                    bool isPersisted = Convert.ToBoolean(columnDetailsReader["IsPersisted"]);
                    string createdDate = columnDetailsReader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = columnDetailsReader["ModifiedDate"].ToString() ?? "";
                    
                    // Format data type with additional info if needed
                    string formattedDataType = dataType;
                    if (dataType.ToLower() is "nvarchar" or "varchar" or "char" or "nchar")
                    {
                        if (dataType.StartsWith("n")) maxLength /= 2; // Unicode types
                        if (maxLength == -1) // MAX
                        {
                            formattedDataType += "(MAX)";
                        }
                        else
                        {
                            formattedDataType += $"({maxLength})";
                        }
                    }
                    else if (dataType.ToLower() is "decimal" or "numeric")
                    {
                        formattedDataType += $"({precision},{scale})";
                    }
                    
                    StringBuilder result = new StringBuilder();
                    result.AppendLine($"Definition of computed column: {columnName} in table {schemaName}.{tblName}");
                    result.AppendLine();
                    
                    result.AppendLine("Column Details:");
                    result.AppendLine($"Data Type: {formattedDataType}");
                    result.AppendLine($"Is Nullable: {(isNullable ? "Yes" : "No")}");
                    result.AppendLine($"Is Persisted: {(isPersisted ? "Yes" : "No")}");
                    result.AppendLine($"Computation Expression: {computedDefinition}");
                    result.AppendLine($"Created Date: {createdDate}");
                    result.AppendLine($"Modified Date: {modifiedDate}");
                    result.AppendLine();
                    
                    // Generate ALTER TABLE statement
                    result.AppendLine("SQL Definition:");
                    result.AppendLine("```sql");
                    result.AppendLine($"ALTER TABLE [{schemaName}].[{tblName}] ADD [{colName}] AS ({computedDefinition}){(isPersisted ? " PERSISTED" : "")};");
                    result.AppendLine("```");
                    
                    return result.ToString();
                }
                else
                {
                    return $"Error: Could not retrieve details for computed column '{columnName}' in table '{schemaName}.{tblName}'.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}