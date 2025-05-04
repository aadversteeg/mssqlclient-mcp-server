using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetTriggerDefinitionTool
    {
        private readonly string? _connectionString;

        public GetTriggerDefinitionTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetTriggerDefinitionTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_trigger_definition"), Description("Get the SQL definition of a trigger.")]
        public string GetTriggerDefinition(string triggerName)
        {
            Console.Error.WriteLine($"GetTriggerDefinition called with triggerName: {triggerName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(triggerName))
            {
                return "Error: Trigger name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema and trigger name
                string schemaName = "dbo"; // Default schema
                string trigName = triggerName;
                
                // If there's a schema specifier in the trigger name
                if (triggerName.Contains('.'))
                {
                    string[] parts = triggerName.Split('.', 2);
                    schemaName = parts[0];
                    trigName = parts[1];
                }
                
                // Check if the trigger exists
                string checkQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.triggers t
                    INNER JOIN 
                        sys.objects o ON t.object_id = o.object_id
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    WHERE 
                        t.name = @TriggerName
                        AND s.name = @SchemaName";
                
                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@TriggerName", trigName);
                checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                int triggerCount = (int)checkCommand.ExecuteScalar();
                
                if (triggerCount == 0)
                {
                    return $"Error: Trigger '{schemaName}.{trigName}' not found in the database.";
                }
                
                // Check if the trigger is encrypted
                string encryptedQuery = @"
                    SELECT 
                        m.is_encrypted 
                    FROM 
                        sys.triggers t
                    INNER JOIN 
                        sys.objects o ON t.object_id = o.object_id
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.sql_modules m ON t.object_id = m.object_id
                    WHERE 
                        t.name = @TriggerName
                        AND s.name = @SchemaName";
                
                using SqlCommand encryptedCommand = new SqlCommand(encryptedQuery, connection);
                encryptedCommand.Parameters.AddWithValue("@TriggerName", trigName);
                encryptedCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                bool isEncrypted = Convert.ToBoolean(encryptedCommand.ExecuteScalar());
                
                if (isEncrypted)
                {
                    return $"The trigger '{schemaName}.{trigName}' is encrypted and its definition cannot be viewed.";
                }
                
                // Get the trigger definition
                string definitionQuery = @"
                    SELECT 
                        m.definition
                    FROM 
                        sys.triggers t
                    INNER JOIN 
                        sys.objects o ON t.object_id = o.object_id
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    INNER JOIN 
                        sys.sql_modules m ON t.object_id = m.object_id
                    WHERE 
                        t.name = @TriggerName
                        AND s.name = @SchemaName";
                
                using SqlCommand definitionCommand = new SqlCommand(definitionQuery, connection);
                definitionCommand.Parameters.AddWithValue("@TriggerName", trigName);
                definitionCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                string? definition = (string?)definitionCommand.ExecuteScalar();
                
                if (string.IsNullOrEmpty(definition))
                {
                    return $"The trigger '{schemaName}.{trigName}' has no definition.";
                }
                
                // Get trigger metadata
                string metadataQuery = @"
                    SELECT 
                        CONVERT(VARCHAR(20), t.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), t.modify_date, 120) AS ModifiedDate,
                        t.is_disabled AS IsDisabled,
                        OBJECT_NAME(t.parent_id) AS TableName,
                        s2.name AS TableSchema,
                        OBJECTPROPERTY(t.object_id, 'ExecIsUpdateTrigger') AS IsUpdate,
                        OBJECTPROPERTY(t.object_id, 'ExecIsDeleteTrigger') AS IsDelete,
                        OBJECTPROPERTY(t.object_id, 'ExecIsInsertTrigger') AS IsInsert,
                        OBJECTPROPERTY(t.object_id, 'ExecIsAfterTrigger') AS IsAfter,
                        OBJECTPROPERTY(t.object_id, 'ExecIsInsteadOfTrigger') AS IsInsteadOf
                    FROM 
                        sys.triggers t
                    INNER JOIN 
                        sys.objects o ON t.object_id = o.object_id
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.objects o2 ON t.parent_id = o2.object_id
                    LEFT JOIN 
                        sys.schemas s2 ON o2.schema_id = s2.schema_id
                    WHERE 
                        t.name = @TriggerName
                        AND s.name = @SchemaName";
                
                using SqlCommand metadataCommand = new SqlCommand(metadataQuery, connection);
                metadataCommand.Parameters.AddWithValue("@TriggerName", trigName);
                metadataCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader metadataReader = metadataCommand.ExecuteReader();
                
                StringBuilder result = new StringBuilder();
                result.AppendLine($"Definition of trigger: {schemaName}.{trigName}");
                result.AppendLine();
                
                if (metadataReader.Read())
                {
                    string createdDate = metadataReader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = metadataReader["ModifiedDate"].ToString() ?? "";
                    bool isDisabled = Convert.ToBoolean(metadataReader["IsDisabled"]);
                    string tableName = metadataReader["TableName"].ToString() ?? "";
                    string tableSchema = metadataReader["TableSchema"].ToString() ?? "";
                    bool isUpdate = Convert.ToBoolean(metadataReader["IsUpdate"]);
                    bool isDelete = Convert.ToBoolean(metadataReader["IsDelete"]);
                    bool isInsert = Convert.ToBoolean(metadataReader["IsInsert"]);
                    bool isAfter = Convert.ToBoolean(metadataReader["IsAfter"]);
                    bool isInsteadOf = Convert.ToBoolean(metadataReader["IsInsteadOf"]);
                    
                    // Build trigger events string
                    List<string> events = new List<string>();
                    if (isInsert) events.Add("INSERT");
                    if (isUpdate) events.Add("UPDATE");
                    if (isDelete) events.Add("DELETE");
                    
                    string triggerType = isAfter ? "AFTER" : isInsteadOf ? "INSTEAD OF" : "UNKNOWN";
                    
                    result.AppendLine("Metadata:");
                    result.AppendLine($"Created Date: {createdDate}");
                    result.AppendLine($"Modified Date: {modifiedDate}");
                    result.AppendLine($"Status: {(isDisabled ? "DISABLED" : "ENABLED")}");
                    result.AppendLine($"Parent Table: {tableSchema}.{tableName}");
                    result.AppendLine($"Trigger Type: {triggerType}");
                    result.AppendLine($"Trigger Events: {string.Join(", ", events)}");
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