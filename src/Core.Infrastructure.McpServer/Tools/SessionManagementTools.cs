using Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// MCP tools for managing query and stored procedure sessions.
    /// </summary>
    [McpServerToolType]
    public class SessionManagementTools
    {
        private readonly IQuerySessionManager _sessionManager;
        private readonly ILogger<SessionManagementTools> _logger;

        public SessionManagementTools(
            IQuerySessionManager sessionManager,
            ILogger<SessionManagementTools> logger)
        {
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [McpServerTool(Name = "get_session_status"), Description("Check the status of a running query or stored procedure session")]
        public string GetSessionStatus(
            [Description("The session ID to check")]
            int sessionId)
        {
            try
            {
                var session = _sessionManager.GetSession(sessionId);
                
                if (session == null)
                {
                    var notFoundResult = new
                    {
                        error = $"Session {sessionId} not found",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    };

                    return JsonSerializer.Serialize(notFoundResult, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true 
                    });
                }

                var duration = (session.EndTime ?? DateTime.UtcNow) - session.StartTime;
                var status = session.IsRunning ? "running" : (session.Error != null ? "failed" : "completed");

                var result = new
                {
                    sessionId = session.SessionId,
                    type = session.Type.ToString().ToLowerInvariant(),
                    query = session.Query,
                    databaseName = session.DatabaseName ?? "default",
                    startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    endTime = session.EndTime?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    duration = $"{duration.TotalSeconds:F1} seconds",
                    status = status,
                    isRunning = session.IsRunning,
                    rowCount = session.RowCount,
                    error = session.Error,
                    timeoutSeconds = session.TimeoutSeconds
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session status for session {SessionId}", sessionId);
                
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

        [McpServerTool(Name = "get_session_results"), Description("Get results from a query or stored procedure session")]
        public string GetSessionResults(
            [Description("The session ID to get results from")]
            int sessionId,
            [Description("Maximum number of rows to return. Default is all rows")]
            int? maxRows = null)
        {
            try
            {
                var session = _sessionManager.GetSession(sessionId);
                
                if (session == null)
                {
                    var notFoundResult = new
                    {
                        error = $"Session {sessionId} not found",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    };

                    return JsonSerializer.Serialize(notFoundResult, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true 
                    });
                }

                var resultsText = session.Results.ToString();
                var lines = resultsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                if (maxRows.HasValue && maxRows.Value > 0 && lines.Length > maxRows.Value + 1) // +1 for header
                {
                    var limitedLines = lines.Take(maxRows.Value + 1).ToArray();
                    resultsText = string.Join('\n', limitedLines);
                    resultsText += $"\n... (showing first {maxRows.Value} rows of {session.RowCount} total)";
                }

                var duration = (session.EndTime ?? DateTime.UtcNow) - session.StartTime;
                var status = session.IsRunning ? "running" : (session.Error != null ? "failed" : "completed");

                var result = new
                {
                    sessionId = session.SessionId,
                    type = session.Type.ToString().ToLowerInvariant(),
                    query = session.Query,
                    databaseName = session.DatabaseName ?? "default",
                    startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    endTime = session.EndTime?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                    duration = $"{duration.TotalSeconds:F1} seconds",
                    status = status,
                    isRunning = session.IsRunning,
                    rowCount = session.RowCount,
                    error = session.Error,
                    results = resultsText,
                    maxRowsApplied = maxRows
                };

                return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session results for session {SessionId}", sessionId);
                
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

        [McpServerTool(Name = "stop_session"), Description("Stop a running query or stored procedure session")]
        public string StopSession(
            [Description("The session ID to stop")]
            int sessionId)
        {
            try
            {
                var success = _sessionManager.CancelSession(sessionId);
                
                if (success)
                {
                    var result = new
                    {
                        sessionId = sessionId,
                        status = "cancelled",
                        message = "Session cancelled successfully",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    };

                    return JsonSerializer.Serialize(result, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true 
                    });
                }
                else
                {
                    var notFoundResult = new
                    {
                        error = $"Session {sessionId} not found or already completed",
                        timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                    };

                    return JsonSerializer.Serialize(notFoundResult, new JsonSerializerOptions 
                    { 
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = true 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop session {SessionId}", sessionId);
                
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

        [McpServerTool(Name = "list_sessions"), Description("List all query and stored procedure sessions")]
        public string ListSessions(
            [Description("Filter by status: 'all', 'running', or 'completed'")]
            string status = "all")
        {
            try
            {
                bool includeCompleted = status.Equals("all", StringComparison.OrdinalIgnoreCase) || 
                                      status.Equals("completed", StringComparison.OrdinalIgnoreCase);
                
                var sessions = _sessionManager.GetAllSessions(includeCompleted);
                
                if (status.Equals("running", StringComparison.OrdinalIgnoreCase))
                {
                    sessions = sessions.Where(s => s.IsRunning);
                }
                else if (status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                {
                    sessions = sessions.Where(s => !s.IsRunning);
                }

                var sessionList = sessions.Select(session =>
                {
                    var duration = (session.EndTime ?? DateTime.UtcNow) - session.StartTime;
                    var sessionStatus = session.IsRunning ? "running" : (session.Error != null ? "failed" : "completed");
                    
                    return new
                    {
                        sessionId = session.SessionId,
                        type = session.Type.ToString().ToLowerInvariant(),
                        query = session.Query.Length > 50 ? session.Query.Substring(0, 50) + "..." : session.Query,
                        databaseName = session.DatabaseName ?? "default",
                        startTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        endTime = session.EndTime?.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                        duration = $"{duration.TotalSeconds:F1} seconds",
                        status = sessionStatus,
                        isRunning = session.IsRunning,
                        rowCount = session.RowCount,
                        hasError = !string.IsNullOrEmpty(session.Error)
                    };
                }).ToList();

                var result = new
                {
                    filter = status,
                    totalSessions = sessionList.Count,
                    sessions = sessionList,
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
                _logger.LogError(ex, "Failed to list sessions");
                
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