using System.Runtime.InteropServices;

namespace IntegrationTests.Helpers
{
    public static class EnvironmentVariableHelper
    {
        /// <summary>
        /// Creates environment variables required for MCP server
        /// </summary>
        public static Dictionary<string, string> CreateEnvironmentVariables(
            string? connectionString = null,
            string modelName = "sqlclient-model",
            string apiVersion = "1.0",
            string? apiKey = null,
            string? baseUrl = null,
            Dictionary<string, string>? additionalVariables = null)
        {
            var variables = new Dictionary<string, string>
            {
                ["MSSQL_CONNECTIONSTRING"] = connectionString ?? GetDefaultConnectionString()
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                variables["MCP_API_KEY"] = apiKey;
            }

            if (!string.IsNullOrEmpty(baseUrl))
            {
                variables["MCP_BASE_URL"] = baseUrl;
            }

            if (additionalVariables != null)
            {
                foreach (var kvp in additionalVariables)
                {
                    variables[kvp.Key] = kvp.Value;
                }
            }

            return variables;
        }
        
        /// <summary>
        /// Gets the default connection string for SQL Server tests
        /// </summary>
        /// <param name="port">The SQL Server port to connect to</param>
        /// <returns>Default connection string for SQL Server</returns>
        public static string GetDefaultConnectionString(int port = 14330)
        {
            return $"Server=localhost,{port};Database=master;User Id=sa;Password=IntegrationTest!123;TrustServerCertificate=true;";
        }

        /// <summary>
        /// Gets a connection string without a Database parameter, which triggers server mode
        /// </summary>
        /// <param name="port">The SQL Server port to connect to</param>
        /// <returns>Server-mode connection string for SQL Server</returns>
        public static string GetServerModeConnectionString(int port = 14330)
        {
            return $"Server=localhost,{port};User Id=sa;Password=IntegrationTest!123;TrustServerCertificate=true;";
        }
        
        /// <summary>
        /// Merges provided variables with system environment variables 
        /// </summary>
        public static Dictionary<string, string> MergeWithSystemEnvironment(
            Dictionary<string, string> variables)
        {
            var result = new Dictionary<string, string>();
            
            // First add system environment variables
            foreach (var key in Environment.GetEnvironmentVariables().Keys)
            {
                if (key is string strKey)
                {
                    result[strKey] = Environment.GetEnvironmentVariable(strKey) ?? string.Empty;
                }
            }
            
            // Then override with provided variables
            foreach (var kvp in variables)
            {
                result[kvp.Key] = kvp.Value;
            }
            
            return result;
        }
    }
}