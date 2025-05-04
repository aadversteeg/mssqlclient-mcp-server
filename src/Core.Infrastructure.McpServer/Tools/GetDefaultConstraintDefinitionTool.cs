using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetDefaultConstraintDefinitionTool
    {
        private readonly string? _connectionString;

        public GetDefaultConstraintDefinitionTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetDefaultConstraintDefinitionTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_default_constraint_definition"), Description("Get the definition of a default constraint.")]
        public string GetDefaultConstraintDefinition(string constraintName)
        {
            Console.Error.WriteLine($"GetDefaultConstraintDefinition called with constraintName: {constraintName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(constraintName))
            {
                return "Error: Constraint name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema and constraint name
                string schemaName = "dbo"; // Default schema
                string constName = constraintName;
                
                // If there's a schema specifier in the constraint name
                if (constraintName.Contains('.'))
                {
                    string[] parts = constraintName.Split('.', 2);
                    schemaName = parts[0];
                    constName = parts[1];
                }
                
                // Check if the constraint exists
                string checkQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.default_constraints dc
                    INNER JOIN 
                        sys.objects o ON dc.object_id = o.object_id
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    WHERE 
                        o.name = @ConstraintName
                        AND s.name = @SchemaName";
                
                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@ConstraintName", constName);
                checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                int constraintCount = (int)checkCommand.ExecuteScalar();
                
                if (constraintCount == 0)
                {
                    return $"Error: Default constraint '{schemaName}.{constName}' not found in the database.";
                }
                
                // Get the constraint details
                string detailsQuery = @"
                    SELECT 
                        dc.object_id,
                        dc.parent_object_id,
                        dc.parent_column_id,
                        dc.definition,
                        t.name AS TableName,
                        ts.name AS TableSchema,
                        c.name AS ColumnName,
                        ty.name AS DataType,
                        c.max_length AS MaxLength,
                        c.precision AS Precision,
                        c.scale AS Scale,
                        c.is_nullable AS IsNullable,
                        CONVERT(VARCHAR(20), dc.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), dc.modify_date, 120) AS ModifiedDate
                    FROM 
                        sys.default_constraints dc
                    INNER JOIN 
                        sys.objects o ON dc.object_id = o.object_id
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    INNER JOIN 
                        sys.tables t ON dc.parent_object_id = t.object_id
                    INNER JOIN 
                        sys.schemas ts ON t.schema_id = ts.schema_id
                    INNER JOIN 
                        sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
                    INNER JOIN 
                        sys.types ty ON c.system_type_id = ty.system_type_id AND c.user_type_id = ty.user_type_id
                    WHERE 
                        o.name = @ConstraintName
                        AND s.name = @SchemaName";
                
                using SqlCommand detailsCommand = new SqlCommand(detailsQuery, connection);
                detailsCommand.Parameters.AddWithValue("@ConstraintName", constName);
                detailsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader detailsReader = detailsCommand.ExecuteReader();
                
                if (detailsReader.Read())
                {
                    string tableSchema = detailsReader["TableSchema"].ToString() ?? "";
                    string tableName = detailsReader["TableName"].ToString() ?? "";
                    string columnName = detailsReader["ColumnName"].ToString() ?? "";
                    string dataType = detailsReader["DataType"].ToString() ?? "";
                    int maxLength = Convert.ToInt32(detailsReader["MaxLength"]);
                    byte precision = Convert.ToByte(detailsReader["Precision"]);
                    byte scale = Convert.ToByte(detailsReader["Scale"]);
                    bool isNullable = Convert.ToBoolean(detailsReader["IsNullable"]);
                    string definition = detailsReader["definition"].ToString() ?? "";
                    string createdDate = detailsReader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = detailsReader["ModifiedDate"].ToString() ?? "";
                    
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
                    result.AppendLine($"Definition of default constraint: {schemaName}.{constName}");
                    result.AppendLine();
                    
                    result.AppendLine("Constraint Details:");
                    result.AppendLine($"Table: {tableSchema}.{tableName}");
                    result.AppendLine($"Column: {columnName}");
                    result.AppendLine($"Data Type: {formattedDataType}");
                    result.AppendLine($"Is Nullable: {(isNullable ? "Yes" : "No")}");
                    result.AppendLine($"Default Value: {definition}");
                    result.AppendLine($"Created Date: {createdDate}");
                    result.AppendLine($"Modified Date: {modifiedDate}");
                    result.AppendLine();
                    
                    // Generate CREATE statement
                    result.AppendLine("SQL Definition:");
                    result.AppendLine("```sql");
                    result.AppendLine($"ALTER TABLE [{tableSchema}].[{tableName}] ADD CONSTRAINT [{constName}] DEFAULT {definition} FOR [{columnName}];");
                    result.AppendLine("```");
                    
                    return result.ToString();
                }
                else
                {
                    return $"Error: Could not retrieve details for constraint '{schemaName}.{constName}'.";
                }
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}