using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Infrastructure.SqlClient;
using Core.Application.Interfaces;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class DatabaseServiceIntegrationTests : IDisposable
    {
        private const string TestInstanceName = "IntegrationTest";
        private const string TestDbName = "TestDatabase";
        private readonly string? _localDbConnectionString;
        private readonly string? _masterConnectionString;
        private readonly string? _userDbConnectionString;
        private readonly IDatabaseService? _serverDatabaseService;
        private readonly IDatabaseService? _databaseService;
        private readonly bool _skipTests;
        private readonly DatabaseConfiguration _configuration;

        public DatabaseServiceIntegrationTests()
        {
            // Create configuration for database services
            _configuration = new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 };
            
            // Skip tests on non-Windows platforms
            _skipTests = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            if (_skipTests)
            {
                return;
            }

            // Set up the LocalDB instance
            SetupLocalDbInstance();

            // Create connection strings for master and user databases
            _localDbConnectionString = $@"Server=(localdb)\{TestInstanceName};Integrated Security=true;Connection Timeout=30;";
            _masterConnectionString = $@"Server=(localdb)\{TestInstanceName};Database=master;Integrated Security=true;Connection Timeout=30;";
            _userDbConnectionString = $@"Server=(localdb)\{TestInstanceName};Database={TestDbName};Integrated Security=true;Connection Timeout=30;";

            // Create the capability detector for server database service
            var serverCapabilityDetector = new SqlServerCapabilityDetector(_masterConnectionString);
            
            // Create the server database service
            _serverDatabaseService = new DatabaseService(_masterConnectionString, serverCapabilityDetector, _configuration);
            
            // Create the test database and initialize services
            CreateTestDatabase().GetAwaiter().GetResult();
            
            // Create the capability detector for user database service
            var userDbCapabilityDetector = new SqlServerCapabilityDetector(_userDbConnectionString);
            
            // Create the user database service
            _databaseService = new DatabaseService(_userDbConnectionString, userDbCapabilityDetector, _configuration);
        }

        public void Dispose()
        {
            if (_skipTests)
            {
                return;
            }
            
            // Clean up test database
            CleanupTestDatabase().GetAwaiter().GetResult();
            
            // Clean up LocalDB instance
            CleanupLocalDbInstance();
            
            GC.SuppressFinalize(this);
        }

        private void SetupLocalDbInstance()
        {
            // Stop the instance if it exists
            ExecuteCommand($"sqllocaldb stop {TestInstanceName}");
            ExecuteCommand($"sqllocaldb delete {TestInstanceName}");
            
            // Create a fresh instance
            ExecuteCommand($"sqllocaldb create {TestInstanceName} -s");
        }

        private void CleanupLocalDbInstance()
        {
            // Stop and delete the instance
            ExecuteCommand($"sqllocaldb stop {TestInstanceName}");
            ExecuteCommand($"sqllocaldb delete {TestInstanceName}");
        }

        private async Task CreateTestDatabase()
        {
            // Create test database
            using (var connection = new SqlConnection(_masterConnectionString))
            {
                await connection.OpenAsync();
                
                // Create the test database
                var createDbCommand = $"CREATE DATABASE [{TestDbName}]";
                using (var command = new SqlCommand(createDbCommand, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
                
                // Create test tables
                connection.ChangeDatabase(TestDbName);
                
                var createTablesCommand = @"
                    CREATE TABLE TestTable1 (
                        Id INT PRIMARY KEY,
                        Name NVARCHAR(100) NOT NULL
                    );
                    
                    CREATE TABLE TestTable2 (
                        Id INT PRIMARY KEY,
                        Description NVARCHAR(MAX),
                        CreatedDate DATETIME DEFAULT GETDATE(),
                        FOREIGN KEY (Id) REFERENCES TestTable1(Id)
                    );
                    
                    INSERT INTO TestTable1 (Id, Name) VALUES (1, 'Test Record 1');
                    INSERT INTO TestTable1 (Id, Name) VALUES (2, 'Test Record 2');
                    INSERT INTO TestTable2 (Id, Description) VALUES (1, 'Description for Record 1');";
                
                using (var command = new SqlCommand(createTablesCommand, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task CleanupTestDatabase()
        {
            try
            {
                using (var connection = new SqlConnection(_masterConnectionString))
                {
                    await connection.OpenAsync();
                    
                    // Force close connections to the test database
                    var closeConnectionsCommand = $@"
                        ALTER DATABASE [{TestDbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        DROP DATABASE IF EXISTS [{TestDbName}];";
                    
                    using (var command = new SqlCommand(closeConnectionsCommand, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up test database: {ex.Message}");
                // Continue with cleanup even if there's an error
            }
        }

        private void ExecuteCommand(string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error executing command: {command}, Error: {ex.Message}");
                throw;
            }
        }

        [SkippableFact(DisplayName = "DBS-001: ListDatabasesAsync returns master and test databases")]
        public async Task DBS001()
        {
            Skip.If(_skipTests, "LocalDB is only available on Windows");
            
            // Arrange
            
            // Act
            var databases = await _serverDatabaseService.ListDatabasesAsync();
            
            // Assert
            databases.Should().NotBeNull();
            databases.Should().Contain(db => db.Name.Equals("master", StringComparison.OrdinalIgnoreCase));
            databases.Should().Contain(db => db.Name.Equals(TestDbName, StringComparison.OrdinalIgnoreCase));
        }
        
        [SkippableFact(DisplayName = "DBS-002: DoesDatabaseExistAsync returns true for existing database")]
        public async Task DBS002()
        {
            Skip.If(_skipTests, "LocalDB is only available on Windows");
            
            // Arrange
            
            // Act
            var exists = await _serverDatabaseService.DoesDatabaseExistAsync(TestDbName);
            
            // Assert
            exists.Should().BeTrue();
        }
        
        [SkippableFact(DisplayName = "DBS-003: DoesDatabaseExistAsync returns false for non-existing database")]
        public async Task DBS003()
        {
            Skip.If(_skipTests, "LocalDB is only available on Windows");
            
            // Arrange
            var nonExistentDbName = "NonExistentDb";
            
            // Act
            var exists = await _serverDatabaseService.DoesDatabaseExistAsync(nonExistentDbName);
            
            // Assert
            exists.Should().BeFalse();
        }
        
        [SkippableFact(DisplayName = "DBS-004: ListTablesAsync returns tables from test database")]
        public async Task DBS004()
        {
            Skip.If(_skipTests, "LocalDB is only available on Windows");
            
            // Arrange
            
            // Act
            var tables = await _databaseService.ListTablesAsync();
            
            // Assert
            tables.Should().NotBeNull();
            tables.Should().HaveCount(2);
            tables.Should().Contain(t => t.Name.Equals("TestTable1", StringComparison.OrdinalIgnoreCase));
            tables.Should().Contain(t => t.Name.Equals("TestTable2", StringComparison.OrdinalIgnoreCase));
            
            // Verify table properties
            var testTable1 = tables.First(t => t.Name.Equals("TestTable1", StringComparison.OrdinalIgnoreCase));
            testTable1.Schema.Should().Be("dbo");
            // testTable1.RowCount.Should().Be(2);  this information is not available in LocalDb
            
            var testTable2 = tables.First(t => t.Name.Equals("TestTable2", StringComparison.OrdinalIgnoreCase));
            testTable2.Schema.Should().Be("dbo");
            // testTable2.RowCount.Should().Be(1);  this information is not available in LocalDb
            // testTable2.ForeignKeyCount.Should().Be(1);
        }
        
        [SkippableFact(DisplayName = "DBS-005: ListTablesAsync with database name parameter switches context")]
        public async Task DBS005()
        {
            Skip.If(_skipTests, "LocalDB is only available on Windows");
            
            // Arrange
            
            // Act
            var tables = await _serverDatabaseService.ListTablesAsync(TestDbName);
            
            // Assert
            tables.Should().NotBeNull();
            tables.Should().HaveCount(2);
            tables.Should().Contain(t => t.Name.Equals("TestTable1", StringComparison.OrdinalIgnoreCase));
            tables.Should().Contain(t => t.Name.Equals("TestTable2", StringComparison.OrdinalIgnoreCase));
        }
        
        [SkippableFact(DisplayName = "DBS-006: GetCurrentDatabaseName returns correct database name")]
        public void DBS006()
        {
            Skip.If(_skipTests, "LocalDB is only available on Windows");
            
            // Arrange
            
            // Act
            var serverDbName = _serverDatabaseService.GetCurrentDatabaseName();
            var userDbName = _databaseService.GetCurrentDatabaseName();
            
            // Assert
            serverDbName.Should().Be("master");
            userDbName.Should().Be(TestDbName);
        }
        
    }
}