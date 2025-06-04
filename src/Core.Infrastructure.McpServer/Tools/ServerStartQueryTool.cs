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
    /// Server-mode MCP tool to start a SQL query in the background for a specific database and return a session ID for monitoring.
    /// </summary>
    [McpServerToolType]
    public class ServerStartQueryTool
    {
        private readonly IQuerySessionManager _sessionManager;
        private readonly ILogger<ServerStartQueryTool> _logger;
        private readonly DatabaseConfiguration _configuration;

        public ServerStartQueryTool(
            IQuerySessionManager sessionManager,
            ILogger<ServerStartQueryTool> logger,
            IOptions<DatabaseConfiguration> configuration)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }

        [McpServerTool(Name = "start_query_in_database"), Description("Start a SQL query in the background for a specific database. Returns a session ID to check progress. Best for long-running queries (server mode).")]
        public async Task<string> StartQueryInDatabase(
            [Description("The name of the database to execute the query in")]
            string databaseName,
            [Description("The SQL query to execute")]
            string query,
            [Description("Optional timeout in seconds. If not specified, uses the default timeout")]
            int? timeoutSeconds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    throw new ArgumentException("Database name cannot be empty", nameof(databaseName));
                }

                if (string.IsNullOrWhiteSpace(query))
                {
                    throw new ArgumentException("Query cannot be empty", nameof(query));
                }

                var effectiveTimeout = timeoutSeconds ?? _configuration.DefaultCommandTimeoutSeconds;
                
                _logger.LogInformation("Starting query session for database: {DatabaseName}, timeout: {TimeoutSeconds}s", 
                    databaseName, effectiveTimeout);

                var session = await _sessionManager.StartQueryAsync(query, databaseName, effectiveTimeout);

                var result = new
                {
                    sessionId = session.SessionId,
                    startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    query = session.Query,
                    databaseName = session.DatabaseName,
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
                _logger.LogError(ex, "Failed to start query session for database: {DatabaseName}", databaseName);
                
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