using System;
using System.Collections.Generic;
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
    public class ServerListStoredProceduresToolTests
    {
        [Fact(DisplayName = "SLSPT-001: ServerListStoredProceduresTool constructor with null server database throws ArgumentNullException")]
        public void SLSPT001()
        {
            // Act
            IServerDatabase? nullServerDatabase = null;
            Action act = () => new ServerListStoredProceduresTool(nullServerDatabase, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SLSPT-002: ListStoredProceduresInDatabase returns error for empty database name")]
        public async Task SLSPT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(string.Empty);
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SLSPT-003: ListStoredProceduresInDatabase returns error for null database name")]
        public async Task SLSPT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(null);
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SLSPT-004: ListStoredProceduresInDatabase returns error for whitespace database name")]
        public async Task SLSPT004()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase("   ");
            
            // Assert
            result.Should().Be("Error: Database name cannot be empty");
        }
        
        [Fact(DisplayName = "SLSPT-005: ListStoredProceduresInDatabase returns error when database does not exist")]
        public async Task SLSPT005()
        {
            // Arrange
            var databaseName = "NonExistentDB";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Be($"Error: Database '{databaseName}' does not exist or is not accessible");
            
            // Verify database existence was checked
            mockServerDatabase.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLSPT-006: ListStoredProceduresInDatabase returns message when no procedures exist")]
        public async Task SLSPT006()
        {
            // Arrange
            var databaseName = "EmptyDB";
            var emptyProcedureList = new List<StoredProcedureInfo>();
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyProcedureList);
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Be($"No stored procedures found in the database '{databaseName}'.");
            
            // Verify both methods were called
            mockServerDatabase.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockServerDatabase.Verify(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLSPT-007: ListStoredProceduresInDatabase returns formatted table when procedures exist")]
        public async Task SLSPT007()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "GetUserById",
                    CreateDate: new DateTime(2023, 1, 1, 10, 0, 0),
                    ModifyDate: new DateTime(2023, 6, 1, 14, 30, 0),
                    Owner: "dbo",
                    Parameters: new List<StoredProcedureParameterInfo>(),
                    IsFunction: false,
                    LastExecutionTime: new DateTime(2024, 1, 1, 9, 0, 0),
                    ExecutionCount: 150,
                    AverageDurationMs: 25
                ),
                new StoredProcedureInfo(
                    SchemaName: "sales",
                    Name: "ProcessOrder",
                    CreateDate: new DateTime(2023, 3, 15, 8, 30, 0),
                    ModifyDate: new DateTime(2023, 8, 20, 16, 45, 0),
                    Owner: "sales",
                    Parameters: new List<StoredProcedureParameterInfo>(),
                    IsFunction: false,
                    LastExecutionTime: null,
                    ExecutionCount: null,
                    AverageDurationMs: null
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Contain($"Available Stored Procedures in '{databaseName}':");
            result.Should().Contain("Schema   | Procedure Name");
            result.Should().Contain("dbo");
            result.Should().Contain("GetUserById");
            result.Should().Contain("sales");
            result.Should().Contain("ProcessOrder");
            result.Should().Contain("2023-01-01 10:00:00"); // Create date
            result.Should().Contain("2024-01-01 09:00:00"); // Last execution
            result.Should().Contain("150"); // Execution count
            result.Should().Contain("N/A"); // For null values
            
            // Verify both methods were called
            mockServerDatabase.Verify(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
            mockServerDatabase.Verify(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLSPT-008: ListStoredProceduresInDatabase handles exception from database existence check")]
        public async Task SLSPT008()
        {
            // Arrange
            var databaseName = "TestDB";
            var expectedErrorMessage = "Database connection failed";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain($"listing stored procedures in database '{databaseName}'");
        }
        
        [Fact(DisplayName = "SLSPT-009: ListStoredProceduresInDatabase handles exception from procedure listing")]
        public async Task SLSPT009()
        {
            // Arrange
            var databaseName = "TestDB";
            var expectedErrorMessage = "Access denied to system views";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException(expectedErrorMessage));
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
            result.Should().Contain($"listing stored procedures in database '{databaseName}'");
        }
        
        [Fact(DisplayName = "SLSPT-010: ListStoredProceduresInDatabase formats table headers correctly")]
        public async Task SLSPT010()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "TestProcedure",
                    CreateDate: DateTime.Now,
                    ModifyDate: DateTime.Now,
                    Owner: "dbo",
                    Parameters: new List<StoredProcedureParameterInfo>(),
                    IsFunction: false,
                    LastExecutionTime: DateTime.Now,
                    ExecutionCount: 1,
                    AverageDurationMs: 10
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Contain("Schema   | Procedure Name                  | Parameters | Last Execution    | Execution Count | Created Date");
            result.Should().Contain("-------- | ------------------------------- | ---------- | ----------------- | --------------- | -------------------");
        }
        
        [Fact(DisplayName = "SLSPT-011: ListStoredProceduresInDatabase handles procedures with no parameters")]
        public async Task SLSPT011()
        {
            // Arrange
            var databaseName = "TestDB";
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "NoParamsProcedure",
                    CreateDate: new DateTime(2023, 1, 1),
                    ModifyDate: new DateTime(2023, 1, 1),
                    Owner: "dbo",
                    Parameters: new List<StoredProcedureParameterInfo>(), // Empty parameters list
                    IsFunction: false,
                    LastExecutionTime: null,
                    ExecutionCount: null,
                    AverageDurationMs: null
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Contain("NoParamsProcedure");
            result.Should().Contain("0         "); // Zero parameters count, padded
            result.Should().Contain("N/A"); // For null execution stats
        }
        
        [Fact(DisplayName = "SLSPT-012: ListStoredProceduresInDatabase handles long procedure names properly")]
        public async Task SLSPT012()
        {
            // Arrange
            var databaseName = "TestDB";
            var longProcedureName = "VeryLongStoredProcedureNameThatExceedsTypicalLength";
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "custom",
                    Name: longProcedureName,
                    CreateDate: new DateTime(2023, 1, 1),
                    ModifyDate: new DateTime(2023, 1, 1),
                    Owner: "custom",
                    Parameters: new List<StoredProcedureParameterInfo>
                    {
                        new StoredProcedureParameterInfo("@param1", "int", 4, 10, 0, false, false, null),
                        new StoredProcedureParameterInfo("@param2", "varchar", 50, 0, 0, false, false, null),
                        new StoredProcedureParameterInfo("@param3", "datetime", 8, 23, 3, false, false, null)
                    },
                    IsFunction: false,
                    LastExecutionTime: new DateTime(2024, 1, 1),
                    ExecutionCount: 42,
                    AverageDurationMs: 100
                )
            };
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.DoesDatabaseExistAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mockServerDatabase.Setup(x => x.ListStoredProceduresAsync(databaseName, It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ServerListStoredProceduresTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProceduresInDatabase(databaseName);
            
            // Assert
            result.Should().Contain(longProcedureName);
            result.Should().Contain("custom");
            result.Should().Contain("3         "); // Three parameters count
            result.Should().Contain("42"); // Execution count
        }
    }
}


