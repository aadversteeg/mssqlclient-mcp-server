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
    /// MCP tool to start a stored procedure execution in the background and return a session ID for monitoring.
    /// </summary>
    [McpServerToolType]
    public class StartStoredProcedureTool
    {
        private readonly IQuerySessionManager _sessionManager;
        private readonly ILogger<StartStoredProcedureTool> _logger;
        private readonly DatabaseConfiguration _configuration;

        public StartStoredProcedureTool(
            IQuerySessionManager sessionManager,
            ILogger<StartStoredProcedureTool> logger,
            IOptions<DatabaseConfiguration> configuration)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        [McpServerTool(Name = "start_stored_procedure"), Description("Start a stored procedure execution in the background. Returns a session ID to check progress. Best for long-running procedures.")]
        public async Task<string> StartStoredProcedure(
            [Description("The name of the stored procedure to execute")]
            string procedureName,
            [Description("JSON object containing the parameters for the stored procedure (e.g., {\"param1\": \"value1\", \"param2\": 123})")]
            string parameters = "{}",
            [Description("Optional database name to execute the procedure in")]
            string? databaseName = null,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(procedureName))
                {
                    throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
                }

                var effectiveTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                
                // Parse parameters from JSON
                Dictionary<string, object?> parameterDict;
                try
                {
                    parameterDict = JsonParameterConverter.ParseParametersFromJson(parameters);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Invalid parameters JSON: {ex.Message}", nameof(parameters));
                }
                
                _logger.LogInformation("Starting stored procedure session for procedure: {ProcedureName} in connected database, timeout: {TimeoutSeconds}s", 
                    procedureName, effectiveTimeout);

                var session = await _sessionManager.StartStoredProcedureAsync(procedureName, parameterDict, null, effectiveTimeout);

                var result = new
                {
                    sessionId = session.SessionId,
                    startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    procedureName = session.Query,
                    databaseName = session.DatabaseName ?? "connected database",
                    parameters = session.Parameters,
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
                _logger.LogError(ex, "Failed to start stored procedure session");
                
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