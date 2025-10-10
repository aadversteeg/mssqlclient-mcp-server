using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class ServerGetStoredProcedureParametersToolTests
    {
        [Fact(DisplayName = "SGSPTPT-001: ServerGetStoredProcedureParametersTool constructor with null server database throws ArgumentNullException")]
        public void SGSPTPT001()
        {
            // Act
            IServerDatabase? nullServerDatabase = null;
            Action act = () => new ServerGetStoredProcedureParametersTool(nullServerDatabase, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SGSPTPT-002: GetStoredProcedureParameters returns error for empty procedure name")]
        public async Task SGSPTPT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(string.Empty);
            
            // Assert
            result.Should().Be("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPTPT-003: GetStoredProcedureParameters returns error for null procedure name")]
        public async Task SGSPTPT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(null);
            
            // Assert
            result.Should().Be("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPTPT-004: GetStoredProcedureParameters returns error for whitespace procedure name")]
        public async Task SGSPTPT004()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters("   ");
            
            // Assert
            result.Should().Be("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "SGSPTPT-005: GetStoredProcedureParameters returns no parameters message when procedure has no parameters")]
        public async Task SGSPTPT005()
        {
            // Arrange
            var procedureName = "dbo.NoParamsProcedure";
            var databaseName = "TestDB";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return no rows
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName);
            
            // Assert
            result.Should().Be($"Stored procedure '{procedureName}' has no parameters or does not exist in database '{databaseName}'.");
            
            // Verify database query was executed
            mockServerDatabase.Verify(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPTPT-006: GetStoredProcedureParameters returns table format by default")]
        public async Task SGSPTPT006()
        {
            // Arrange
            var procedureName = "TestProcedure";
            var databaseName = "TestDB";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return one parameter
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            mockReader.Setup(x => x.GetOrdinal("ParameterName")).Returns(0);
            mockReader.Setup(x => x.GetOrdinal("ParameterId")).Returns(1);
            mockReader.Setup(x => x.GetOrdinal("DataType")).Returns(2);
            mockReader.Setup(x => x.GetOrdinal("MaxLength")).Returns(3);
            mockReader.Setup(x => x.GetOrdinal("Precision")).Returns(4);
            mockReader.Setup(x => x.GetOrdinal("Scale")).Returns(5);
            mockReader.Setup(x => x.GetOrdinal("IsOutput")).Returns(6);
            mockReader.Setup(x => x.GetOrdinal("HasDefaultValue")).Returns(7);
            mockReader.Setup(x => x.GetOrdinal("DefaultValue")).Returns(8);
            
            mockReader.Setup(x => x.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync("@UserId");
            mockReader.Setup(x => x.GetFieldValueAsync<int>(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            mockReader.Setup(x => x.GetFieldValueAsync<string>(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync("int");
            mockReader.Setup(x => x.GetFieldValueAsync<short>(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync((short)4);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(4, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)10);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)0);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(6, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.IsDBNullAsync(8, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName);
            
            // Assert
            result.Should().Contain("Parameters for stored procedure:");
            result.Should().Contain("| Parameter | Type | Required | Direction | Default |");
            result.Should().Contain("UserId");
            result.Should().Contain("Example usage:");
            
            // Verify reader was disposed
            mockReader.Verify(x => x.Dispose(), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPTPT-007: GetStoredProcedureParameters returns JSON format when requested")]
        public async Task SGSPTPT007()
        {
            // Arrange
            var procedureName = "TestProcedure";
            var databaseName = "TestDB";
            var format = "json";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return one parameter
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            mockReader.Setup(x => x.GetOrdinal("ParameterName")).Returns(0);
            mockReader.Setup(x => x.GetOrdinal("ParameterId")).Returns(1);
            mockReader.Setup(x => x.GetOrdinal("DataType")).Returns(2);
            mockReader.Setup(x => x.GetOrdinal("MaxLength")).Returns(3);
            mockReader.Setup(x => x.GetOrdinal("Precision")).Returns(4);
            mockReader.Setup(x => x.GetOrdinal("Scale")).Returns(5);
            mockReader.Setup(x => x.GetOrdinal("IsOutput")).Returns(6);
            mockReader.Setup(x => x.GetOrdinal("HasDefaultValue")).Returns(7);
            mockReader.Setup(x => x.GetOrdinal("DefaultValue")).Returns(8);
            
            mockReader.Setup(x => x.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync("@UserId");
            mockReader.Setup(x => x.GetFieldValueAsync<int>(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            mockReader.Setup(x => x.GetFieldValueAsync<string>(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync("int");
            mockReader.Setup(x => x.GetFieldValueAsync<short>(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync((short)4);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(4, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)10);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)0);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(6, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.IsDBNullAsync(8, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName, format);
            
            // Assert
            result.Should().Contain("\"procedureName\":");
            result.Should().Contain("\"parameters\":");
            result.Should().Contain("\"type\": \"object\"");
            result.Should().Contain("UserId");
            
            // Verify reader was disposed
            mockReader.Verify(x => x.Dispose(), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPTPT-008: GetStoredProcedureParameters returns error for unsupported format")]
        public async Task SGSPTPT008()
        {
            // Arrange
            var procedureName = "TestProcedure";
            var databaseName = "TestDB";
            var invalidFormat = "xml";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return one parameter
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);
            
            mockReader.Setup(x => x.GetOrdinal(It.IsAny<string>())).Returns(0);
            mockReader.Setup(x => x.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync("@UserId");
            mockReader.Setup(x => x.GetFieldValueAsync<int>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            mockReader.Setup(x => x.GetFieldValueAsync<short>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync((short)4);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)10);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.IsDBNullAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName, invalidFormat);
            
            // Assert
            result.Should().Be($"Error: Unsupported format '{invalidFormat}'. Use 'table' or 'json'.");
        }
        
        [Fact(DisplayName = "SGSPTPT-009: GetStoredProcedureParameters handles schema-qualified procedure names")]
        public async Task SGSPTPT009()
        {
            // Arrange
            var procedureName = "custom.TestProcedure";
            var databaseName = "TestDB";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return no rows to verify query was called with correct schema
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName);
            
            // Assert
            result.Should().Contain($"Stored procedure '{procedureName}' has no parameters or does not exist");
            
            // Verify the query was executed with the schema-qualified name in the WHERE clause
            mockServerDatabase.Verify(x => x.ExecuteQueryInDatabaseAsync(
                databaseName, 
                It.Is<string>(query => query.Contains("WHERE s.name = 'custom' AND sp.name = 'TestProcedure'")), 
                It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }
        
        [Fact(DisplayName = "SGSPTPT-010: GetStoredProcedureParameters handles exception from server database")]
        public async Task SGSPTPT010()
        {
            // Arrange
            var procedureName = "TestProcedure";
            var databaseName = "TestDB";
            var expectedErrorMessage = "Database connection failed";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain("getting stored procedure parameters");
        }
        
        [Fact(DisplayName = "SGSPTPT-011: GetStoredProcedureParameters uses master database when no database specified")]
        public async Task SGSPTPT011()
        {
            // Arrange
            var procedureName = "TestProcedure";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync("master", It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName);
            
            // Assert
            result.Should().Contain("Stored procedure 'TestProcedure' has no parameters or does not exist in database 'current'");
            
            // Verify master database was used when no database was specified
            mockServerDatabase.Verify(x => x.ExecuteQueryInDatabaseAsync("master", It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SGSPTPT-012: GetStoredProcedureParameters skips return value parameter")]
        public async Task SGSPTPT012()
        {
            // Arrange
            var procedureName = "TestProcedure";
            var databaseName = "TestDB";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return return value parameter (empty name, parameterId = 0) followed by real parameter
            mockReader.SetupSequence(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)   // Return value parameter
                .ReturnsAsync(true)   // Real parameter
                .ReturnsAsync(false); // End
            
            mockReader.Setup(x => x.GetOrdinal("ParameterName")).Returns(0);
            mockReader.Setup(x => x.GetOrdinal("ParameterId")).Returns(1);
            mockReader.Setup(x => x.GetOrdinal("DataType")).Returns(2);
            mockReader.Setup(x => x.GetOrdinal("MaxLength")).Returns(3);
            mockReader.Setup(x => x.GetOrdinal("Precision")).Returns(4);
            mockReader.Setup(x => x.GetOrdinal("Scale")).Returns(5);
            mockReader.Setup(x => x.GetOrdinal("IsOutput")).Returns(6);
            mockReader.Setup(x => x.GetOrdinal("HasDefaultValue")).Returns(7);
            mockReader.Setup(x => x.GetOrdinal("DefaultValue")).Returns(8);
            
            // First call (return value parameter - should be skipped)
            mockReader.SetupSequence(x => x.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync("")          // Return value has empty name
                .ReturnsAsync("@UserId");  // Real parameter
            
            mockReader.SetupSequence(x => x.GetFieldValueAsync<int>(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(0)  // Return value has parameterId = 0
                .ReturnsAsync(1); // Real parameter
            
            // Setup other fields for the real parameter
            mockReader.Setup(x => x.GetFieldValueAsync<string>(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync("int");
            mockReader.Setup(x => x.GetFieldValueAsync<short>(3, It.IsAny<CancellationToken>()))
                .ReturnsAsync((short)4);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(4, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)10);
            mockReader.Setup(x => x.GetFieldValueAsync<byte>(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte)0);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(6, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.GetFieldValueAsync<bool>(7, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            mockReader.Setup(x => x.IsDBNullAsync(8, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            mockServerDatabase.Setup(x => x.ExecuteQueryInDatabaseAsync(databaseName, It.IsAny<string>(), It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var tool = new ServerGetStoredProcedureParametersTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, databaseName);
            
            // Assert
            result.Should().Contain("UserId");  // Should contain the real parameter
            result.Should().Contain("Parameters for stored procedure:");
            result.Should().Contain("| Parameter | Type | Required | Direction | Default |");
            
            // Verify reader was called twice (once for return value, once for real parameter)
            mockReader.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
    }
}


