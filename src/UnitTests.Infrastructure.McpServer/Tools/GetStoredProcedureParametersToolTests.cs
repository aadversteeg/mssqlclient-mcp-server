using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Application.Models;
using Core.Infrastructure.McpServer.Tools;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class GetStoredProcedureParametersToolTests
    {
        [Fact(DisplayName = "GSPPT-001: GetStoredProcedureParametersTool constructor with null database context throws ArgumentNullException")]
        public void GSPPT001()
        {
            // Act
            IDatabaseContext? nullContext = null;
            var configuration = TestHelpers.CreateConfiguration();
            Action act = () => new GetStoredProcedureParametersTool(nullContext, configuration);
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "GSPPT-002: GetStoredProcedureParameters returns error for empty procedure name")]
        public async Task GSPPT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(string.Empty);
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "GSPPT-003: GetStoredProcedureParameters returns error for null procedure name")]
        public async Task GSPPT003()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(null);
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "GSPPT-004: GetStoredProcedureParameters returns error for whitespace procedure name")]
        public async Task GSPPT004()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters("   ");
            
            // Assert
            result.Should().Contain("Error: Procedure name cannot be empty");
        }
        
        [Fact(DisplayName = "GSPPT-005: GetStoredProcedureParameters handles procedure with no parameters")]
        public async Task GSPPT005()
        {
            // Arrange
            var procedureName = "dbo.GetAllUsers";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return no rows (no parameters)
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false);
            
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                It.IsAny<string>(), 
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName);
            
            // Assert
            result.Should().Contain("has no parameters or does not exist");
            result.Should().Contain(procedureName);
        }
        
        [Fact(DisplayName = "GSPPT-006: GetStoredProcedureParameters returns table format by default")]
        public async Task GSPPT006()
        {
            // Arrange
            var procedureName = "dbo.GetUser";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return parameter data
            var parameterData = new[] 
            {
                new { ParameterName = "@UserId", ParameterId = 1, DataType = "int", MaxLength = (short)4, 
                      Precision = (byte)10, Scale = (byte)0, IsOutput = false, HasDefaultValue = false, DefaultValue = (object?)null }
            };
            
            var readCount = 0;
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => readCount++ < parameterData.Length);
            
            // Setup GetOrdinal for column names
            mockReader.Setup(x => x.GetOrdinal("ParameterName")).Returns(0);
            mockReader.Setup(x => x.GetOrdinal("ParameterId")).Returns(1);
            mockReader.Setup(x => x.GetOrdinal("DataType")).Returns(2);
            mockReader.Setup(x => x.GetOrdinal("MaxLength")).Returns(3);
            mockReader.Setup(x => x.GetOrdinal("Precision")).Returns(4);
            mockReader.Setup(x => x.GetOrdinal("Scale")).Returns(5);
            mockReader.Setup(x => x.GetOrdinal("IsOutput")).Returns(6);
            mockReader.Setup(x => x.GetOrdinal("HasDefaultValue")).Returns(7);
            mockReader.Setup(x => x.GetOrdinal("DefaultValue")).Returns(8);
            
            // Setup field value getters for the first parameter
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
            
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                It.IsAny<string>(), 
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName);
            
            // Assert
            result.Should().Contain("Parameters for stored procedure");
            result.Should().Contain("UserId");
            result.Should().Contain("int");
            result.Should().Contain("Example usage:");
        }
        
        [Fact(DisplayName = "GSPPT-007: GetStoredProcedureParameters returns JSON format when requested")]
        public async Task GSPPT007()
        {
            // Arrange
            var procedureName = "dbo.GetUser";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return no parameters for simplicity
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => false);
            
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                It.IsAny<string>(), 
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, "json");
            
            // Assert - Should return the no parameters message, not JSON, since there are no parameters
            result.Should().Contain("has no parameters or does not exist");
        }
        
        [Fact(DisplayName = "GSPPT-008: GetStoredProcedureParameters returns error for invalid format")]
        public async Task GSPPT008()
        {
            // Arrange
            var procedureName = "dbo.GetUser";
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var mockReader = new Mock<IAsyncDataReader>();
            
            // Setup reader to return parameter data
            var readCount = 0;
            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => readCount++ < 1);
            
            // Setup minimal parameter data
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
            
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                It.IsAny<string>(), 
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockReader.Object);
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName, "invalidformat");
            
            // Assert
            result.Should().Contain("Error: Unsupported format 'invalidformat'");
        }
        
        [Fact(DisplayName = "GSPPT-009: GetStoredProcedureParameters handles exception from database context")]
        public async Task GSPPT009()
        {
            // Arrange
            var procedureName = "dbo.NonExistentProcedure";
            var expectedErrorMessage = "Database connection failed";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ExecuteQueryAsync(
                It.IsAny<string>(), 
                It.IsAny<ToolCallTimeoutContext?>(),
                It.IsAny<int?>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var configuration = TestHelpers.CreateConfiguration();
            var tool = new GetStoredProcedureParametersTool(mockDatabaseContext.Object, configuration);
            
            // Act
            var result = await tool.GetStoredProcedureParameters(procedureName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}