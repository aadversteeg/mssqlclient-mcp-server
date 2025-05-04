using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetStoredProcedureDefinitionTool
    {
        private readonly string? _connectionString;

        public GetStoredProcedureDefinitionTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetStoredProcedureDefinitionTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_stored_procedure_definition"), Description("Get the SQL definition of a stored procedure.")]
        public string GetStoredProcedureDefinition(string procedureName)
        {
            Console.Error.WriteLine($"GetStoredProcedureDefinition called with procedureName: {procedureName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema and procedure name
                string schemaName = "dbo"; // Default schema
                string procName = procedureName;
                
                // If there's a schema specifier in the procedure name
                if (procedureName.Contains('.'))
                {
                    string[] parts = procedureName.Split('.', 2);
                    schemaName = parts[0];
                    procName = parts[1];
                }
                
                // Check if the procedure exists
                string checkQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.procedures p
                    INNER JOIN 
                        sys.schemas s ON p.schema_id = s.schema_id
                    WHERE 
                        p.name = @ProcName
                        AND s.name = @SchemaName";
                
                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@ProcName", procName);
                checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                int procedureCount = (int)checkCommand.ExecuteScalar();
                
                if (procedureCount == 0)
                {
                    return $"Error: Stored procedure '{schemaName}.{procName}' not found in the database.";
                }
                
                // Check if the procedure is encrypted
                string encryptedQuery = @"
                    SELECT 
                        m.is_encrypted 
                    FROM 
                        sys.procedures p
                    INNER JOIN 
                        sys.schemas s ON p.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.sql_modules m ON p.object_id = m.object_id
                    WHERE 
                        p.name = @ProcName
                        AND s.name = @SchemaName";
                
                using SqlCommand encryptedCommand = new SqlCommand(encryptedQuery, connection);
                encryptedCommand.Parameters.AddWithValue("@ProcName", procName);
                encryptedCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                bool isEncrypted = Convert.ToBoolean(encryptedCommand.ExecuteScalar());
                
                if (isEncrypted)
                {
                    return $"The stored procedure '{schemaName}.{procName}' is encrypted and its definition cannot be viewed.";
                }
                
                // Get the procedure definition
                string definitionQuery = @"
                    SELECT 
                        m.definition
                    FROM 
                        sys.procedures p
                    INNER JOIN 
                        sys.schemas s ON p.schema_id = s.schema_id
                    INNER JOIN 
                        sys.sql_modules m ON p.object_id = m.object_id
                    WHERE 
                        p.name = @ProcName
                        AND s.name = @SchemaName";
                
                using SqlCommand definitionCommand = new SqlCommand(definitionQuery, connection);
                definitionCommand.Parameters.AddWithValue("@ProcName", procName);
                definitionCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                string? definition = (string?)definitionCommand.ExecuteScalar();
                
                if (string.IsNullOrEmpty(definition))
                {
                    return $"The stored procedure '{schemaName}.{procName}' has no definition.";
                }
                
                // Get procedure parameters
                string paramQuery = @"
                    SELECT 
                        p.name AS ParameterName,
                        t.name AS DataType,
                        CASE WHEN t.name IN ('nvarchar', 'nchar', 'varchar', 'char', 'binary', 'varbinary')
                            THEN p.max_length
                            ELSE NULL
                        END AS MaxLength,
                        p.is_output AS IsOutput,
                        CASE WHEN p.has_default_value = 1
                            THEN OBJECT_DEFINITION(p.default_object_id)
                            ELSE NULL
                        END AS DefaultValue
                    FROM 
                        sys.procedures proc
                    INNER JOIN 
                        sys.schemas s ON proc.schema_id = s.schema_id
                    INNER JOIN 
                        sys.parameters p ON proc.object_id = p.object_id
                    INNER JOIN 
                        sys.types t ON p.system_type_id = t.system_type_id AND p.user_type_id = t.user_type_id
                    WHERE 
                        proc.name = @ProcName
                        AND s.name = @SchemaName
                    ORDER BY 
                        p.parameter_id";
                
                using SqlCommand paramCommand = new SqlCommand(paramQuery, connection);
                paramCommand.Parameters.AddWithValue("@ProcName", procName);
                paramCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader paramReader = paramCommand.ExecuteReader();
                
                StringBuilder result = new StringBuilder();
                result.AppendLine($"Definition of stored procedure: {schemaName}.{procName}");
                result.AppendLine();
                
                // Add parameters section if there are any
                if (paramReader.HasRows)
                {
                    result.AppendLine("Parameters:");
                    result.AppendLine("Name | Data Type | Max Length | Is Output | Default Value");
                    result.AppendLine("---- | --------- | ---------- | --------- | -------------");
                    
                    while (paramReader.Read())
                    {
                        string paramName = paramReader["ParameterName"].ToString() ?? "";
                        string dataType = paramReader["DataType"].ToString() ?? "";
                        string maxLength = paramReader["MaxLength"] == DBNull.Value ? "N/A" : paramReader["MaxLength"].ToString() ?? "";
                        bool isOutput = Convert.ToBoolean(paramReader["IsOutput"]);
                        string defaultValue = paramReader["DefaultValue"] == DBNull.Value ? "N/A" : paramReader["DefaultValue"].ToString() ?? "";
                        
                        result.AppendLine($"{paramName} | {dataType} | {maxLength} | {(isOutput ? "Yes" : "No")} | {defaultValue}");
                    }
                    
                    result.AppendLine();
                }
                
                paramReader.Close();
                
                // Get procedure creation date and other metadata
                string metadataQuery = @"
                    SELECT 
                        CONVERT(VARCHAR(20), p.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), p.modify_date, 120) AS ModifiedDate,
                        p.is_recompiled AS IsRecompiled
                    FROM 
                        sys.procedures p
                    INNER JOIN 
                        sys.schemas s ON p.schema_id = s.schema_id
                    WHERE 
                        p.name = @ProcName
                        AND s.name = @SchemaName";
                
                using SqlCommand metadataCommand = new SqlCommand(metadataQuery, connection);
                metadataCommand.Parameters.AddWithValue("@ProcName", procName);
                metadataCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader metadataReader = metadataCommand.ExecuteReader();
                
                if (metadataReader.Read())
                {
                    string createdDate = metadataReader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = metadataReader["ModifiedDate"].ToString() ?? "";
                    bool isRecompiled = Convert.ToBoolean(metadataReader["IsRecompiled"]);
                    
                    result.AppendLine("Metadata:");
                    result.AppendLine($"Created Date: {createdDate}");
                    result.AppendLine($"Modified Date: {modifiedDate}");
                    result.AppendLine($"Recompile on Execution: {(isRecompiled ? "Yes" : "No")}");
                    result.AppendLine();
                }
                
                metadataReader.Close();
                
                // Add the SQL definition
                result.AppendLine("SQL Definition:");
                result.AppendLine("```sql");
                result.AppendLine(definition);
                result.AppendLine("```");
                
                return result.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}