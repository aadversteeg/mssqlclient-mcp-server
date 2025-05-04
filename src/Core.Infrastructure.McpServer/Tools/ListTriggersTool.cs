using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class ListTriggersTool
    {
        private readonly string? _connectionString;

        public ListTriggersTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"ListTriggersTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "list_triggers"), Description("List all triggers in the database.")]
        public string ListTriggers()
        {
            Console.Error.WriteLine($"ListTriggers called");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Query to get all triggers
                string query = @"
                    SELECT 
                        tr.name AS TriggerName,
                        SCHEMA_NAME(o.schema_id) AS SchemaName,
                        o.name AS TableName,
                        CASE 
                            WHEN tr.is_instead_of_trigger = 1 THEN 'INSTEAD OF'
                            ELSE 'AFTER'
                        END AS TriggerType,
                        CASE
                            WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsInsertTrigger') = 1 THEN 'INSERT'
                            WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsUpdateTrigger') = 1 THEN 'UPDATE'
                            WHEN OBJECTPROPERTY(tr.object_id, 'ExecIsDeleteTrigger') = 1 THEN 'DELETE'
                            ELSE 'MULTIPLE'
                        END AS TriggerEvent,
                        CASE
                            WHEN tr.is_disabled = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsDisabled,
                        CASE
                            WHEN tr.is_not_for_replication = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS NotForReplication,
                        CONVERT(VARCHAR(20), tr.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), tr.modify_date, 120) AS ModifiedDate,
                        CASE
                            WHEN m.is_encrypted = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsEncrypted,
                        CASE
                            WHEN ep.value IS NOT NULL THEN ep.value
                            ELSE ''
                        END AS Description
                    FROM 
                        sys.triggers tr
                    INNER JOIN 
                        sys.objects o ON tr.parent_id = o.object_id
                    LEFT JOIN 
                        sys.sql_modules m ON tr.object_id = m.object_id
                    LEFT JOIN 
                        sys.extended_properties ep ON tr.object_id = ep.major_id AND ep.minor_id = 0 AND ep.name = 'MS_Description'
                    ORDER BY 
                        SchemaName, TableName, tr.name";
                
                using SqlCommand command = new SqlCommand(query, connection);
                using SqlDataReader reader = command.ExecuteReader();
                
                StringBuilder triggerList = new StringBuilder();
                triggerList.AppendLine("Database Triggers:");
                triggerList.AppendLine();
                triggerList.AppendLine("Schema | Table Name | Trigger Name | Type | Event | Disabled | Encrypted | Created Date | Modified Date | Description");
                triggerList.AppendLine("------ | ---------- | ------------ | ---- | ----- | -------- | --------- | ------------ | ------------- | -----------");
                
                bool hasTriggers = false;
                while (reader.Read())
                {
                    hasTriggers = true;
                    string schemaName = reader["SchemaName"].ToString() ?? "";
                    string tableName = reader["TableName"].ToString() ?? "";
                    string triggerName = reader["TriggerName"].ToString() ?? "";
                    string triggerType = reader["TriggerType"].ToString() ?? "";
                    string triggerEvent = reader["TriggerEvent"].ToString() ?? "";
                    string isDisabled = reader["IsDisabled"].ToString() ?? "No";
                    string isEncrypted = reader["IsEncrypted"].ToString() ?? "No";
                    string createdDate = reader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = reader["ModifiedDate"].ToString() ?? "";
                    string description = reader["Description"].ToString() ?? "";
                    
                    triggerList.AppendLine($"{schemaName} | {tableName} | {triggerName} | {triggerType} | {triggerEvent} | {isDisabled} | {isEncrypted} | {createdDate} | {modifiedDate} | {description}");
                }
                
                if (!hasTriggers)
                {
                    triggerList.AppendLine("No triggers found in the database.");
                }
                
                return triggerList.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: SQL error: {ex.Message}";
            }
        }
    }
}