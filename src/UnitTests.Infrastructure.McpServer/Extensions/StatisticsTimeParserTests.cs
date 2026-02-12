using Core.Infrastructure.McpServer.Extensions;
using FluentAssertions;

namespace UnitTests.Infrastructure.McpServer.Extensions
{
    public class StatisticsTimeParserTests
    {
        [Fact(DisplayName = "STP-001: Parse valid execution times returns correct ElapsedMs and CpuMs")]
        public void STP001()
        {
            // Arrange
            var messages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 16 ms,  elapsed time = 123 ms."
            };

            // Act
            var result = StatisticsTimeParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result!.CpuMs.Should().Be(16);
            result.ElapsedMs.Should().Be(123);
        }

        [Fact(DisplayName = "STP-002: Parse empty messages list returns null")]
        public void STP002()
        {
            // Arrange
            var messages = new List<string>();

            // Act
            var result = StatisticsTimeParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "STP-003: Parse messages with only parse-and-compile times returns null")]
        public void STP003()
        {
            // Arrange
            var messages = new List<string>
            {
                "SQL Server parse and compile time: \n   CPU time = 0 ms, elapsed time = 0 ms."
            };

            // Act
            var result = StatisticsTimeParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "STP-004: Parse messages with multiple execution times returns the last one")]
        public void STP004()
        {
            // Arrange
            var messages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 5 ms,  elapsed time = 10 ms.",
                "SQL Server Execution Times:\n   CPU time = 20 ms,  elapsed time = 50 ms."
            };

            // Act
            var result = StatisticsTimeParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result!.CpuMs.Should().Be(20);
            result.ElapsedMs.Should().Be(50);
        }

        [Fact(DisplayName = "STP-005: Parse messages with mixed content extracts execution times correctly")]
        public void STP005()
        {
            // Arrange
            var messages = new List<string>
            {
                "Some PRINT output from the query",
                "SQL Server parse and compile time: \n   CPU time = 0 ms, elapsed time = 1 ms.",
                "(5 rows affected)",
                "SQL Server Execution Times:\n   CPU time = 32 ms,  elapsed time = 200 ms."
            };

            // Act
            var result = StatisticsTimeParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result!.CpuMs.Should().Be(32);
            result.ElapsedMs.Should().Be(200);
        }

        [Fact(DisplayName = "STP-006: Parse messages with zero CPU and elapsed time returns zeros")]
        public void STP006()
        {
            // Arrange
            var messages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 0 ms,  elapsed time = 0 ms."
            };

            // Act
            var result = StatisticsTimeParser.Parse(messages);

            // Assert
            result.Should().NotBeNull();
            result!.CpuMs.Should().Be(0);
            result.ElapsedMs.Should().Be(0);
        }

        [Fact(DisplayName = "STP-007: Parse null input returns null")]
        public void STP007()
        {
            // Act
            var result = StatisticsTimeParser.Parse(null!);

            // Assert
            result.Should().BeNull();
        }
    }
}
