using Core.Infrastructure.McpServer.Extensions;
using FluentAssertions;

namespace UnitTests.Infrastructure.McpServer.Extensions
{
    public class RowsAffectedParserTests
    {
        [Fact(DisplayName = "RAP-001: Single rows affected message returns correct value")]
        public void RAP001()
        {
            // Arrange
            var messages = new List<string>
            {
                "(5 rows affected)"
            };

            // Act
            var result = RowsAffectedParser.Parse(messages);

            // Assert
            result.Should().Be(5);
        }

        [Fact(DisplayName = "RAP-002: Empty messages returns null")]
        public void RAP002()
        {
            // Arrange
            var messages = new List<string>();

            // Act
            var result = RowsAffectedParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "RAP-003: Null input returns null")]
        public void RAP003()
        {
            // Act
            var result = RowsAffectedParser.Parse(null);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "RAP-004: Multiple messages with rows affected values are summed")]
        public void RAP004()
        {
            // Arrange
            var messages = new List<string>
            {
                "(3 rows affected)",
                "(7 rows affected)"
            };

            // Act
            var result = RowsAffectedParser.Parse(messages);

            // Assert
            result.Should().Be(10);
        }

        [Fact(DisplayName = "RAP-005: Singular row affected returns 1")]
        public void RAP005()
        {
            // Arrange
            var messages = new List<string>
            {
                "(1 row affected)"
            };

            // Act
            var result = RowsAffectedParser.Parse(messages);

            // Assert
            result.Should().Be(1);
        }

        [Fact(DisplayName = "RAP-006: No rows affected messages returns null")]
        public void RAP006()
        {
            // Arrange
            var messages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 16 ms,  elapsed time = 123 ms.",
                "SQL Server parse and compile time: \n   CPU time = 0 ms, elapsed time = 1 ms."
            };

            // Act
            var result = RowsAffectedParser.Parse(messages);

            // Assert
            result.Should().BeNull();
        }

        [Fact(DisplayName = "RAP-007: Zero rows affected returns 0")]
        public void RAP007()
        {
            // Arrange
            var messages = new List<string>
            {
                "(0 rows affected)"
            };

            // Act
            var result = RowsAffectedParser.Parse(messages);

            // Assert
            result.Should().Be(0);
        }
    }
}
