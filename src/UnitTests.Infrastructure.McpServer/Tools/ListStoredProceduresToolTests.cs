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
            Action act = () => new ListStoredProceduresTool(nullContext);
            
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
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyProcedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.ListStoredProcedures();
            
            // Assert
            result.Should().Contain("No stored procedures found");
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<CancellationToken>()), Times.Once);
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
            
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(procedureList);
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.ListStoredProcedures();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("GetUser");
            result.Should().Contain("CreateUser");
            result.Should().Contain("Available Stored Procedures:");
            mockDatabaseContext.Verify(x => x.ListStoredProceduresAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "LSPT-004: ListStoredProcedures handles exception from database context")]
        public async Task LSPT004()
        {
            // Arrange
            var expectedErrorMessage = "Database connection failed";
            
            var mockDatabaseContext = new Mock<IDatabaseContext>();
            mockDatabaseContext.Setup(x => x.ListStoredProceduresAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ListStoredProceduresTool(mockDatabaseContext.Object);
            
            // Act
            var result = await tool.ListStoredProcedures();
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}