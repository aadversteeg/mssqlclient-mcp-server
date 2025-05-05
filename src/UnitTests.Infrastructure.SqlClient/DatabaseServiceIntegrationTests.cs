using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class DatabaseServiceIntegrationTests : IDisposable
    {
        private const string TestInstanceName = "IntegrationTest";
        private const string TestDbName = "TestDatabase";
        private readonly string _localDbConnectionString;
        private readonly string _masterConnectionString;
        private readonly string _userDbConnectionString;
        private readonly IDatabaseService _masterDatabaseService;
        private readonly IDatabaseService _userDatabaseService;

        public DatabaseServiceIntegrationTests()
        {
            // Set up the LocalDB instance
            SetupLocalDbInstance();

            // Create connection strings for master and user databases
            _localDbConnectionString = $@"Server=(localdb)\{TestInstanceName};Integrated Security=true;Connection Timeout=30;";
            _masterConnectionString = $@"Server=(localdb)\{TestInstanceName};Database=master;Integrated Security=true;Connection Timeout=30;";
            _userDbConnectionString = $@"Server=(localdb)\{TestInstanceName};Database={TestDbName};Integrated Security=true;Connection Timeout=30;";

            // Create the master database service
            _masterDatabaseService = new DatabaseService(_masterConnectionString);
            
            // Create the test database and initialize services
            CreateTestDatabase().GetAwaiter().GetResult();
            _userDatabaseService = new DatabaseService(_userDbConnectionString);
        }

        public void Dispose()
        {
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

        [Fact(DisplayName = "DBS-001: ListDatabasesAsync returns master and test databases")]
        public async Task DBS001()
        {
            // Arrange
            
            // Act
            var databases = await _masterDatabaseService.ListDatabasesAsync();
            
            // Assert
            databases.Should().NotBeNull();
            databases.Should().Contain(db => db.Name.Equals("master", StringComparison.OrdinalIgnoreCase));
            databases.Should().Contain(db => db.Name.Equals(TestDbName, StringComparison.OrdinalIgnoreCase));
        }
        
        [Fact(DisplayName = "DBS-002: DoesDatabaseExistAsync returns true for existing database")]
        public async Task DBS002()
        {
            // Arrange
            
            // Act
            var exists = await _masterDatabaseService.DoesDatabaseExistAsync(TestDbName);
            
            // Assert
            exists.Should().BeTrue();
        }
        
        [Fact(DisplayName = "DBS-003: DoesDatabaseExistAsync returns false for non-existing database")]
        public async Task DBS003()
        {
            // Arrange
            var nonExistentDbName = "NonExistentDb";
            
            // Act
            var exists = await _masterDatabaseService.DoesDatabaseExistAsync(nonExistentDbName);
            
            // Assert
            exists.Should().BeFalse();
        }
        
        [Fact(DisplayName = "DBS-004: ListTablesAsync returns tables from test database")]
        public async Task DBS004()
        {
            // Arrange
            
            // Act
            var tables = await _userDatabaseService.ListTablesAsync();
            
            // Assert
            tables.Should().NotBeNull();
            tables.Should().HaveCount(2);
            tables.Should().Contain(t => t.Name.Equals("TestTable1", StringComparison.OrdinalIgnoreCase));
            tables.Should().Contain(t => t.Name.Equals("TestTable2", StringComparison.OrdinalIgnoreCase));
            
            // Verify table properties
            var testTable1 = tables.First(t => t.Name.Equals("TestTable1", StringComparison.OrdinalIgnoreCase));
            testTable1.Schema.Should().Be("dbo");
            testTable1.RowCount.Should().Be(2);
            
            var testTable2 = tables.First(t => t.Name.Equals("TestTable2", StringComparison.OrdinalIgnoreCase));
            testTable2.Schema.Should().Be("dbo");
            testTable2.RowCount.Should().Be(1);
            testTable2.ForeignKeyCount.Should().Be(1);
        }
        
        [Fact(DisplayName = "DBS-005: ListTablesAsync with database name parameter switches context")]
        public async Task DBS005()
        {
            // Arrange
            
            // Act
            var tables = await _masterDatabaseService.ListTablesAsync(TestDbName);
            
            // Assert
            tables.Should().NotBeNull();
            tables.Should().HaveCount(2);
            tables.Should().Contain(t => t.Name.Equals("TestTable1", StringComparison.OrdinalIgnoreCase));
            tables.Should().Contain(t => t.Name.Equals("TestTable2", StringComparison.OrdinalIgnoreCase));
        }
        
        [Fact(DisplayName = "DBS-006: GetCurrentDatabaseName returns correct database name")]
        public void DBS006()
        {
            // Arrange
            
            // Act
            var masterDbName = _masterDatabaseService.GetCurrentDatabaseName();
            var userDbName = _userDatabaseService.GetCurrentDatabaseName();
            
            // Assert
            masterDbName.Should().Be("master");
            userDbName.Should().Be(TestDbName);
        }
        
        [Fact(DisplayName = "DBS-007: IsMasterDatabaseAsync returns true for master database")]
        public async Task DBS007()
        {
            // Arrange
            
            // Act
            var isMaster = await _masterDatabaseService.IsMasterDatabaseAsync();
            
            // Assert
            isMaster.Should().BeTrue();
        }
        
        [Fact(DisplayName = "DBS-008: IsMasterDatabaseAsync returns false for user database")]
        public async Task DBS008()
        {
            // Arrange
            
            // Act
            var isMaster = await _userDatabaseService.IsMasterDatabaseAsync();
            
            // Assert
            isMaster.Should().BeFalse();
        }
    }
}