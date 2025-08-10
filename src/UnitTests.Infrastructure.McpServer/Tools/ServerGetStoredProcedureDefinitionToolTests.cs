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
    public class ServerGetStoredProcedureDefinitionToolTests
    {
        [Fact(DisplayName = "SGSPDT-001: ServerGetStoredProcedureDefinitionTool constructor with null server database throws ArgumentNullException")]
        public void SGSPDT001()
        {
            // Act
            IServerDatabase? nullServerDatabase = null;
            Action act = () => new ServerGetStoredProcedureDefinitionTool(nullServerDatabase, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SGSPDT-002: GetStoredProcedureDefinitionInDatabase returns error for empty database name")]
        public async Task SGSPDT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(string.Empty, "TestProcedure");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPDT-003: GetStoredProcedureDefinitionInDatabase returns error for null database name")]
        public async Task SGSPDT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(null, "TestProcedure");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPDT-004: GetStoredProcedureDefinitionInDatabase returns error for whitespace database name")]
        public async Task SGSPDT004()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase("   ", "TestProcedure");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPDT-005: GetStoredProcedureDefinitionInDatabase returns error for empty procedure name")]
        public async Task SGSPDT005()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase("TestDB", string.Empty);
            
            // Assert
            result.Should().Be("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPDT-006: GetStoredProcedureDefinitionInDatabase returns error for null procedure name")]
        public async Task SGSPDT006()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase("TestDB", null);
            
            // Assert
            result.Should().Be("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPDT-007: GetStoredProcedureDefinitionInDatabase returns error for whitespace procedure name")]
        public async Task SGSPDT007()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase("TestDB", "   ");
            
            // Assert
            result.Should().Be("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPDT-008: GetStoredProcedureDefinitionInDatabase returns error when database does not exist")]
        public async Task SGSPDT008()
        {
            // Arrange
            var databaseName = "NonExistentDB";
            var procedureName = "TestProcedure";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().Be($"Error: Database '{databaseName}' does not exist or is not accessible");
            
            // Verify database existence was checked
            mockServerDatabase.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPDT-009: GetStoredProcedureDefinitionInDatabase returns formatted definition when procedure exists")]
        public async Task SGSPDT009()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "GetUserById";
            var expectedDefinition = "CREATE PROCEDURE GetUserById\n@UserId INT\nAS\nBEGIN\n    SELECT * FROM Users WHERE Id = @UserId\nEND";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.GetStoredProcedureDefinitionAsync(databaseName, procedureName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDefinition);
            
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().StartWith($"Definition for stored procedure '{procedureName}' in database '{databaseName}':");
            result.Should().Contain(expectedDefinition);
            
            // Verify both database existence and definition retrieval were called
            mockServerDatabase.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockServerDatabase.Verify(x => x.GetStoredProcedureDefinitionAsync(databaseName, procedureName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPDT-010: GetStoredProcedureDefinitionInDatabase returns helpful message when definition is empty")]
        public async Task SGSPDT010()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "NonExistentProcedure";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.GetStoredProcedureDefinitionAsync(databaseName, procedureName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);
            
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().Be($"No definition found for stored procedure '{procedureName}' in database '{databaseName}'. The procedure might not exist or you don't have permission to view its definition.");
            
            // Verify both methods were called
            mockServerDatabase.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockServerDatabase.Verify(x => x.GetStoredProcedureDefinitionAsync(databaseName, procedureName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPDT-011: GetStoredProcedureDefinitionInDatabase handles exception from database existence check")]
        public async Task SGSPDT011()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "TestProcedure";
            var expectedErrorMessage = "Database connection failed";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain($"getting definition for stored procedure '{procedureName}' in database '{databaseName}'");
        }
        
        [Fact(DisplayName = "SGSPDT-012: GetStoredProcedureDefinitionInDatabase handles exception from definition retrieval")]
        public async Task SGSPDT012()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureName = "TestProcedure";
            var expectedErrorMessage = "Procedure access denied";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.GetStoredProcedureDefinitionAsync(databaseName, procedureName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException(expectedErrorMessage));
            
            var tool = new ServerGetStoredProcedureDefinitionTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureDefinitionInDatabase(databaseName, procedureName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain($"getting definition for stored procedure '{procedureName}' in database '{databaseName}'");
        }
    }
}


