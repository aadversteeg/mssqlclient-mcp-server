using Core.Infrastructure.McpServer.Extensions;
using FluentAssertions;

namespace UnitTests.Infrastructure.McpServer.Extensions
{
    public class StatisticsIoParserTests
    {
        [Fact(DisplayName = "SIP-001: Parse single table IO returns correct values extracted")]
        public void SIP001()
        {
            // Arrange
            var messages = new List<string>
            {
                "Table 'Products'. Scan count 1, logical reads 42, physical reads 3, page server reads 0, read-ahead reads 40, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result![0].TableName.Should().Be("Products");
            result[0].LogicalReads.Should().Be(42);
            result[0].PhysicalReads.Should().Be(3);
            result[0].ReadAheadReads.Should().Be(40);
        }

        [Fact(DisplayName = "SIP-002: Empty messages returns null")]
        public void SIP002()
        {
            // Arrange
            var messages = new List<string>();

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "SIP-003: Null input returns null")]
        public void SIP003()
        {
            // Act
            var result = StatisticsIoParser.Parse(null);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "SIP-004: Multiple tables returns one entry per table")]
        public void SIP004()
        {
            // Arrange
            var messages = new List<string>
            {
                "Table 'Products'. Scan count 1, logical reads 42, physical reads 3, page server reads 0, read-ahead reads 40, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0.",
                "Table 'Orders'. Scan count 2, logical reads 100, physical reads 10, page server reads 0, read-ahead reads 50, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result!.Should().Contain(e => e.TableName == "Products");
            result.Should().Contain(e => e.TableName == "Orders");
        }

        [Fact(DisplayName = "SIP-005: Worktable entries are filtered out")]
        public void SIP005()
        {
            // Arrange
            var messages = new List<string>
            {
                "Table 'Worktable'. Scan count 1, logical reads 10, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "SIP-006: Same table appears twice has values summed")]
        public void SIP006()
        {
            // Arrange
            var messages = new List<string>
            {
                "Table 'Products'. Scan count 1, logical reads 20, physical reads 5, page server reads 0, read-ahead reads 10, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0.",
                "Table 'Products'. Scan count 1, logical reads 30, physical reads 7, page server reads 0, read-ahead reads 15, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result![0].TableName.Should().Be("Products");
            result[0].LogicalReads.Should().Be(50);
            result[0].PhysicalReads.Should().Be(12);
            result[0].ReadAheadReads.Should().Be(25);
        }

        [Fact(DisplayName = "SIP-007: Mixed content extracts only IO lines")]
        public void SIP007()
        {
            // Arrange
            var messages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 16 ms,  elapsed time = 123 ms.",
                "Table 'Products'. Scan count 1, logical reads 42, physical reads 3, page server reads 0, read-ahead reads 40, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result![0].TableName.Should().Be("Products");
        }

        [Fact(DisplayName = "SIP-008: Zero reads returns zeros")]
        public void SIP008()
        {
            // Arrange
            var messages = new List<string>
            {
                "Table 'Products'. Scan count 0, logical reads 0, physical reads 0, page server reads 0, read-ahead reads 0, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1);
            result![0].LogicalReads.Should().Be(0);
            result[0].PhysicalReads.Should().Be(0);
            result[0].ReadAheadReads.Should().Be(0);
        }

        [Fact(DisplayName = "SIP-009: No IO entries in messages returns null")]
        public void SIP009()
        {
            // Arrange
            var messages = new List<string>
            {
                "Some PRINT output from the query",
                "(5 rows affected)",
                "SQL Server parse and compile time: \n   CPU time = 0 ms, elapsed time = 1 ms."
            };

            // Act
            var result = StatisticsIoParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }
    }
}
