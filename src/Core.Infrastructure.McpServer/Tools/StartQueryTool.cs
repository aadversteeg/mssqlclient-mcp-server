using System.ComponentModel;
using System.Text.Json;
using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Database-mode MCP tool to start a SQL query in the background and return a session ID for monitoring.
    /// </summary>
    [McpServerToolType]
    public class StartQueryTool
    {
        private readonly IQuerySessionManager _sessionManager;
        private readonly ILogger<StartQueryTool> _logger;
        private readonly DatabaseConfiguration _configuration;

        public StartQueryTool(
            IQuerySessionManager sessionManager,
            ILogger<StartQueryTool> logger,
            IOptions<DatabaseConfiguration> configuration)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        [McpServerTool(Name = "start_query"), Description("Start a SQL query in the background on the connected database. Returns a session ID to check progress. Best for long-running queries.")]
        public async Task<string> StartQuery(
            [Description("The SQL query to execute")]
            string query,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    throw new ArgumentException("Query cannot be empty", nameof(query));
                }

                var effectiveTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                
                _logger.LogInformation("Starting query session for connected database, timeout: {TimeoutSeconds}s", 
                    effectiveTimeout);

                var session = await _sessionManager.StartQueryAsync(query, null, effectiveTimeout);

                var result = new
                {
                    sessionId = session.SessionId,
                    startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    query = session.Query,
                    databaseName = session.DatabaseName ?? "connected database",
                    timeoutSeconds = session.TimeoutSeconds,
                    status = "running",
                    message = "Query started successfully. Use get_session_status to check progress."
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start query session");
                
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