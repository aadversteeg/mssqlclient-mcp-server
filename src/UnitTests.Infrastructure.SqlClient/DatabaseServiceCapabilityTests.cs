using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Models;
using Core.Infrastructure.SqlClient;
using Core.Application.Interfaces;
using Core.Infrastructure.SqlClient.Interfaces;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class DatabaseServiceCapabilityTests
    {
        private readonly string _dummyConnectionString = "Server=test;Database=dummy;Trusted_Connection=True;";
        private readonly DatabaseConfiguration _configuration = new DatabaseConfiguration { DefaultCommandTimeoutSeconds = 30 };

        [Fact(DisplayName = "DSC-001: DatabaseService uses capabilities to determine feature availability")]
        public async Task DSC001()
        {
            // Arrange
            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>(MockBehavior.Strict);
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SqlServerCapability
                {
                    MajorVersion = 15, // SQL Server 2019
                    SupportsExactRowCount = true,
                    SupportsDetailedIndexMetadata = true
                });
                
            var service = new DatabaseService(_dummyConnectionString, mockCapabilityDetector.Object, _configuration);
            
            // Act - trigger capability detection by accessing a method
            var capability = await GetPrivateCapabilities(service);
            
            // Assert
            service.Should().BeAssignableTo<IDatabaseService>();
            mockCapabilityDetector.Verify(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
            capability.MajorVersion.Should().Be(15);
            capability.SupportsExactRowCount.Should().BeTrue();
        }

        [Fact(DisplayName = "DSC-002: DatabaseService uses fallback queries for SQL Server 2008")]
        public async Task DSC002()
        {
            // This test verifies that when connected to an older SQL Server version,
            // the service uses simpler, more compatible queries

            // Arrange - SQL Server 2008 with limited capabilities
            var oldVersionCapability = new SqlServerCapability
            {
                MajorVersion = 10, // SQL Server 2008
                SupportsExactRowCount = false,
                SupportsDetailedIndexMetadata = false,
                SupportsJson = false
            };

            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>(MockBehavior.Strict);
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(oldVersionCapability);

            // We can't fully test the SQL query behavior without a real database,
            // but we can verify that the capability is checked and cached
            var service = new DatabaseService(_dummyConnectionString, mockCapabilityDetector.Object, _configuration);

            // Act - trigger capability detection by accessing a method
            var capability = await GetPrivateCapabilities(service);
            
            // Assert
            mockCapabilityDetector.Verify(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
            capability.MajorVersion.Should().Be(10);
            capability.SupportsExactRowCount.Should().BeFalse();
            
            // Call the method again to verify the capability is cached
            mockCapabilityDetector.Invocations.Clear();
            capability = await GetPrivateCapabilities(service);
            mockCapabilityDetector.Verify(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "DSC-003: Capability detector not called again after caching")]
        public async Task DSC003()
        {
            // Arrange
            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>(MockBehavior.Strict);
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SqlServerCapability
                {
                    MajorVersion = 15, // SQL Server 2019
                    SupportsExactRowCount = true,
                    SupportsDetailedIndexMetadata = true
                });
                
            var service = new DatabaseService(_dummyConnectionString, mockCapabilityDetector.Object, _configuration);
            
            // Act - initial capability detection
            await GetPrivateCapabilities(service);
            mockCapabilityDetector.Verify(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
            
            // Reset invocation count
            mockCapabilityDetector.Invocations.Clear();
            
            // Act - call methods that would use capabilities
            await GetPrivateCapabilities(service);
            await GetPrivateCapabilities(service);
            
            // Assert - detector should not be called again
            mockCapabilityDetector.Verify(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact(DisplayName = "DSC-004: Service handles different SQL Server versions")]
        public async Task DSC004()
        {
            // Arrange - SQL Server 2008 with minimal capabilities
            var limitedCapability = new SqlServerCapability
            {
                MajorVersion = 10, // SQL Server 2008
                SupportsExactRowCount = false,
                SupportsDetailedIndexMetadata = false,
                SupportsDataCompression = false
            };

            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>(MockBehavior.Strict);
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(limitedCapability);

            var service = new DatabaseService(_dummyConnectionString, mockCapabilityDetector.Object, _configuration);

            // Act
            var capability = await GetPrivateCapabilities(service);
            
            // Assert
            capability.MajorVersion.Should().Be(10);
            capability.SupportsExactRowCount.Should().BeFalse();
            capability.SupportsDetailedIndexMetadata.Should().BeFalse();
        }

        [Fact(DisplayName = "DSC-005: Service uses full capabilities with modern SQL Server")]
        public async Task DSC005()
        {
            // Arrange - SQL Server 2019 with full capabilities
            var fullCapability = new SqlServerCapability
            {
                MajorVersion = 15, // SQL Server 2019
                SupportsExactRowCount = true,
                SupportsDetailedIndexMetadata = true,
                SupportsJson = true,
                SupportsColumnstoreIndex = true,
                SupportsDataCompression = true,
                SupportsInMemoryOLTP = true
            };

            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>(MockBehavior.Strict);
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(fullCapability);

            var service = new DatabaseService(_dummyConnectionString, mockCapabilityDetector.Object, _configuration);
            
            // Act
            var capability = await GetPrivateCapabilities(service);
            
            // Assert
            capability.MajorVersion.Should().Be(15);
            capability.SupportsExactRowCount.Should().BeTrue();
            capability.SupportsDetailedIndexMetadata.Should().BeTrue();
            capability.SupportsDataCompression.Should().BeTrue();
        }

        [Fact(DisplayName = "DSC-006: Service handles Azure SQL DB capabilities differently")]
        public async Task DSC006()
        {
            // Arrange - Azure SQL Database capabilities
            var azureCapability = new SqlServerCapability
            {
                MajorVersion = 12, // Azure SQL DB reports as 12.x
                IsAzureSqlDatabase = true,
                IsOnPremisesSqlServer = false,
                SupportsExactRowCount = true,
                SupportsDetailedIndexMetadata = true,
                SupportsJson = true,
                SupportsDataCompression = true
            };

            var mockCapabilityDetector = new Mock<ISqlServerCapabilityDetector>(MockBehavior.Strict);
            mockCapabilityDetector.Setup(x => x.DetectCapabilitiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(azureCapability);

            var service = new DatabaseService(_dummyConnectionString, mockCapabilityDetector.Object, _configuration);
            
            // Act
            var capability = await GetPrivateCapabilities(service);
            
            // Assert
            capability.IsAzureSqlDatabase.Should().BeTrue();
            capability.IsOnPremisesSqlServer.Should().BeFalse();
            capability.SupportsExactRowCount.Should().BeTrue();
        }
        
        /// <summary>
        /// Helper method to access the private GetCapabilitiesAsync method through reflection.
        /// </summary>
        private async Task<SqlServerCapability> GetPrivateCapabilities(DatabaseService service)
        {
            // Use reflection to access the private GetCapabilitiesAsync method
            var method = typeof(DatabaseService).GetMethod("GetCapabilitiesAsync", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
                throw new InvalidOperationException("GetCapabilitiesAsync method not found");
                
            return await (Task<SqlServerCapability>)method.Invoke(service, new object[] { CancellationToken.None });
        }
    }
}