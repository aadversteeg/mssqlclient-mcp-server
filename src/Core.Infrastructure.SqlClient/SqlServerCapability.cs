using System;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Represents the capabilities of a SQL Server instance.
    /// This record contains information about supported features and version-specific capabilities.
    /// </summary>
    public sealed record SqlServerCapability
    {
        /// <summary>
        /// Gets the SQL Server version string (e.g., "Microsoft SQL Server 2019 (RTM) - 15.0.2000.5").
        /// </summary>
        public string Version { get; init; } = string.Empty;

        /// <summary>
        /// Gets the major version number (e.g., 15 for SQL Server 2019).
        /// </summary>
        public int MajorVersion { get; init; }

        /// <summary>
        /// Gets the minor version number.
        /// </summary>
        public int MinorVersion { get; init; }

        /// <summary>
        /// Gets the build number.
        /// </summary>
        public int BuildNumber { get; init; }

        /// <summary>
        /// Gets the SQL Server edition (e.g., "Enterprise", "Standard", "Express").
        /// </summary>
        public string Edition { get; init; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the server supports partitioning feature.
        /// </summary>
        public bool SupportsPartitioning { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports columnstore indexes.
        /// </summary>
        public bool SupportsColumnstoreIndex { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports JSON data operations.
        /// </summary>
        public bool SupportsJson { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports memory-optimized tables.
        /// </summary>
        public bool SupportsInMemoryOLTP { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports row-level security.
        /// </summary>
        public bool SupportsRowLevelSecurity { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports dynamic data masking.
        /// </summary>
        public bool SupportsDynamicDataMasking { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports data compression.
        /// </summary>
        public bool SupportsDataCompression { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports database snapshots.
        /// </summary>
        public bool SupportsDatabaseSnapshots { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports query store.
        /// </summary>
        public bool SupportsQueryStore { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports resumable index operations.
        /// </summary>
        public bool SupportsResumableIndexOperations { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports graph database features.
        /// </summary>
        public bool SupportsGraphDatabase { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports always encrypted feature.
        /// </summary>
        public bool SupportsAlwaysEncrypted { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports exact row count retrieval.
        /// Some queries may fail on older versions when trying to get exact row counts for large tables.
        /// </summary>
        public bool SupportsExactRowCount { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports modern index metadata with additional details.
        /// </summary>
        public bool SupportsDetailedIndexMetadata { get; init; }

        /// <summary>
        /// Gets a value indicating whether the server supports temporal tables.
        /// </summary>
        public bool SupportsTemporalTables { get; init; }

        /// <summary>
        /// Gets whether this server is Azure SQL Database (PaaS).
        /// </summary>
        public bool IsAzureSqlDatabase { get; init; }

        /// <summary>
        /// Gets whether this server is SQL Server on an Azure VM (IaaS).
        /// </summary>
        public bool IsAzureVmSqlServer { get; init; }

        /// <summary>
        /// Gets whether this server is an on-premises SQL Server.
        /// </summary>
        public bool IsOnPremisesSqlServer { get; init; }
    }
}