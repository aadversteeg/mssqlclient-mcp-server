using System.Diagnostics;
using Core.Application.Interfaces;
using Core.Infrastructure.McpServer.Extensions;
using FluentAssertions;
using Moq;

namespace UnitTests.Infrastructure.McpServer.Extensions
{
    public class AsyncDataReaderExtensionsTests
    {
        [Fact(DisplayName = "ADRE-001: ToToolResult with rows and stopwatch, no info messages returns client timing only")]
        public async Task ADRE001()
        {
            // Arrange
            var mockReader = CreateMockReaderWithOneRow();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Total rows: 1");
            result.Should().MatchRegex(@"Execution time: \d+ms");
            result.Should().NotContain("server:");
            result.Should().NotContain("CPU:");
        }

        [Fact(DisplayName = "ADRE-002: ToToolResult with rows and stopwatch + valid statistics messages returns full timing line")]
        public async Task ADRE002()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 12 ms,  elapsed time = 38 ms."
            };
            var mockReader = CreateMockReaderWithOneRow(infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Total rows: 1");
            result.Should().MatchRegex(@"Execution time: \d+ms \(server: 38ms, CPU: 12ms\)");
        }

        [Fact(DisplayName = "ADRE-003: ToToolResult with no rows + stopwatch + messages returns timing on no-rows message")]
        public async Task ADRE003()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 5 ms,  elapsed time = 10 ms."
            };
            var mockReader = CreateMockReaderWithNoRows(infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Query executed successfully. No results returned.");
            result.Should().MatchRegex(@"Execution time: \d+ms \(server: 10ms, CPU: 5ms\)");
        }

        [Fact(DisplayName = "ADRE-004: ToToolResult with no rows + stopwatch, no info messages returns client timing only")]
        public async Task ADRE004()
        {
            // Arrange
            var mockReader = CreateMockReaderWithNoRows();
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Query executed successfully. No results returned.");
            result.Should().MatchRegex(@"Execution time: \d+ms");
            result.Should().NotContain("server:");
        }

        private static Mock<IAsyncDataReader> CreateMockReaderWithOneRow(List<string>? infoMessages = null)
        {
            var mockReader = new Mock<IAsyncDataReader>();
            var readCalled = false;

            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (!readCalled)
                    {
                        readCalled = true;
                        return true;
                    }
                    return false;
                });

            mockReader.Setup(x => x.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            mockReader.Setup(x => x.FieldCount).Returns(1);
            mockReader.Setup(x => x.GetColumnNames()).Returns(new[] { "Value" });
            mockReader.Setup(x => x.IsDBNullAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(x => x.GetFieldValueAsync<object>(0, It.IsAny<CancellationToken>())).ReturnsAsync("TestValue");
            mockReader.Setup(x => x.InfoMessages).Returns(infoMessages ?? new List<string>());

            return mockReader;
        }

        private static Mock<IAsyncDataReader> CreateMockReaderWithNoRows(List<string>? infoMessages = null)
        {
            var mockReader = new Mock<IAsyncDataReader>();

            mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            mockReader.Setup(x => x.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            mockReader.Setup(x => x.FieldCount).Returns(0);
            mockReader.Setup(x => x.GetColumnNames()).Returns(Array.Empty<string>());
            mockReader.Setup(x => x.InfoMessages).Returns(infoMessages ?? new List<string>());

            return mockReader;
        }
    }
}
