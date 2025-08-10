using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Models;
using Core.Infrastructure.McpServer.Tools;
using Core.Infrastructure.SqlClient;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Tools
{
    public class ServerCapabilitiesToolTests
    {
        [Fact(DisplayName = "SCT-001: ServerCapabilitiesTool returns capabilities in server mode")]
        public async Task SCT001()
        {
            // Arrange
            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>();
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SqlServerCapability
                {
                    Version = "Microsoft SQL Server 2019",
                    MajorVersion = 15,
                    MinorVersion = 0,
                    BuildNumber = 4123,
                    Edition = "Enterprise Edition",
                    SupportsExactRowCount = true,
                    SupportsDetailedIndexMetadata = true,
                    IsOnPremisesSqlServer = true
                });
            
            var mockDatabaseService = new Mock<IDatabaseService>();
            mockDatabaseService.Setup(x => x.GetCurrentDatabaseName())
                .Returns(""); // Empty string for server mode
            
            var tool = new ServerCapabilitiesTool(mockCapabilityDetector.Object, mockDatabaseService.Object);
            
            // Act
            var result = await tool.GetServerCapabilitiesAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.ToolMode.Should().Be("server");
            result.DatabaseName.Should().BeNull();
            result.MajorVersion.Should().Be(15);
            result.Version.Should().Be("Microsoft SQL Server 2019");
            result.Features["SupportsExactRowCount"].Should().BeTrue();
            result.Features["SupportsDetailedIndexMetadata"].Should().BeTrue();
            result.IsOnPremisesSqlServer.Should().BeTrue();
        }
        
        [Fact(DisplayName = "SCT-002: ServerCapabilitiesTool returns capabilities in database mode")]
        public async Task SCT002()
        {
            // Arrange
            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>();
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SqlServerCapability
                {
                    Version = "Microsoft SQL Server 2019",
                    MajorVersion = 15,
                    MinorVersion = 0,
                    BuildNumber = 4123,
                    Edition = "Enterprise Edition",
                    SupportsExactRowCount = true,
                    SupportsDetailedIndexMetadata = true,
                    IsOnPremisesSqlServer = true
                });
            
            var mockDatabaseService = new Mock<IDatabaseService>();
            mockDatabaseService.Setup(x => x.GetCurrentDatabaseName())
                .Returns("TestDatabase"); // Database name for database mode
            
            var tool = new ServerCapabilitiesTool(mockCapabilityDetector.Object, mockDatabaseService.Object);
            
            // Act
            var result = await tool.GetServerCapabilitiesAsync();
            
            // Assert
            result.Should().NotBeNull();
            result.ToolMode.Should().Be("database");
            result.DatabaseName.Should().Be("TestDatabase");
            result.MajorVersion.Should().Be(15);
            result.Version.Should().Be("Microsoft SQL Server 2019");
            result.Features["SupportsExactRowCount"].Should().BeTrue();
            result.Features["SupportsDetailedIndexMetadata"].Should().BeTrue();
            result.IsOnPremisesSqlServer.Should().BeTrue();
        }
    }
}


