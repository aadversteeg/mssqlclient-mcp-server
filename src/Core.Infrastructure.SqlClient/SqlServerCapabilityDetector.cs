using Microsoft.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.Infrastructure.SqlClient.Interfaces;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Service responsible for detecting SQL Server capabilities based on version and edition.
    /// This class helps determine which features are available on the connected SQL Server instance.
    /// </summary>
    public class SqlServerCapabilityDetector : ISqlServerCapabilityDetector
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerCapabilityDetector"/> class.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string</param>
        public SqlServerCapabilityDetector(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        /// <summary>
        /// Detects the capabilities of the SQL Server instance.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A SqlServerCapability object containing information about the server's capabilities</returns>
        public async Task<SqlServerCapability> DetectCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get server version information
            var (version, majorVersion, minorVersion, buildNumber, edition) = await GetServerVersionInfoAsync(connection, cancellationToken);
            
            // Determine server type (Azure SQL DB, Azure VM, On-premises)
            var (isAzureDb, isAzureVm, isOnPrem) = await DetectServerTypeAsync(connection, version, cancellationToken);

            return new SqlServerCapability
            {
                Version = version,
                MajorVersion = majorVersion,
                MinorVersion = minorVersion,
                BuildNumber = buildNumber,
                Edition = edition,
                
                // Server type
                IsAzureSqlDatabase = isAzureDb,
                IsAzureVmSqlServer = isAzureVm,
                IsOnPremisesSqlServer = isOnPrem,
                
                // Features based on version
                SupportsPartitioning = majorVersion >= 10,  // SQL 2008+
                SupportsColumnstoreIndex = majorVersion >= 11, // SQL 2012+
                SupportsJson = (majorVersion >= 13), // SQL 2016+
                SupportsInMemoryOLTP = (majorVersion >= 12), // SQL 2014+
                SupportsRowLevelSecurity = (majorVersion >= 13), // SQL 2016+
                SupportsDynamicDataMasking = (majorVersion >= 13), // SQL 2016+
                SupportsDataCompression = (majorVersion >= 10), // SQL 2008+
                SupportsDatabaseSnapshots = (majorVersion >= 9) && !isAzureDb, // SQL 2005+ but not in Azure SQL DB
                SupportsQueryStore = (majorVersion >= 13), // SQL 2016+
                SupportsResumableIndexOperations = (majorVersion >= 14), // SQL 2017+
                SupportsGraphDatabase = (majorVersion >= 14), // SQL 2017+
                SupportsAlwaysEncrypted = (majorVersion >= 13), // SQL 2016+
                SupportsExactRowCount = await DetectExactRowCountSupportAsync(connection, cancellationToken),
                SupportsDetailedIndexMetadata = await DetectDetailedIndexMetadataSupportAsync(connection, cancellationToken),
                SupportsTemporalTables = (majorVersion >= 13) // SQL 2016+
            };
        }

        /// <summary>
        /// Gets detailed server version information.
        /// </summary>
        /// <param name="connection">An open SQL connection</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Tuple containing version string, major version, minor version, build number, and edition</returns>
        private async Task<(string version, int majorVersion, int minorVersion, int buildNumber, string edition)> 
            GetServerVersionInfoAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            string version = string.Empty;
            string edition = string.Empty;
            int majorVersion = 0;
            int minorVersion = 0;
            int buildNumber = 0;

            try
            {
                // Get version string
                const string versionQuery = @"
                    SELECT @@VERSION AS ServerVersion,
                           SERVERPROPERTY('ProductVersion') AS ProductVersion, 
                           SERVERPROPERTY('Edition') AS Edition";

                using (var command = new SqlCommand(versionQuery, connection))
                using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        version = reader["ServerVersion"].ToString() ?? string.Empty;
                        string productVersion = reader["ProductVersion"].ToString() ?? string.Empty;
                        edition = reader["Edition"].ToString() ?? string.Empty;

                        // Parse version numbers from product version (format: 15.0.2000.5)
                        string[] versionParts = productVersion.Split('.');
                        if (versionParts.Length >= 3)
                        {
                            if (int.TryParse(versionParts[0], out int major))
                                majorVersion = major;
                                
                            if (int.TryParse(versionParts[1], out int minor))
                                minorVersion = minor;
                                
                            if (int.TryParse(versionParts[2], out int build))
                                buildNumber = build;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error getting SQL Server version: {ex.Message}");
                
                // If the above method fails, try a simpler approach using the connection's ServerVersion
                version = connection.ServerVersion;
                
                // Parse version string (format typically: 15.00.2000)
                string[] parts = version.Split('.');
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[0], out int major))
                        majorVersion = major;
                        
                    if (int.TryParse(parts[1], out int minor))
                        minorVersion = minor;
                        
                    if (int.TryParse(parts[2], out int build))
                        buildNumber = build;
                }
            }

            return (version, majorVersion, minorVersion, buildNumber, edition);
        }
        
        /// <summary>
        /// Detects the type of SQL Server (Azure SQL DB, Azure VM, On-premises).
        /// </summary>
        /// <param name="connection">An open SQL connection</param>
        /// <param name="versionString">The SQL Server version string</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>Tuple of booleans indicating whether it's Azure SQL DB, Azure VM, or on-premises</returns>
        private async Task<(bool isAzureDb, bool isAzureVm, bool isOnPrem)> 
            DetectServerTypeAsync(SqlConnection connection, string versionString, CancellationToken cancellationToken)
        {
            bool isAzureDb = false;
            bool isAzureVm = false;
            bool isOnPrem = false;

            try
            {
                // Check for Azure SQL Database
                const string azureQuery = @"
                    SELECT CASE 
                        WHEN SERVERPROPERTY('EngineEdition') = 5 THEN 1 
                        ELSE 0 
                    END AS IsAzureSqlDb";

                using (var command = new SqlCommand(azureQuery, connection))
                {
                    var result = await command.ExecuteScalarAsync(cancellationToken);
                    isAzureDb = Convert.ToBoolean(result);
                }

                // If it's not Azure SQL DB, determine if it's Azure VM or on-premises
                if (!isAzureDb)
                {
                    // Check for Azure VM by looking for Azure-specific strings in version info
                    isAzureVm = versionString.Contains("Windows Azure", StringComparison.OrdinalIgnoreCase) ||
                                versionString.Contains("Microsoft Azure", StringComparison.OrdinalIgnoreCase);
                    
                    // If it's not Azure VM, then it's on-premises
                    isOnPrem = !isAzureVm;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error detecting SQL Server type: {ex.Message}");
                
                // If we can't determine specifically, assume on-premises
                isOnPrem = true;
            }

            return (isAzureDb, isAzureVm, isOnPrem);
        }
        
        /// <summary>
        /// Detects whether the server supports exact row count retrieval.
        /// </summary>
        /// <param name="connection">An open SQL connection</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the server supports exact row count retrieval, otherwise false</returns>
        private async Task<bool> DetectExactRowCountSupportAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                // Try a row count query that would be problematic on older versions with large tables
                const string countQuery = @"
                    SELECT OBJECT_NAME(p.object_id) AS TableName,
                           SUM(p.rows) AS [RowCount]
                    FROM sys.partitions p
                    WHERE p.index_id IN (0, 1) -- heap or clustered index
                    GROUP BY p.object_id
                    ORDER BY TableName";

                using (var command = new SqlCommand(countQuery, connection))
                {
                    // Just testing if the command executes successfully
                    using var _ = await command.ExecuteReaderAsync(cancellationToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error testing exact row count support: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Detects whether the server supports detailed index metadata.
        /// </summary>
        /// <param name="connection">An open SQL connection</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if the server supports detailed index metadata, otherwise false</returns>
        private async Task<bool> DetectDetailedIndexMetadataSupportAsync(SqlConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                // Try using DMVs that are available in modern SQL Server versions
                const string indexQuery = @"
                    SELECT TOP 1
                        i.name AS IndexName,
                        i.type_desc AS IndexType,
                        i.is_primary_key,
                        i.is_unique,
                        i.is_unique_constraint,
                        i.fill_factor,
                        s.avg_fragmentation_in_percent
                    FROM sys.indexes i
                    LEFT JOIN sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
                    ON i.object_id = s.object_id AND i.index_id = s.index_id
                    WHERE i.object_id > 100";

                using (var command = new SqlCommand(indexQuery, connection))
                {
                    // Just testing if the command executes successfully
                    using var _ = await command.ExecuteReaderAsync(cancellationToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error testing detailed index metadata support: {ex.Message}");
                return false;
            }
        }
    }
}