using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetFunctionDefinitionTool
    {
        private readonly string? _connectionString;

        public GetFunctionDefinitionTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetFunctionDefinitionTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_function_definition"), Description("Get the SQL definition of a function.")]
        public string GetFunctionDefinition(string functionName)
        {
            Console.Error.WriteLine($"GetFunctionDefinition called with functionName: {functionName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(functionName))
            {
                return "Error: Function name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema and function name
                string schemaName = "dbo"; // Default schema
                string funcName = functionName;
                
                // If there's a schema specifier in the function name
                if (functionName.Contains('.'))
                {
                    string[] parts = functionName.Split('.', 2);
                    schemaName = parts[0];
                    funcName = parts[1];
                }
                
                // Check if the function exists
                string checkQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    WHERE 
                        o.name = @FuncName
                        AND s.name = @SchemaName
                        AND o.type IN ('FN', 'IF', 'TF')"; // SQL scalar, inline table-valued, or table-valued function
                
                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@FuncName", funcName);
                checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                int functionCount = (int)checkCommand.ExecuteScalar();
                
                if (functionCount == 0)
                {
                    return $"Error: Function '{schemaName}.{funcName}' not found in the database.";
                }
                
                // Get the function type
                string typeQuery = @"
                    SELECT 
                        o.type
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    WHERE 
                        o.name = @FuncName
                        AND s.name = @SchemaName";
                
                using SqlCommand typeCommand = new SqlCommand(typeQuery, connection);
                typeCommand.Parameters.AddWithValue("@FuncName", funcName);
                typeCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                string functionType = typeCommand.ExecuteScalar()?.ToString() ?? "";
                string functionTypeName = "";
                
                switch (functionType)
                {
                    case "FN":
                        functionTypeName = "Scalar Function";
                        break;
                    case "IF":
                        functionTypeName = "Inline Table-Valued Function";
                        break;
                    case "TF":
                        functionTypeName = "Table-Valued Function";
                        break;
                    default:
                        functionTypeName = "Unknown Function Type";
                        break;
                }
                
                // Check if the function is encrypted
                string encryptedQuery = @"
                    SELECT 
                        m.is_encrypted 
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.sql_modules m ON o.object_id = m.object_id
                    WHERE 
                        o.name = @FuncName
                        AND s.name = @SchemaName";
                
                using SqlCommand encryptedCommand = new SqlCommand(encryptedQuery, connection);
                encryptedCommand.Parameters.AddWithValue("@FuncName", funcName);
                encryptedCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                bool isEncrypted = Convert.ToBoolean(encryptedCommand.ExecuteScalar());
                
                if (isEncrypted)
                {
                    return $"The function '{schemaName}.{funcName}' is encrypted and its definition cannot be viewed.";
                }
                
                // Get the function definition
                string definitionQuery = @"
                    SELECT 
                        m.definition
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    INNER JOIN 
                        sys.sql_modules m ON o.object_id = m.object_id
                    WHERE 
                        o.name = @FuncName
                        AND s.name = @SchemaName";
                
                using SqlCommand definitionCommand = new SqlCommand(definitionQuery, connection);
                definitionCommand.Parameters.AddWithValue("@FuncName", funcName);
                definitionCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                string? definition = (string?)definitionCommand.ExecuteScalar();
                
                if (string.IsNullOrEmpty(definition))
                {
                    return $"The function '{schemaName}.{funcName}' has no definition.";
                }
                
                // Get function parameters
                string paramQuery = @"
                    SELECT 
                        p.name AS ParameterName,
                        t.name AS DataType,
                        CASE WHEN t.name IN ('nvarchar', 'nchar', 'varchar', 'char', 'binary', 'varbinary')
                            THEN p.max_length
                            ELSE NULL
                        END AS MaxLength,
                        p.precision AS Precision,
                        p.scale AS Scale,
                        p.is_output AS IsOutput,
                        CASE WHEN p.has_default_value = 1
                            THEN OBJECT_DEFINITION(p.default_object_id)
                            ELSE NULL
                        END AS DefaultValue
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    INNER JOIN 
                        sys.parameters p ON o.object_id = p.object_id
                    INNER JOIN 
                        sys.types t ON p.system_type_id = t.system_type_id AND p.user_type_id = t.user_type_id
                    WHERE 
                        o.name = @FuncName
                        AND s.name = @SchemaName
                        AND p.parameter_id > 0 -- Exclude return value
                    ORDER BY 
                        p.parameter_id";
                
                using SqlCommand paramCommand = new SqlCommand(paramQuery, connection);
                paramCommand.Parameters.AddWithValue("@FuncName", funcName);
                paramCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader paramReader = paramCommand.ExecuteReader();
                
                StringBuilder result = new StringBuilder();
                result.AppendLine($"Definition of {functionTypeName.ToLower()}: {schemaName}.{funcName}");
                result.AppendLine();
                
                // Add parameters section if there are any
                if (paramReader.HasRows)
                {
                    result.AppendLine("Parameters:");
                    result.AppendLine("Name | Data Type | Max Length | Precision | Scale | Is Output | Default Value");
                    result.AppendLine("---- | --------- | ---------- | --------- | ----- | --------- | -------------");
                    
                    while (paramReader.Read())
                    {
                        string paramName = paramReader["ParameterName"].ToString() ?? "";
                        string dataType = paramReader["DataType"].ToString() ?? "";
                        string maxLength = paramReader["MaxLength"] == DBNull.Value ? "N/A" : paramReader["MaxLength"].ToString() ?? "";
                        string precision = paramReader["Precision"] == DBNull.Value ? "N/A" : paramReader["Precision"].ToString() ?? "";
                        string scale = paramReader["Scale"] == DBNull.Value ? "N/A" : paramReader["Scale"].ToString() ?? "";
                        bool isOutput = Convert.ToBoolean(paramReader["IsOutput"]);
                        string defaultValue = paramReader["DefaultValue"] == DBNull.Value ? "N/A" : paramReader["DefaultValue"].ToString() ?? "";
                        
                        result.AppendLine($"{paramName} | {dataType} | {maxLength} | {precision} | {scale} | {(isOutput ? "Yes" : "No")} | {defaultValue}");
                    }
                    
                    result.AppendLine();
                }
                
                paramReader.Close();
                
                // Get function return type (for scalar functions)
                if (functionType == "FN") // Scalar function
                {
                    string returnTypeQuery = @"
                        SELECT 
                            t.name AS ReturnType,
                            CASE WHEN t.name IN ('nvarchar', 'nchar', 'varchar', 'char', 'binary', 'varbinary')
                                THEN p.max_length
                                ELSE NULL
                            END AS MaxLength,
                            p.precision AS Precision,
                            p.scale AS Scale
                        FROM 
                            sys.objects o
                        INNER JOIN 
                            sys.schemas s ON o.schema_id = s.schema_id
                        INNER JOIN 
                            sys.parameters p ON o.object_id = p.object_id
                        INNER JOIN 
                            sys.types t ON p.system_type_id = t.system_type_id AND p.user_type_id = t.user_type_id
                        WHERE 
                            o.name = @FuncName
                            AND s.name = @SchemaName
                            AND p.parameter_id = 0"; // Return value has parameter_id = 0
                    
                    using SqlCommand returnTypeCommand = new SqlCommand(returnTypeQuery, connection);
                    returnTypeCommand.Parameters.AddWithValue("@FuncName", funcName);
                    returnTypeCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    
                    using SqlDataReader returnTypeReader = returnTypeCommand.ExecuteReader();
                    
                    if (returnTypeReader.Read())
                    {
                        string returnType = returnTypeReader["ReturnType"].ToString() ?? "";
                        string maxLength = returnTypeReader["MaxLength"] == DBNull.Value ? "" : $"({returnTypeReader["MaxLength"]})";
                        string precision = returnTypeReader["Precision"] == DBNull.Value ? "" : returnTypeReader["Precision"].ToString() ?? "";
                        string scale = returnTypeReader["Scale"] == DBNull.Value ? "" : $",{returnTypeReader["Scale"]})";
                        
                        string fullReturnType = returnType;
                        if (!string.IsNullOrEmpty(maxLength))
                        {
                            fullReturnType += maxLength;
                        }
                        else if (!string.IsNullOrEmpty(precision))
                        {
                            fullReturnType += $"({precision}{scale}";
                        }
                        
                        result.AppendLine("Return Type:");
                        result.AppendLine(fullReturnType);
                        result.AppendLine();
                    }
                    
                    returnTypeReader.Close();
                }
                // For table-valued functions (TF), get the return table structure
                else if (functionType == "TF")
                {
                    string tableColumnsQuery = @"
                        SELECT 
                            c.name AS ColumnName,
                            t.name AS DataType,
                            CASE WHEN t.name IN ('nvarchar', 'nchar', 'varchar', 'char', 'binary', 'varbinary')
                                THEN c.max_length
                                ELSE NULL
                            END AS MaxLength,
                            c.precision AS Precision,
                            c.scale AS Scale,
                            c.is_nullable AS IsNullable
                        FROM 
                            sys.objects o
                        INNER JOIN 
                            sys.schemas s ON o.schema_id = s.schema_id
                        INNER JOIN 
                            sys.columns c ON o.object_id = c.object_id
                        INNER JOIN 
                            sys.types t ON c.system_type_id = t.system_type_id AND c.user_type_id = t.user_type_id
                        WHERE 
                            o.name = @FuncName
                            AND s.name = @SchemaName
                        ORDER BY 
                            c.column_id";
                    
                    using SqlCommand tableColumnsCommand = new SqlCommand(tableColumnsQuery, connection);
                    tableColumnsCommand.Parameters.AddWithValue("@FuncName", funcName);
                    tableColumnsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    
                    using SqlDataReader tableColumnsReader = tableColumnsCommand.ExecuteReader();
                    
                    if (tableColumnsReader.HasRows)
                    {
                        result.AppendLine("Return Table Structure:");
                        result.AppendLine("Column Name | Data Type | Max Length | Precision | Scale | Is Nullable");
                        result.AppendLine("----------- | --------- | ---------- | --------- | ----- | -----------");
                        
                        while (tableColumnsReader.Read())
                        {
                            string columnName = tableColumnsReader["ColumnName"].ToString() ?? "";
                            string dataType = tableColumnsReader["DataType"].ToString() ?? "";
                            string maxLength = tableColumnsReader["MaxLength"] == DBNull.Value ? "N/A" : tableColumnsReader["MaxLength"].ToString() ?? "";
                            string precision = tableColumnsReader["Precision"] == DBNull.Value ? "N/A" : tableColumnsReader["Precision"].ToString() ?? "";
                            string scale = tableColumnsReader["Scale"] == DBNull.Value ? "N/A" : tableColumnsReader["Scale"].ToString() ?? "";
                            bool isNullable = Convert.ToBoolean(tableColumnsReader["IsNullable"]);
                            
                            result.AppendLine($"{columnName} | {dataType} | {maxLength} | {precision} | {scale} | {(isNullable ? "Yes" : "No")}");
                        }
                        
                        result.AppendLine();
                    }
                    
                    tableColumnsReader.Close();
                }
                
                // Get function creation date and other metadata
                string metadataQuery = @"
                    SELECT 
                        CONVERT(VARCHAR(20), o.create_date, 120) AS CreatedDate,
                        CONVERT(VARCHAR(20), o.modify_date, 120) AS ModifiedDate,
                        OBJECTPROPERTY(o.object_id, 'ExecIsAnsiNullsOn') AS IsAnsiNullsOn,
                        OBJECTPROPERTY(o.object_id, 'ExecIsQuotedIdentOn') AS IsQuotedIdentOn,
                        OBJECTPROPERTY(o.object_id, 'IsSchemaBound') AS IsSchemaBound
                    FROM 
                        sys.objects o
                    INNER JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    WHERE 
                        o.name = @FuncName
                        AND s.name = @SchemaName";
                
                using SqlCommand metadataCommand = new SqlCommand(metadataQuery, connection);
                metadataCommand.Parameters.AddWithValue("@FuncName", funcName);
                metadataCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using SqlDataReader metadataReader = metadataCommand.ExecuteReader();
                
                if (metadataReader.Read())
                {
                    string createdDate = metadataReader["CreatedDate"].ToString() ?? "";
                    string modifiedDate = metadataReader["ModifiedDate"].ToString() ?? "";
                    bool isAnsiNullsOn = Convert.ToBoolean(metadataReader["IsAnsiNullsOn"]);
                    bool isQuotedIdentOn = Convert.ToBoolean(metadataReader["IsQuotedIdentOn"]);
                    bool isSchemaBound = Convert.ToBoolean(metadataReader["IsSchemaBound"]);
                    
                    result.AppendLine("Metadata:");
                    result.AppendLine($"Function Type: {functionTypeName}");
                    result.AppendLine($"Created Date: {createdDate}");
                    result.AppendLine($"Modified Date: {modifiedDate}");
                    result.AppendLine($"ANSI_NULLS: {(isAnsiNullsOn ? "ON" : "OFF")}");
                    result.AppendLine($"QUOTED_IDENTIFIER: {(isQuotedIdentOn ? "ON" : "OFF")}");
                    result.AppendLine($"Schema Bound: {(isSchemaBound ? "Yes" : "No")}");
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