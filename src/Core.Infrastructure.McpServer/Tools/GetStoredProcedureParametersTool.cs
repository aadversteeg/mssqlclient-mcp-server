using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// MCP tool for getting parameter information for stored procedures in the current database context.
    /// </summary>
    [McpServerToolType]
    public class GetStoredProcedureParametersTool
    {
        private readonly IDatabaseContext _databaseContext;
        private readonly DatabaseConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the GetStoredProcedureParametersTool class.
        /// </summary>
        /// <param name="databaseContext">The database context service</param>
        /// <param name="configuration">Database configuration</param>
        public GetStoredProcedureParametersTool(IDatabaseContext databaseContext, IOptions<DatabaseConfiguration> configuration)
        {
            _databaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Gets parameter information for a stored procedure including names, types, and whether they are required.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure (can include schema like 'dbo.MyProcedure')</param>
        /// <param name="format">Output format: 'table' for human-readable table (default), 'json' for JSON Schema format</param>
        /// <param name="timeoutSeconds">Optional query timeout in seconds</param>
        /// <returns>Parameter information in the requested format</returns>
        [McpServerTool(Name = "get_stored_procedure_parameters"), 
         Description(@"Get parameter information for a stored procedure including names, types, and whether they are required.

This tool helps you understand what parameters a stored procedure expects before calling it.
It supports two output formats:

1. Table format (default): Shows a formatted table with parameter details and example usage
2. JSON format: Returns JSON Schema compatible with the stored procedure parameters

Examples:
- get_stored_procedure_parameters(""sp_GetCustomerOrders"")
- get_stored_procedure_parameters(""dbo.sp_UpdateCustomer"", ""table"")
- get_stored_procedure_parameters(""sp_ProcessPayment"", ""json"")

Table format shows parameter name, SQL data type, whether it's required or optional, direction (INPUT/OUTPUT), and default values.
JSON format provides JSON Schema with SQL-specific extensions for parameter validation and documentation.

Note: This tool works within the current database context. For cross-database queries, use the server mode.")]
        public async Task<string> GetStoredProcedureParameters(
            string procedureName, 
            string format = "table",
            int? timeoutSeconds = null)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
            {
                return "Error: Procedure name cannot be empty";
            }

            try
            {
                // Parse schema and procedure name
                string schemaName = "dbo";
                string procNameOnly = procedureName;
                
                if (procedureName.Contains("."))
                {
                    var parts = procedureName.Split(new[] {'.'}, 2);
                    schemaName = parts[0].Trim(new[] {'[', ']'});
                    procNameOnly = parts[1].Trim(new[] {'[', ']'});
                }

                var parameters = await GetStoredProcedureParametersAsync(schemaName, procNameOnly, timeoutSeconds);
                
                if (!parameters.Any())
                {
                    return $"Stored procedure '{procedureName}' has no parameters or does not exist in the current database.";
                }

                return format.ToLower() switch
                {
                    "json" => FormatParameterInformationAsJson(procedureName, parameters),
                    "table" => FormatParameterInformation(procedureName, parameters),
                    _ => $"Error: Unsupported format '{format}'. Use 'table' or 'json'."
                };
            }
            catch (Exception ex)
            {
                return ex.ToSqlErrorResult("getting stored procedure parameters");
            }
        }

        /// <summary>
        /// Gets stored procedure parameter metadata from the current database.
        /// </summary>
        private async Task<List<ParameterInfo>> GetStoredProcedureParametersAsync(
            string schemaName, 
            string procedureName,
            int? timeoutSeconds = null)
        {
            var query = $@"
                SELECT 
                    p.name AS ParameterName,
                    p.parameter_id AS ParameterId,
                    t.name AS DataType,
                    p.max_length AS MaxLength,
                    p.precision AS Precision,
                    p.scale AS Scale,
                    p.is_output AS IsOutput,
                    p.has_default_value AS HasDefaultValue,
                    p.default_value AS DefaultValue
                FROM sys.parameters p
                JOIN sys.types t ON p.user_type_id = t.user_type_id
                JOIN sys.procedures sp ON p.object_id = sp.object_id
                JOIN sys.schemas s ON sp.schema_id = s.schema_id
                WHERE s.name = '{schemaName}' AND sp.name = '{procedureName}'
                ORDER BY p.parameter_id";

            var parameters = new List<ParameterInfo>();
            
            // Create timeout context
            var (timeoutContext, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(_configuration);
            
            IAsyncDataReader reader;
            try
            {
                reader = await _databaseContext.ExecuteQueryAsync(query, timeoutContext, timeoutSeconds);
            }
            catch (OperationCanceledException ex) when (timeoutContext?.IsTimeoutExceeded == true)
            {
                tokenSource?.Dispose();
                throw new InvalidOperationException(timeoutContext.CreateTimeoutExceededMessage(), ex);
            }
            catch (SqlException ex) when (timeoutContext?.IsTimeoutExceeded == true && SqlExceptionHelper.IsTimeoutError(ex))
            {
                // SQL Server throws SqlException when cancelled - show custom timeout message
                tokenSource?.Dispose();
                throw new InvalidOperationException(timeoutContext.CreateTimeoutExceededMessage(), ex);
            }
            catch
            {
                tokenSource?.Dispose();
                throw;
            }

            try
            {
                while (await reader.ReadAsync())
                {
                    var parameterName = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("ParameterName")) ?? "";
                    var parameterId = await reader.GetFieldValueAsync<int>(reader.GetOrdinal("ParameterId"));
                    
                    // Skip return value parameter (usually has empty name and parameterId = 0)
                    if (string.IsNullOrEmpty(parameterName) || parameterId == 0)
                        continue;

                    var dataType = await reader.GetFieldValueAsync<string>(reader.GetOrdinal("DataType")) ?? "";
                    var maxLength = await reader.GetFieldValueAsync<short>(reader.GetOrdinal("MaxLength"));
                    var precision = await reader.GetFieldValueAsync<byte>(reader.GetOrdinal("Precision"));
                    var scale = await reader.GetFieldValueAsync<byte>(reader.GetOrdinal("Scale"));
                    var isOutput = await reader.GetFieldValueAsync<bool>(reader.GetOrdinal("IsOutput"));
                    var hasDefaultValue = await reader.GetFieldValueAsync<bool>(reader.GetOrdinal("HasDefaultValue"));
                    var defaultValue = await reader.IsDBNullAsync(reader.GetOrdinal("DefaultValue")) ? 
                        null : await reader.GetFieldValueAsync<object>(reader.GetOrdinal("DefaultValue"));

                    parameters.Add(new ParameterInfo
                    {
                        ParameterName = parameterName,
                        ParameterId = parameterId,
                        DataType = dataType,
                        MaxLength = maxLength,
                        Precision = precision,
                        Scale = scale,
                        IsOutput = isOutput,
                        HasDefaultValue = hasDefaultValue,
                        DefaultValue = defaultValue,
                        DisplayType = GetDisplayType(dataType, maxLength, precision, scale),
                        RequirementDescription = GetRequirementDescription(isOutput, hasDefaultValue),
                        ExampleValue = GetExampleValue(dataType)
                    });
                }
            }
            finally
            {
                reader.Dispose();
                tokenSource?.Dispose();
            }

            return parameters;
        }

        /// <summary>
        /// Formats the parameter information as JSON Schema format with SQL-specific extensions.
        /// </summary>
        private string FormatParameterInformationAsJson(
            string procedureName, 
            List<ParameterInfo> parameters)
        {
            var inputParams = parameters.Where(p => !p.IsOutput).ToList();
            var outputParams = parameters.Where(p => p.IsOutput).ToList();

            var schema = new
            {
                procedureName = procedureName,
                description = $"Parameter schema for stored procedure {procedureName}",
                parameters = new
                {
                    type = "object",
                    properties = inputParams.ToDictionary(
                        p => p.ParameterName.TrimStart('@'),
                        p => GetJsonSchemaForParameter(p)
                    ),
                    required = inputParams.Where(p => !p.HasDefaultValue).Select(p => p.ParameterName.TrimStart('@')).ToArray(),
                    additionalProperties = false
                },
                returnValue = new
                {
                    type = "integer",
                    sqlType = "int",
                    description = "Return code (0 for success)"
                },
                outputParameters = outputParams.Count > 0 ? outputParams.Select(p => new
                {
                    name = p.ParameterName.TrimStart('@'),
                    type = GetJsonTypeForSqlType(p.DataType),
                    sqlType = p.DisplayType,
                    description = $"Output parameter {p.ParameterName}"
                }).ToArray() : null
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(schema, options);
        }

        /// <summary>
        /// Formats the parameter information into a readable table format.
        /// </summary>
        private string FormatParameterInformation(
            string procedureName, 
            List<ParameterInfo> parameters)
        {
            var result = new StringBuilder();
            
            result.AppendLine($"Parameters for stored procedure: {procedureName}");
            result.AppendLine($"Database: {_databaseContext.GetType().Name} (current database context)");
            result.AppendLine();
            
            // Create formatted table
            result.AppendLine("| Parameter | Type | Required | Direction | Default |");
            result.AppendLine("|-----------|------|----------|-----------|---------|");
            
            foreach (var param in parameters)
            {
                var paramName = param.ParameterName.TrimStart('@');
                var defaultVal = param.HasDefaultValue ? 
                    (param.DefaultValue?.ToString() ?? "NULL") : "-";
                
                result.AppendLine($"| {paramName} | {param.DisplayType} | {param.RequirementDescription} | {param.Direction} | {defaultVal} |");
            }
            
            result.AppendLine();
            
            // Add example usage
            var inputParams = parameters.Where(p => !p.IsOutput).ToList();
            
            if (inputParams.Any())
            {
                result.AppendLine("Example usage:");
                result.AppendLine("```json");
                result.AppendLine("{");
                
                var exampleParams = inputParams.Select(p => 
                    $"  \"{p.ParameterName.TrimStart('@')}\": {p.ExampleValue}");
                
                result.AppendLine(string.Join(",\n", exampleParams));
                result.AppendLine("}");
                result.AppendLine("```");
            }
            else
            {
                result.AppendLine("This stored procedure has no input parameters.");
            }

            // Add output parameter information if any
            var outputParams = parameters.Where(p => p.IsOutput).ToList();
            if (outputParams.Any())
            {
                result.AppendLine();
                result.AppendLine("Output parameters:");
                foreach (var param in outputParams)
                {
                    result.AppendLine($"- {param.ParameterName} ({param.DisplayType})");
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets a JSON Schema object for a parameter including SQL-specific extensions.
        /// </summary>
        private object GetJsonSchemaForParameter(ParameterInfo parameter)
        {
            var baseSchema = new Dictionary<string, object>
            {
                ["type"] = GetJsonTypeForSqlType(parameter.DataType),
                ["sqlType"] = parameter.DisplayType,
                ["sqlParameter"] = parameter.ParameterName,
                ["position"] = parameter.ParameterId,
                ["isOutput"] = parameter.IsOutput,
                ["description"] = $"Parameter {parameter.ParameterName} of type {parameter.DisplayType}"
            };

            // Add constraints based on SQL type
            AddJsonSchemaConstraints(baseSchema, parameter);

            // Add default value if present
            if (parameter.HasDefaultValue)
            {
                baseSchema["hasDefault"] = true;
                if (parameter.DefaultValue != null)
                {
                    baseSchema["defaultValue"] = ConvertDefaultValueForJson(parameter.DefaultValue, parameter.DataType);
                }
            }

            return baseSchema;
        }

        /// <summary>
        /// Gets the JSON Schema type for a SQL data type.
        /// </summary>
        private string GetJsonTypeForSqlType(string sqlType)
        {
            return sqlType.ToLower() switch
            {
                "int" or "bigint" or "smallint" or "tinyint" => "integer",
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "number",
                "bit" => "boolean",
                "datetime" or "datetime2" or "date" or "time" or "datetimeoffset" => "string",
                "uniqueidentifier" => "string",
                "nvarchar" or "varchar" or "char" or "nchar" or "text" or "ntext" => "string",
                "varbinary" or "binary" or "image" => "string",
                "xml" => "string",
                _ => "string"
            };
        }

        /// <summary>
        /// Adds JSON Schema constraints based on the SQL parameter metadata.
        /// </summary>
        private void AddJsonSchemaConstraints(Dictionary<string, object> schema, ParameterInfo parameter)
        {
            var sqlType = parameter.DataType.ToLower();

            switch (sqlType)
            {
                case "int":
                    schema["minimum"] = -2147483648;
                    schema["maximum"] = 2147483647;
                    break;
                case "smallint":
                    schema["minimum"] = -32768;
                    schema["maximum"] = 32767;
                    break;
                case "tinyint":
                    schema["minimum"] = 0;
                    schema["maximum"] = 255;
                    break;
                case "bigint":
                    schema["minimum"] = -9223372036854775808L;
                    schema["maximum"] = 9223372036854775807L;
                    break;
                case "varchar" or "char":
                    if (parameter.MaxLength > 0)
                        schema["maxLength"] = parameter.MaxLength;
                    break;
                case "nvarchar" or "nchar":
                    if (parameter.MaxLength > 0)
                        schema["maxLength"] = parameter.MaxLength / 2; // Unicode characters
                    break;
                case "decimal" or "numeric":
                    if (parameter.Scale > 0)
                    {
                        var divisor = Math.Pow(10, parameter.Scale);
                        schema["multipleOf"] = 1.0 / divisor;
                    }
                    break;
                case "datetime" or "datetime2" or "datetimeoffset":
                    schema["format"] = "date-time";
                    break;
                case "date":
                    schema["format"] = "date";
                    break;
                case "time":
                    schema["format"] = "time";
                    break;
                case "uniqueidentifier":
                    schema["format"] = "uuid";
                    break;
                case "varbinary" or "binary" or "image":
                    schema["contentEncoding"] = "base64";
                    break;
            }
        }

        /// <summary>
        /// Converts a default value to appropriate JSON representation.
        /// </summary>
        private object? ConvertDefaultValueForJson(object defaultValue, string sqlType)
        {
            if (defaultValue == null || defaultValue == DBNull.Value)
                return null;

            var sqlTypeLower = sqlType.ToLower();
            var defaultStr = defaultValue.ToString();

            return sqlTypeLower switch
            {
                "int" or "bigint" or "smallint" or "tinyint" => 
                    int.TryParse(defaultStr, out var intVal) ? intVal : defaultStr,
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => 
                    decimal.TryParse(defaultStr, out var decVal) ? decVal : defaultStr,
                "bit" => 
                    bool.TryParse(defaultStr, out var boolVal) ? boolVal : (defaultStr == "1"),
                _ => defaultStr
            };
        }

        /// <summary>
        /// Gets a display-friendly representation of the parameter type.
        /// </summary>
        private string GetDisplayType(string dataType, short maxLength, byte precision, byte scale)
        {
            var baseType = dataType.ToLower();
            
            return baseType switch
            {
                "varchar" or "char" or "varbinary" or "binary" => 
                    maxLength == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLength})",
                "nvarchar" or "nchar" => 
                    maxLength == -1 ? $"{dataType}(MAX)" : $"{dataType}({maxLength / 2})",
                "decimal" or "numeric" => 
                    scale > 0 ? $"{dataType}({precision},{scale})" : $"{dataType}({precision})",
                "float" => 
                    precision > 0 ? $"{dataType}({precision})" : dataType,
                _ => dataType
            };
        }

        /// <summary>
        /// Gets a description of whether the parameter is required or optional.
        /// </summary>
        private string GetRequirementDescription(bool isOutput, bool hasDefaultValue)
        {
            if (isOutput) return "No";
            if (hasDefaultValue) return "No";
            return "Yes";
        }

        /// <summary>
        /// Gets an example value for the given SQL data type.
        /// </summary>
        private string GetExampleValue(string dataType)
        {
            return dataType.ToLower() switch
            {
                "int" or "bigint" or "smallint" or "tinyint" => "123",
                "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "99.99",
                "bit" => "true",
                "datetime" or "datetime2" or "date" => "\"2024-01-01\"",
                "time" => "\"14:30:00\"",
                "datetimeoffset" => "\"2024-01-01T14:30:00+00:00\"",
                "uniqueidentifier" => "\"550e8400-e29b-41d4-a716-446655440000\"",
                "nvarchar" or "varchar" or "char" or "nchar" or "text" or "ntext" => "\"example text\"",
                "varbinary" or "binary" or "image" => "\"base64encodeddata\"",
                "xml" => "\"<root>example</root>\"",
                _ => "null"
            };
        }

        /// <summary>
        /// Internal class to hold parameter information.
        /// </summary>
        private class ParameterInfo
        {
            public string ParameterName { get; set; } = "";
            public int ParameterId { get; set; }
            public string DataType { get; set; } = "";
            public short MaxLength { get; set; }
            public byte Precision { get; set; }
            public byte Scale { get; set; }
            public bool IsOutput { get; set; }
            public bool HasDefaultValue { get; set; }
            public object? DefaultValue { get; set; }
            public string DisplayType { get; set; } = "";
            public string RequirementDescription { get; set; } = "";
            public string ExampleValue { get; set; } = "";
            
            public string Direction => IsOutput ? "OUTPUT" : "INPUT";
        }
    }
}