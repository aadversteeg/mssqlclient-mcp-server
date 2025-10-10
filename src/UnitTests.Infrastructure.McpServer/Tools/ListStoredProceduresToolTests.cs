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
    public class ListStoredProceduresToolTests
    {
        [Fact(DisplayName = "LSPT-001: ListStoredProceduresTool constructor with null database context throws ArgumentNullException")]
        public void LSPT001()
        {
            // Act
            IDatabaseContext? nullContext = null;
            Action act = () => new ListStoredProceduresTool(nullContext, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("databaseContext");
        }
        
        [Fact(DisplayName = "LSPT-002: ListStoredProcedures returns message when no procedures exist")]
        public async Task LSPT002()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var emptyProcedureList = new List<StoredProcedureInfo>();
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyProcedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProcedures();
            
            // Assert
            result.Should().Contain("No stored procedures found");
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LSPT-003: ListStoredProcedures returns formatted procedure list")]
        public async Task LSPT003()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var parameters = new List<StoredProcedureParameterInfo>
            {
                new StoredProcedureParameterInfo("@UserId", "int", 4, 0, 0, false, false, null),
                new StoredProcedureParameterInfo("@UserName", "varchar", 255, 0, 0, false, true, null)
            };
            
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "GetUser",
                    CreateDate: new DateTime(2023, 1, 1),
                    ModifyDate: new DateTime(2023, 1, 1),
                    Owner: "dbo",
                    Parameters: parameters,
                    IsFunction: false,
                    LastExecutionTime: new DateTime(2023, 12, 1, 10, 30, 0),
                    ExecutionCount: 100,
                    AverageDurationMs: 50
                ),
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "CreateUser",
                    CreateDate: new DateTime(2023, 1, 2),
                    ModifyDate: new DateTime(2023, 1, 2),
                    Owner: "dbo",
                    Parameters: new List<StoredProcedureParameterInfo>(),
                    IsFunction: false,
                    LastExecutionTime: null,
                    ExecutionCount: null,
                    AverageDurationMs: null
                )
            };
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProcedures();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("GetUser");
            result.Should().Contain("CreateUser");
            result.Should().Contain("Available Stored Procedures:");
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LSPT-004: ListStoredProcedures handles exception from database context")]
        public async Task LSPT004()
        {
            // Arrange
            var expectedErrorMessage = "Database connection failed";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProcedures();
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
        
        [Fact(DisplayName = "LSPT-005: ListStoredProcedures with timeout parameter passes through correctly")]
        public async Task LSPT005()
        {
            // Arrange
            var timeoutSeconds = 60;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var parameters = new List<StoredProcedureParameterInfo>
            {
                new StoredProcedureParameterInfo("@UserId", "int", 4, 0, 0, false, false, null)
            };
            
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "dbo",
                    Name: "GetUserById",
                    CreateDate: new DateTime(2023, 1, 1),
                    ModifyDate: new DateTime(2023, 1, 1),
                    Owner: "dbo",
                    Parameters: parameters,
                    IsFunction: false,
                    LastExecutionTime: new DateTime(2023, 12, 1, 10, 30, 0),
                    ExecutionCount: 50,
                    AverageDurationMs: 25
                )
            };
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProcedures(timeoutSeconds);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("GetUserById");
            
            // Verify the timeout parameter was passed through correctly
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), timeoutSeconds, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LSPT-006: ListStoredProcedures with null timeout uses default")]
        public async Task LSPT006()
        {
            // Arrange
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var procedureList = new List<StoredProcedureInfo>();
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProcedures(null);
            
            // Assert
            result.Should().NotBeNull();
            
            // Verify null timeout was passed through
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), null, It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LSPT-007: ListStoredProcedures verifies timeout parameter is passed through correctly")]
        public async Task LSPT007()
        {
            // Arrange
            var specificTimeout = 75;
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            var parameters = new List<StoredProcedureParameterInfo>
            {
                new StoredProcedureParameterInfo("@OrderId", "int", 4, 0, 0, false, false, null),
                new StoredProcedureParameterInfo("@Status", "varchar", 50, 0, 0, false, true, null)
            };
            
            var procedureList = new List<StoredProcedureInfo>
            {
                new StoredProcedureInfo(
                    SchemaName: "sales",
                    Name: "UpdateOrderStatus",
                    CreateDate: new DateTime(2023, 2, 1),
                    ModifyDate: new DateTime(2023, 2, 1),
                    Owner: "dbo",
                    Parameters: parameters,
                    IsFunction: false,
                    LastExecutionTime: new DateTime(2023, 12, 1, 14, 15, 0),
                    ExecutionCount: 200,
                    AverageDurationMs: 15
                )
            };
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), specificTimeout, It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.ListStoredProcedures(specificTimeout);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("UpdateOrderStatus");
            
            // Verify the exact timeout value was passed
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), specificTimeout, It.IsAny<CancellationToken>()), Times.Once);
            
            // Verify it was not called with any other timeout value
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.Is<int?>(t => t != specificTimeout), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}