using System.ComponentModel;
using System.Text.Json;
using Core.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// MCP tools for managing command timeout settings.
    /// </summary>
    public class TimeoutManagementTools
    {
        private readonly ILogger<TimeoutManagementTools> _logger;
        private readonly DatabaseConfiguration _configuration;

        public TimeoutManagementTools(
            ILogger<TimeoutManagementTools> logger,
            DatabaseConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [McpServerTool(Name = "get_command_timeout"), Description("Gets the current default command timeout setting")]
        public string GetCommandTimeout()
        {
            try
            {
                var result = new
                {
                    defaultCommandTimeoutSeconds = _configuration.DefaultCommandTimeoutSeconds,
                    connectionTimeoutSeconds = _configuration.ConnectionTimeoutSeconds,
                    maxConcurrentSessions = _configuration.MaxConcurrentSessions,
                    sessionCleanupIntervalMinutes = _configuration.SessionCleanupIntervalMinutes,
                    totalToolCallTimeoutSeconds = _configuration.TotalToolCallTimeoutSeconds,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get command timeout");
                
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

        [McpServerTool(Name = "set_command_timeout"), Description("Sets the default command timeout for all subsequent SQL operations. Note: When TotalToolCallTimeoutSeconds is configured, the effective timeout will be the minimum of this value and the remaining total timeout. This only affects new operations, not existing sessions.")]
        public string SetCommandTimeout(
            [Description("Timeout in seconds")]
            int timeoutSeconds)
        {
            try
            {
                if (timeoutSeconds < 1 || timeoutSeconds > 3600) // Max 1 hour
                {
                    throw new ArgumentException("Timeout must be between 1 and 3600 seconds", nameof(timeoutSeconds));
                }

                var oldTimeout = _configuration.DefaultCommandTimeoutSeconds;
                _configuration.DefaultCommandTimeoutSeconds = timeoutSeconds;

                var result = new
                {
                    message = "Default command timeout updated successfully",
                    oldTimeoutSeconds = oldTimeout,
                    newTimeoutSeconds = timeoutSeconds,
                    note = "This change only affects new operations. Existing sessions will continue with their original timeout settings.",
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                _logger.LogInformation("Default command timeout changed from {OldTimeout}s to {NewTimeout}s", oldTimeout, timeoutSeconds);

                return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set command timeout to {TimeoutSeconds}s", timeoutSeconds);
                
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