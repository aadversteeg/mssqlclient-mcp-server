using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Moq;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class ServerExecuteQueryToolTests
    {
        [Fact(DisplayName = "SEQT-001: ServerExecuteQueryTool constructor with null server database throws ArgumentNullException")]
        public void SEQT001()
        {
            // Act
            IServerDatabase? nullServerDb = null;
            Action act = () => new ServerExecuteQueryTool(nullServerDb, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SEQT-002: ServerExecuteQueryTool returns error for empty database name")]
        public async Task SEQT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerExecuteQueryTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ExecuteQueryInDatabase(string.Empty, "SELECT 1", null);
            
            // Assert
            result.Should().Contain("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SEQT-003: ServerExecuteQueryTool returns error for empty query")]
        public async Task SEQT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerExecuteQueryTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ExecuteQueryInDatabase("TestDb", string.Empty, null);
            
            // Assert
            result.Should().Contain("Error: Query cannot be empty");
        }
        
        [Fact(DisplayName = "SEQT-004: ServerExecuteQueryTool executes query successfully")]
        public async Task SEQT004()
        {
            // Arrange
            var databaseName = "TestDb";
            var query = "SELECT * FROM Users";
            
            var mockReader = new Mock<IAsyncDataReader>();
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false); // No rows to read
            mockReader.Setup(x => x.FieldCount)
                .Returns(0);
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(
                databaseName,
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerExecuteQueryTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ExecuteQueryInDatabase(databaseName, query, null);
            
            // Assert
            result.Should().NotBeNull();
            mockServerDatabase.Verify(x => x.ExecuteQueryInDatabaseAsync(
                databaseName,
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                null,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact(DisplayName = "SEQT-005: ServerExecuteQueryTool handles exception from server database")]
        public async Task SEQT005()
        {
            // Arrange
            var databaseName = "TestDb";
            var query = "SELECT * FROM Users";
            var expectedErrorMessage = "Error executing query";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(
                databaseName,
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerExecuteQueryTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ExecuteQueryInDatabase(databaseName, query, null);
            
            // Assert
            result.Should().Be($"Error: SQL error while executing query: {expectedErrorMessage}");
        }
        
        [Fact(DisplayName = "SEQT-006: ServerExecuteQueryTool passes timeout to server database")]
        public async Task SEQT006()
        {
            // Arrange
            var databaseName = "TestDb";
            var query = "SELECT * FROM Users";
            var timeoutSeconds = 300;
            
            var mockReader = new Mock<IAsyncDataReader>();
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false); // No rows to read
            mockReader.Setup(x => x.FieldCount)
                .Returns(0);
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(
                databaseName,
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                timeoutSeconds,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerExecuteQueryTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ExecuteQueryInDatabase(databaseName, query, timeoutSeconds);
            
            // Assert
            result.Should().NotBeNull();
            mockServerDatabase.Verify(x => x.ExecuteQueryInDatabaseAsync(
                databaseName,
                query,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                timeoutSeconds,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}


