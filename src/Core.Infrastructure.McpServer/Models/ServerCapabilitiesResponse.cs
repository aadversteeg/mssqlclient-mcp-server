using Core.Infrastructure.SqlClient;
using System.Text.Json.Serialization;

namespace Core.Infrastructure.McpServer.Models
{
    /// <summary>
    /// Response model for SQL Server capabilities
    /// </summary>
    public class ServerCapabilitiesResponse
    {
        /// <summary>
        /// Full version string of the SQL Server instance
        /// </summary>
        public string Version { get; }
        
        /// <summary>
        /// Major version number
        /// </summary>
        public int MajorVersion { get; }
        
        /// <summary>
        /// Minor version number
        /// </summary>
        public int MinorVersion { get; }
        
        /// <summary>
        /// Build number
        /// </summary>
        public int BuildNumber { get; }
        
        /// <summary>
        /// SQL Server edition (e.g., "Enterprise Edition")
        /// </summary>
        public string Edition { get; }
        
        /// <summary>
        /// Whether the server is Azure SQL Database
        /// </summary>
        public bool IsAzureSqlDatabase { get; }
        
        /// <summary>
        /// Whether the server is SQL Server running on Azure VM
        /// </summary>
        public bool IsAzureVmSqlServer { get; }
        
        /// <summary>
        /// Whether the server is on-premises SQL Server
        /// </summary>
        public bool IsOnPremisesSqlServer { get; }
        
        /// <summary>
        /// The mode in which the tool is running ("server" or "database")
        /// </summary>
        public string ToolMode { get; }
        
        /// <summary>
        /// The name of the current database (only included in database mode)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DatabaseName { get; }
        
        /// <summary>
        /// Dictionary of supported features
        /// </summary>
        public Dictionary<string, bool> Features { get; }

        /// <summary>
        /// Creates a new ServerCapabilitiesResponse
        /// </summary>
        /// <param name="capability">The SQL Server capability information</param>
        /// <param name="isDatabaseMode">Whether the tool is running in database mode</param>
        /// <param name="databaseName">The name of the current database (only in database mode)</param>
        public ServerCapabilitiesResponse(SqlServerCapability capability, bool isDatabaseMode, string? databaseName = null)
        {
            Version = capability.Version;
            MajorVersion = capability.MajorVersion;
            MinorVersion = capability.MinorVersion;
            BuildNumber = capability.BuildNumber;
            Edition = capability.Edition;
            IsAzureSqlDatabase = capability.IsAzureSqlDatabase;
            IsAzureVmSqlServer = capability.IsAzureVmSqlServer;
            IsOnPremisesSqlServer = capability.IsOnPremisesSqlServer;
            
            // Set tool mode and database name
            ToolMode = isDatabaseMode ? "database" : "server";
            DatabaseName = isDatabaseMode ? databaseName : null;
            
            // Convert all feature flags to a dictionary for easier consumption
            Features = new Dictionary<string, bool>
            {
                ["SupportsPartitioning"] = capability.SupportsPartitioning,
                ["SupportsColumnstoreIndex"] = capability.SupportsColumnstoreIndex,
                ["SupportsJson"] = capability.SupportsJson,
                ["SupportsInMemoryOLTP"] = capability.SupportsInMemoryOLTP,
                ["SupportsRowLevelSecurity"] = capability.SupportsRowLevelSecurity,
                ["SupportsDynamicDataMasking"] = capability.SupportsDynamicDataMasking,
                ["SupportsDataCompression"] = capability.SupportsDataCompression,
                ["SupportsDatabaseSnapshots"] = capability.SupportsDatabaseSnapshots,
                ["SupportsQueryStore"] = capability.SupportsQueryStore,
                ["SupportsResumableIndexOperations"] = capability.SupportsResumableIndexOperations,
                ["SupportsGraphDatabase"] = capability.SupportsGraphDatabase,
                ["SupportsAlwaysEncrypted"] = capability.SupportsAlwaysEncrypted,
                ["SupportsExactRowCount"] = capability.SupportsExactRowCount,
                ["SupportsDetailedIndexMetadata"] = capability.SupportsDetailedIndexMetadata,
                ["SupportsTemporalTables"] = capability.SupportsTemporalTables
            };
        }
    }
}