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
            Action act = () => new GetStoredProcedureDefinitionTool(nullContext);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "GSPDT-002: GetStoredProcedureDefinition returns error for empty procedure name")]
        public async Task GSPDT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
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
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
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
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
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
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(definition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain($"Definition for stored procedure '{procedureName}':");
            result.Should().Contain(definition);
            mockDatabaseContext.Verify(x => x.GetStoredProcedureDefinitionAsync(
                procedureName,
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
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyDefinition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
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
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(whitespaceDefinition);
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
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
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new GetStoredProcedureDefinitionTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.GetStoredProcedureDefinition(procedureName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}