using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class ExecuteStoredProcedureToolTests
    {
        [Fact(DisplayName = "ESPT-001: ExecuteStoredProcedureTool constructor with null database context throws ArgumentNullException")]
        public void ESPT001()
        {
            // Arrange
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new ExecuteStoredProcedureTool(nullContext, mockOptions.Object);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "ESPT-002: ExecuteStoredProcedureTool returns error for empty procedure name")]
        public async Task ESPT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteStoredProcedureTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedure(string.Empty, "{}");
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "ESPT-003: ExecuteStoredProcedureTool executes stored procedure with parameters")]
        public async Task ESPT003()
        {
            // Arrange
            var procedureName = "TestProc";
            var parameters = new Dictionary<string, object?>
            {
                { "Param1", 123 },
                { "Param2", "test" }
            };
            var parametersJson = JsonSerializer.Serialize(parameters);
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return a simple result
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false); // No rows to read
            mockReader.Setup(x => x.FieldCount)
                .Returns(0);
            
            mockDatabaseContext.Setup(x => x.ExecuteStoredProcedureAsync(
                procedureName, 
                It.Is<Dictionary<string, object?>>(p => 
                    p.Count == parameters.Count && 
                    p.ContainsKey("Param1") && 
                    p.ContainsKey("Param2")
                ), 
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteStoredProcedureTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedure(procedureName, parametersJson);
            
            // Assert
            result.Should().NotBeNull();
            mockDatabaseContext.Verify(x => x.ExecuteStoredProcedureAsync(
                procedureName,
                It.IsAny<Dictionary<string, object?>>(),
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "ESPT-004: ExecuteStoredProcedureTool handles exception from database context")]
        public async Task ESPT004()
        {
            // Arrange
            var procedureName = "TestProc";
            var parameters = "{}";
            var expectedErrorMessage = "Error executing stored procedure";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ExecuteStoredProcedureAsync(
                procedureName, 
                It.IsAny<Dictionary<string, object?>>(), 
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteStoredProcedureTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedure(procedureName, parameters);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
        
        [Fact(DisplayName = "ESPT-005: ExecuteStoredProcedureTool handles invalid JSON for parameters")]
        public async Task ESPT005()
        {
            // Arrange
            var procedureName = "TestProc";
            var invalidJson = "{invalid json"; // Invalid JSON string
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            
            var mockOptions = new Mock<IOptions<DatabaseConfiguration>>();
            mockOptions.Setup(o => o.Value).Returns(new DatabaseConfiguration { TotalToolCallTimeoutSeconds = null });
            var tool = new ExecuteStoredProcedureTool(mockDatabaseContext.Object, mockOptions.Object);
            
            // Act
            var result = await tool.ExecuteStoredProcedure(procedureName, invalidJson);
            
            // Assert
            result.Should().Contain("Error parsing parameters");
        }
    }
}