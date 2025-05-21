using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class ServerExecuteStoredProcedureToolTests
    {
        [Fact(DisplayName = "SESPT-001: ServerExecuteStoredProcedureTool constructor with null server database throws ArgumentNullException")]
        public void SESPT001()
        {
            // Act
            IServerDatabase? nullServerDb = null;
            Action act = () => new ServerExecuteStoredProcedureTool(nullServerDb);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SESPT-002: ServerExecuteStoredProcedureTool returns error for empty database name")]
        public async Task SESPT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerExecuteStoredProcedureTool(mockServerDatabase.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedureInDatabase(string.Empty, "TestProc", "{}");
            
            // Assert
            result.Should().Contain("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SESPT-003: ServerExecuteStoredProcedureTool returns error for empty procedure name")]
        public async Task SESPT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerExecuteStoredProcedureTool(mockServerDatabase.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedureInDatabase("TestDb", string.Empty, "{}");
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SESPT-004: ServerExecuteStoredProcedureTool executes stored procedure with parameters")]
        public async Task SESPT004()
        {
            // Arrange
            var databaseName = "TestDb";
            var procedureName = "TestProc";
            var parameters = new Dictionary<string, object?>
            {
                { "Param1", 123 },
                { "Param2", "test" }
            };
            var parametersJson = JsonSerializer.Serialize(parameters);
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return a simple result
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false); // No rows to read
            mockReader.Setup(x => x.FieldCount)
                .Returns(0);
            
            mockServerDatabase.Setup(x => x.ExecuteStoredProcedureAsync(
                databaseName,
                procedureName, 
                It.Is<Dictionary<string, object?>>(p => 
                    p.Count == parameters.Count && 
                    p.ContainsKey("Param1") && 
                    p.ContainsKey("Param2")
                ), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerExecuteStoredProcedureTool(mockServerDatabase.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedureInDatabase(databaseName, procedureName, parametersJson);
            
            // Assert
            result.Should().NotBeNull();
            mockServerDatabase.Verify(x => x.ExecuteStoredProcedureAsync(
                databaseName,
                procedureName,
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
        
        [Fact(DisplayName = "SESPT-005: ServerExecuteStoredProcedureTool handles exception from server database")]
        public async Task SESPT005()
        {
            // Arrange
            var databaseName = "TestDb";
            var procedureName = "TestProc";
            var parameters = "{}";
            var expectedErrorMessage = "Error executing stored procedure";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ExecuteStoredProcedureAsync(
                databaseName,
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(), 
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerExecuteStoredProcedureTool(mockServerDatabase.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedureInDatabase(databaseName, procedureName, parameters);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
        
        [Fact(DisplayName = "SESPT-006: ServerExecuteStoredProcedureTool handles invalid JSON for parameters")]
        public async Task SESPT006()
        {
            // Arrange
            var databaseName = "TestDb";
            var procedureName = "TestProc";
            var invalidJson = "{invalid json"; // Invalid JSON string
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            
            var tool = new ServerExecuteStoredProcedureTool(mockServerDatabase.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedureInDatabase(databaseName, procedureName, invalidJson);
            
            // Assert
            result.Should().Contain("Error parsing parameters");
        }
    }
}