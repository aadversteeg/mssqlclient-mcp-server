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
    public class ServerListDatabasesToolTests
    {
        [Fact(DisplayName = "SLDT-001: ServerListDatabasesTool constructor with null server database throws ArgumentNullException")]
        public void SLDT001()
        {
            // Act
            IServerDatabase? nullServerDatabase = null;
            Action act = () => new ServerListDatabasesTool(nullServerDatabase, TestHelpers.CreateConfiguration());
            
            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("serverDatabase");
        }
        
        [Fact(DisplayName = "SLDT-002: GetDatabases returns empty list when no databases exist")]
        public async Task SLDT002()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var emptyDatabaseList = new List<DatabaseInfo>();
            
            mockServerDatabase.Setup(x => x.ListDatabasesAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyDatabaseList);
            
            var tool = new ServerListDatabasesTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetDatabases();
            
            // Assert
            result.Should().NotBeNull();
            mockServerDatabase.Verify(x => x.ListDatabasesAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLDT-003: GetDatabases returns formatted database list")]
        public async Task SLDT003()
        {
            // Arrange
            var mockServerDatabase = new Mock<IServerDatabase>();
            var databaseList = new List<DatabaseInfo>
            {
                new DatabaseInfo(
                    Name: "master",
                    State: "ONLINE",
                    SizeMB: 10.5,
                    Owner: "sa",
                    CompatibilityLevel: "160",
                    CollationName: "SQL_Latin1_General_CP1_CI_AS",
                    CreateDate: new DateTime(2023, 1, 1),
                    RecoveryModel: "SIMPLE",
                    IsReadOnly: false
                ),
                new DatabaseInfo(
                    Name: "TestDB",
                    State: "ONLINE",
                    SizeMB: 100.0,
                    Owner: "dbo",
                    CompatibilityLevel: "160",
                    CollationName: "SQL_Latin1_General_CP1_CI_AS",
                    CreateDate: new DateTime(2023, 6, 1),
                    RecoveryModel: "FULL",
                    IsReadOnly: false
                )
            };
            
            mockServerDatabase.Setup(x => x.ListDatabasesAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(databaseList);
            
            var tool = new ServerListDatabasesTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetDatabases();
            
            // Assert
            result.Should().NotBeNull();
            result.Should().Contain("master");
            result.Should().Contain("TestDB");
            result.Should().Contain("ONLINE");
            mockServerDatabase.Verify(x => x.ListDatabasesAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
        
        [Fact(DisplayName = "SLDT-004: GetDatabases handles exception from server database")]
        public async Task SLDT004()
        {
            // Arrange
            var expectedErrorMessage = "Server connection failed";
            
            var mockServerDatabase = new Mock<IServerDatabase>();
            mockServerDatabase.Setup(x => x.ListDatabasesAsync(It.IsAny<Core.Application.Models.ToolCallTimeoutContext?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException(expectedErrorMessage));
            
            var tool = new ServerListDatabasesTool(mockServerDatabase.Object, TestHelpers.CreateConfiguration());
            
            // Act
            var result = await tool.GetDatabases();
            
            // Assert
            result.Should().Contain(expectedErrorMessage);
        }
    }
}



