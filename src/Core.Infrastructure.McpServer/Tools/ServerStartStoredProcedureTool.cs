using System.ComponentModel;
using System.Text.Json;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.SqlClient.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Server-mode MCP tool to start a stored procedure execution in the background for a specific database and return a session ID for monitoring.
    /// </summary>
    [McpServerToolType]
    public class ServerStartStoredProcedureTool
    {
        private readonly IQuerySessionManager _sessionManager;
        private readonly ILogger<ServerStartStoredProcedureTool> _logger;
        private readonly DatabaseConfiguration _configuration;

        public ServerStartStoredProcedureTool(
            IQuerySessionManager sessionManager,
            ILogger<ServerStartStoredProcedureTool> logger,
            IOptions<DatabaseConfiguration> configuration)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        [McpServerTool(Name = "start_stored_procedure_in_database"), Description("Start a stored procedure execution in the background for a specific database. Returns a session ID to check progress. Best for long-running procedures (server mode).")]
        public async Task<string> StartStoredProcedureInDatabase(
            [Description("The name of the database to execute the procedure in")]
            string databaseName,
            [Description("The name of the stored procedure to execute")]
            string procedureName,
            [Description("JSON object containing the parameters for the stored procedure (e.g., {\"param1\": \"value1\", \"param2\": 123})")]
            string parameters = "{}",
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
                }

                if (string.IsNullOrWhiteSpace(procedureName))
                {
                    throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
                }

                var effectiveTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                
                // Parse parameters from JSON
                Dictionary<string, object?> parsedParameters;
                try
                {
                    parsedParameters = JsonParameterConverter.ParseParametersFromJson(parameters);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error parsing parameters: {ex.Message}", nameof(parameters));
                }
                
                _logger.LogInformation("Starting stored procedure session for database: {DatabaseName}, procedure: {ProcedureName}, timeout: {TimeoutSeconds}s", 
                    databaseName, procedureName, effectiveTimeout);

                var session = await _sessionManager.StartStoredProcedureAsync(procedureName, parsedParameters, databaseName, effectiveTimeout);

                var result = new
                {
                    sessionId = session.SessionId,
                    startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    procedureName = session.Query,
                    databaseName = session.DatabaseName,
                    parameters = parsedParameters,
                    timeoutSeconds = session.TimeoutSeconds,
                    status = "running",
                    message = "Stored procedure started successfully. Use get_session_status to check progress."
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start stored procedure session for database: {DatabaseName}, procedure: {ProcedureName}", 
                    databaseName, procedureName);
                
                var errorResult = new
                {
                    error = ex.Message,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
        }
    }
}