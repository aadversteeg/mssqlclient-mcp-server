using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class GetStoredProcedureDefinitionToolTests
    {
        [Fact(DisplayName = "GSPDT-001: GetStoredProcedureDefinitionTool constructor with null database context throws ArgumentNullException")]
        public void GSPDT001()
        {
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new GetStoredProcedureDefinitionTool(nullContext, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "GSPDT-002: GetStoredProcedureDefinition returns error for empty procedure name")]
        public async Task GSPDT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(string.Empty);
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "GSPDT-003: GetStoredProcedureDefinition returns error for null procedure name")]
        public async Task GSPDT003()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(null);
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "GSPDT-004: GetStoredProcedureDefinition returns error for whitespace procedure name")]
        public async Task GSPDT004()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition("   ");
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "GSPDT-005: GetStoredProcedureDefinition returns formatted definition")]
        public async Task GSPDT005()
        {
            // Arrange
            var procedureName = "dbo.GetUsers";
            var definition = "CREATE PROCEDURE dbo.GetUsers AS BEGIN SELECT * FROM Users END";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(definition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain($"Definition for stored procedure '{procedureName}':");
            result.Should().Contain(definition);
            mockDatabaseContext.Verify(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "GSPDT-006: GetStoredProcedureDefinition handles empty definition")]
        public async Task GSPDT006()
        {
            // Arrange
            var procedureName = "dbo.NonExistentProcedure";
            var emptyDefinition = string.Empty;
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyDefinition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName);
            
            // Assert
            result.Should().Contain("No definition found");
            result.Should().Contain(procedureName);
        }
        
        [Fact(DisplayName = "GSPDT-007: GetStoredProcedureDefinition handles whitespace definition")]
        public async Task GSPDT007()
        {
            // Arrange
            var procedureName = "dbo.EmptyProcedure";
            var whitespaceDefinition = "   ";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(whitespaceDefinition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName);
            
            // Assert
            result.Should().Contain("No definition found");
            result.Should().Contain(procedureName);
        }
        
        [Fact(DisplayName = "GSPDT-008: GetStoredProcedureDefinition handles exception from database context")]
        public async Task GSPDT008()
        {
            // Arrange
            var procedureName = "dbo.TestProcedure";
            var expectedErrorMessage = "Procedure does not exist";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
        
        [Fact(DisplayName = "GSPDT-009: GetStoredProcedureDefinition with timeout parameter passes through correctly")]
        public async Task GSPDT009()
        {
            // Arrange
            var procedureName = "dbo.GetUserDetails";
            var timeoutSeconds = 45;
            var definition = "CREATE PROCEDURE dbo.GetUserDetails @UserId INT AS BEGIN SELECT * FROM Users WHERE Id = @UserId END";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                timeoutSeconds,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(definition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName, timeoutSeconds);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain($"Definition for stored procedure '{procedureName}':");
            result.Should().Contain(definition);
            
            // Verify the timeout parameter was passed through correctly
            mockDatabaseContext.Verify(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                timeoutSeconds,
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "GSPDT-010: GetStoredProcedureDefinition with null timeout uses default")]
        public async Task GSPDT010()
        {
            // Arrange
            var procedureName = "dbo.DeleteUser";
            var definition = "CREATE PROCEDURE dbo.DeleteUser @UserId INT AS BEGIN DELETE FROM Users WHERE Id = @UserId END";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                null,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(definition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName, null);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain($"Definition for stored procedure '{procedureName}':");
            result.Should().Contain(definition);
            
            // Verify null timeout was passed through
            mockDatabaseContext.Verify(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                null,
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "GSPDT-011: GetStoredProcedureDefinition verifies timeout parameter is passed through correctly")]
        public async Task GSPDT011()
        {
            // Arrange
            var procedureName = "sales.CalculateOrderTotal";
            var specificTimeout = 120;
            var definition = "CREATE PROCEDURE sales.CalculateOrderTotal @OrderId INT AS BEGIN SELECT SUM(Price * Quantity) FROM OrderItems WHERE OrderId = @OrderId END";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                specificTimeout,
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(definition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName, specificTimeout);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain($"Definition for stored procedure '{procedureName}':");
            result.Should().Contain(definition);
            
            // Verify the exact timeout value was passed
            mockDatabaseContext.Verify(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                specificTimeout,
                It.IsAny<CancellationToken>()), 
                Times.Once);
            
            // Verify it was not called with any other timeout value
            mockDatabaseContext.Verify(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.Is<int?>(t => t != specificTimeout),
                It.IsAny<CancellationToken>()), 
                Times.Never);
        }
    }
}