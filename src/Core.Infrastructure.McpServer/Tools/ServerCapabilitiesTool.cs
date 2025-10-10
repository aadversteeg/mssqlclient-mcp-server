using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Models;
using Core.Infrastructure.SqlClient.Interfaces;
using ModelContextProtocol.Server;

namespace Core.Infrastructure.McpServer.Tools
{
    /// <summary>
    /// Tool that provides information about SQL Server capabilities
    /// </summary>
    [McpServerToolType]
    public class ServerCapabilitiesTool
    {
        private readonly ISqlServerCapabilityDetector _capabilityDetector;
        private readonly IDatabaseService _databaseService;
        private readonly bool _isDatabaseMode;

        /// <summary>
        /// Initializes a new instance of the ServerCapabilitiesTool
        /// </summary>
        /// <param name="capabilityDetector">The SQL Server capability detector</param>
        /// <param name="databaseService">The database service</param>
        public ServerCapabilitiesTool(ISqlServerCapabilityDetector capabilityDetector, IDatabaseService databaseService)
        {
            _capabilityDetector = capabilityDetector;
            _databaseService = databaseService;
            
            // Determine if we're in database mode by checking if a database is specified in the connection string
            string currentDb = _databaseService.GetCurrentDatabaseName();
            _isDatabaseMode = !string.IsNullOrWhiteSpace(currentDb);
        }

        /// <summary>
        /// Get SQL Server capabilities
        /// </summary>
        /// <returns>Server capabilities information</returns>
        [McpServerTool(Name = "server_capabilities"), Description("Get SQL Server capabilities")]
        public async Task<ServerCapabilitiesResponse> GetServerCapabilitiesAsync()
        {
            // Get capabilities using a default cancellation token
            var capabilities = await _capabilityDetector.DetectCapabilitiesAsync(CancellationToken.None);
            
            // Get current database name if in database mode
            string? databaseName = null;
            if (_isDatabaseMode)
            {
                databaseName = _databaseService.GetCurrentDatabaseName();
            }
            
            // Return capabilities response
            return new ServerCapabilitiesResponse(capabilities, _isDatabaseMode, databaseName);
        }
    }
}