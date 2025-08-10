using System.Collections.Concurrent;
using System.Text;
using Core.Application.Interfaces;
using Core.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Application
{
    /// <summary>
    /// Manages query and stored procedure sessions for background execution.
    /// </summary>
    public class QuerySessionManager : IQuerySessionManager
    {
        private readonly ConcurrentDictionary<int, QuerySession> _sessions;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<QuerySessionManager> _logger;
        private readonly DatabaseConfiguration _configuration;
        private int _nextSessionId = 1;
        
        public QuerySessionManager(
            IDatabaseService databaseService, 
            ILogger<QuerySessionManager> logger,
            IOptions<DatabaseConfiguration> configuration)
        {
            _sessions = new ConcurrentDictionary<int, QuerySession>();
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        /// <summary>
        /// Starts a new query execution session in the background.
        /// </summary>
        public Task<QuerySession> StartQueryAsync(string query, string? databaseName, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query cannot be empty", nameof(query));
            
            // Check if we've reached the maximum number of concurrent sessions
            var runningSessions = _sessions.Values.Count(s => s.IsRunning);
            if (runningSessions >= _configuration.MaxConcurrentSessions)
            {
                throw new InvalidOperationException($"Maximum number of concurrent sessions ({_configuration.MaxConcurrentSessions}) reached");
            }
            
            var sessionId = Interlocked.Increment(ref _nextSessionId);
            var session = new QuerySession
            {
                SessionId = sessionId,
                Query = query,
                Type = QuerySessionType.Query,
                StartTime = DateTime.UtcNow,
                IsRunning = true,
                DatabaseName = databaseName,
                TimeoutSeconds = timeoutSeconds,
                CancellationToken = new CancellationTokenSource()
            };
            
            _sessions.TryAdd(sessionId, session);
            
            // Start the query execution in the background
            _ = Task.Run(async () => await ExecuteQueryInBackground(session), cancellationToken);
            
            _logger.LogInformation("Started query session {SessionId} for database {DatabaseName}", sessionId, databaseName ?? "default");
            
            return Task.FromResult(session);
        }
        
        /// <summary>
        /// Starts a new stored procedure execution session in the background.
        /// </summary>
        public Task<QuerySession> StartStoredProcedureAsync(string procedureName, Dictionary<string, object?>? parameters, string? databaseName, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(procedureName))
                throw new ArgumentException("Procedure name cannot be empty", nameof(procedureName));
            
            // Check if we've reached the maximum number of concurrent sessions
            var runningSessions = _sessions.Values.Count(s => s.IsRunning);
            if (runningSessions >= _configuration.MaxConcurrentSessions)
            {
                throw new InvalidOperationException($"Maximum number of concurrent sessions ({_configuration.MaxConcurrentSessions}) reached");
            }
            
            var sessionId = Interlocked.Increment(ref _nextSessionId);
            var session = new QuerySession
            {
                SessionId = sessionId,
                Query = procedureName,
                Type = QuerySessionType.StoredProcedure,
                StartTime = DateTime.UtcNow,
                IsRunning = true,
                DatabaseName = databaseName,
                Parameters = parameters,
                TimeoutSeconds = timeoutSeconds,
                CancellationToken = new CancellationTokenSource()
            };
            
            _sessions.TryAdd(sessionId, session);
            
            // Start the stored procedure execution in the background
            _ = Task.Run(async () => await ExecuteStoredProcedureInBackground(session), cancellationToken);
            
            _logger.LogInformation("Started stored procedure session {SessionId} for procedure {ProcedureName} in database {DatabaseName}", 
                sessionId, procedureName, databaseName ?? "default");
            
            return Task.FromResult(session);
        }
        
        /// <summary>
        /// Gets a session by its ID.
        /// </summary>
        public QuerySession? GetSession(int sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }
        
        /// <summary>
        /// Cancels a running session.
        /// </summary>
        public bool CancelSession(int sessionId)
        {
            if (_sessions.TryGetValue(sessionId, out var session) && session.IsRunning)
            {
                try
                {
                    session.CancellationToken?.Cancel();
                    session.IsRunning = false;
                    session.EndTime = DateTime.UtcNow;
                    session.Error = "Session was cancelled by user";
                    
                    _logger.LogInformation("Cancelled session {SessionId}", sessionId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cancelling session {SessionId}", sessionId);
                    return false;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets all sessions, optionally including completed ones.
        /// </summary>
        public IEnumerable<QuerySession> GetAllSessions(bool includeCompleted = false)
        {
            if (includeCompleted)
            {
                return _sessions.Values.OrderByDescending(s => s.StartTime);
            }
            
            return _sessions.Values.Where(s => s.IsRunning).OrderByDescending(s => s.StartTime);
        }
        
        /// <summary>
        /// Cleans up completed sessions that are older than the cleanup interval.
        /// </summary>
        public async Task CleanupCompletedSessions()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_configuration.SessionCleanupIntervalMinutes);
            var sessionsToRemove = _sessions.Values
                .Where(s => !s.IsRunning && s.EndTime.HasValue && s.EndTime.Value < cutoffTime)
                .ToList();
            
            foreach (var session in sessionsToRemove)
            {
                _sessions.TryRemove(session.SessionId, out _);
                session.CancellationToken?.Dispose();
            }
            
            if (sessionsToRemove.Any())
            {
                _logger.LogInformation("Cleaned up {Count} completed sessions", sessionsToRemove.Count);
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// Executes a query in the background and updates the session with results.
        /// </summary>
        private async Task ExecuteQueryInBackground(QuerySession session)
        {
            try
            {
                _logger.LogDebug("Executing query for session {SessionId}", session.SessionId);
                
                using var reader = await _databaseService.ExecuteQueryAsync(
                    session.Query, 
                    session.DatabaseName, 
                    null,
                    session.TimeoutSeconds,
                    session.CancellationToken?.Token ?? CancellationToken.None);
                
                var results = new StringBuilder();
                var rowCount = 0L;
                
                // Write column headers
                var columnNames = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }
                results.AppendLine(string.Join("\t", columnNames));
                
                // Read all rows
                while (await reader.ReadAsync(session.CancellationToken?.Token ?? CancellationToken.None))
                {
                    var values = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = await reader.IsDBNullAsync(i) ? "NULL" : (await reader.GetFieldValueAsync<object>(i))?.ToString() ?? "";
                        values.Add(value);
                    }
                    results.AppendLine(string.Join("\t", values));
                    rowCount++;
                    
                    // Update session periodically
                    if (rowCount % 1000 == 0)
                    {
                        session.RowCount = rowCount;
                        session.Results = results;
                    }
                }
                
                // Update final results
                session.RowCount = rowCount;
                session.Results = results;
                session.IsRunning = false;
                session.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation("Query session {SessionId} completed successfully with {RowCount} rows", 
                    session.SessionId, rowCount);
            }
            catch (OperationCanceledException)
            {
                session.IsRunning = false;
                session.EndTime = DateTime.UtcNow;
                session.Error = "Query execution was cancelled";
                
                _logger.LogInformation("Query session {SessionId} was cancelled", session.SessionId);
            }
            catch (Exception ex)
            {
                session.IsRunning = false;
                session.EndTime = DateTime.UtcNow;
                session.Error = ex.Message;
                
                _logger.LogError(ex, "Query session {SessionId} failed", session.SessionId);
            }
            finally
            {
                // Ensure session is marked as not running
                session.IsRunning = false;
                if (!session.EndTime.HasValue)
                {
                    session.EndTime = DateTime.UtcNow;
                }
            }
        }
        
        /// <summary>
        /// Executes a stored procedure in the background and updates the session with results.
        /// </summary>
        private async Task ExecuteStoredProcedureInBackground(QuerySession session)
        {
            try
            {
                _logger.LogDebug("Executing stored procedure for session {SessionId}", session.SessionId);
                
                using var reader = await _databaseService.ExecuteStoredProcedureAsync(
                    session.Query, 
                    session.Parameters ?? new Dictionary<string, object?>(), 
                    session.DatabaseName, 
                    null,
                    session.TimeoutSeconds,
                    session.CancellationToken?.Token ?? CancellationToken.None);
                
                var results = new StringBuilder();
                var rowCount = 0L;
                
                // Write column headers
                var columnNames = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    columnNames.Add(reader.GetName(i));
                }
                results.AppendLine(string.Join("\t", columnNames));
                
                // Read all rows
                while (await reader.ReadAsync(session.CancellationToken?.Token ?? CancellationToken.None))
                {
                    var values = new List<string>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var value = await reader.IsDBNullAsync(i) ? "NULL" : (await reader.GetFieldValueAsync<object>(i))?.ToString() ?? "";
                        values.Add(value);
                    }
                    results.AppendLine(string.Join("\t", values));
                    rowCount++;
                    
                    // Update session periodically
                    if (rowCount % 1000 == 0)
                    {
                        session.RowCount = rowCount;
                        session.Results = results;
                    }
                }
                
                // Update final results
                session.RowCount = rowCount;
                session.Results = results;
                session.IsRunning = false;
                session.EndTime = DateTime.UtcNow;
                
                _logger.LogInformation("Stored procedure session {SessionId} completed successfully with {RowCount} rows", 
                    session.SessionId, rowCount);
            }
            catch (OperationCanceledException)
            {
                session.IsRunning = false;
                session.EndTime = DateTime.UtcNow;
                session.Error = "Stored procedure execution was cancelled";
                
                _logger.LogInformation("Stored procedure session {SessionId} was cancelled", session.SessionId);
            }
            catch (Exception ex)
            {
                session.IsRunning = false;
                session.EndTime = DateTime.UtcNow;
                session.Error = ex.Message;
                
                _logger.LogError(ex, "Stored procedure session {SessionId} failed", session.SessionId);
            }
            finally
            {
                // Ensure session is marked as not running
                session.IsRunning = false;
                if (!session.EndTime.HasValue)
                {
                    session.EndTime = DateTime.UtcNow;
                }
            }
        }
    }
}