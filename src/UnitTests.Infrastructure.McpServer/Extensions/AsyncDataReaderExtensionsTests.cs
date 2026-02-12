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

        [Fact(DisplayName = "ADRE-005: With IO messages — IO stats line appears in output")]
        public async Task ADRE005()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "Table 'Products'. Scan count 1, logical reads 42, physical reads 3, page server reads 0, read-ahead reads 40, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };
            var mockReader = CreateMockReaderWithOneRow(infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("IO stats: Products (logical: 42, physical: 3, read-ahead: 40)");
        }

        [Fact(DisplayName = "ADRE-006: With timing + IO — both lines appear")]
        public async Task ADRE006()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 12 ms,  elapsed time = 38 ms.",
                "Table 'Orders'. Scan count 1, logical reads 100, physical reads 5, page server reads 0, read-ahead reads 80, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };
            var mockReader = CreateMockReaderWithOneRow(infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Execution time:");
            result.Should().Contain("IO stats:");
        }

        [Fact(DisplayName = "ADRE-007: No rows + IO — IO stats line appears")]
        public async Task ADRE007()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "Table 'Customers'. Scan count 1, logical reads 20, physical reads 1, page server reads 0, read-ahead reads 15, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };
            var mockReader = CreateMockReaderWithNoRows(infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("IO stats:");
        }

        [Fact(DisplayName = "ADRE-008: No rows + rows affected messages — Rows affected: N appears")]
        public async Task ADRE008()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "(5 rows affected)"
            };
            var mockReader = CreateMockReaderWithNoRows(infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Rows affected: 5");
        }

        [Fact(DisplayName = "ADRE-009: With execution plan result set — plan XML in output")]
        public async Task ADRE009()
        {
            // Arrange
            var planXml = "<ShowPlanXML xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\">plan content</ShowPlanXML>";
            var mockReader = CreateMockReaderWithOneRowAndExecutionPlan(planXml);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Execution plan:");
            result.Should().Contain(planXml);
        }

        [Fact(DisplayName = "ADRE-010: All stats combined — all sections present")]
        public async Task ADRE010()
        {
            // Arrange
            var infoMessages = new List<string>
            {
                "SQL Server Execution Times:\n   CPU time = 25 ms,  elapsed time = 50 ms.",
                "Table 'Sales'. Scan count 1, logical reads 60, physical reads 2, page server reads 0, read-ahead reads 55, page server read-ahead reads 0, lob logical reads 0, lob physical reads 0, lob read-ahead reads 0, lob page server reads 0, lob page server read-ahead reads 0."
            };
            var planXml = "<ShowPlanXML xmlns=\"http://schemas.microsoft.com/sqlserver/2004/07/showplan\">full plan</ShowPlanXML>";
            var mockReader = CreateMockReaderWithOneRowAndExecutionPlan(planXml, infoMessages);
            var stopwatch = Stopwatch.StartNew();

            // Act
            var result = await mockReader.Object.ToToolResult(stopwatch);

            // Assert
            result.Should().Contain("Total rows:");
            result.Should().Contain("Execution time:");
            result.Should().Contain("IO stats:");
            result.Should().Contain("Execution plan:");
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

        private static Mock<IAsyncDataReader> CreateMockReaderWithOneRowAndExecutionPlan(string planXml, List<string>? infoMessages = null)
        {
            var mockReader = new Mock<IAsyncDataReader>();

            // First result set: one data row
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

            mockReader.Setup(x => x.FieldCount).Returns(1);
            mockReader.Setup(x => x.GetColumnNames()).Returns(new[] { "Value" });
            mockReader.Setup(x => x.IsDBNullAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            mockReader.Setup(x => x.GetFieldValueAsync<object>(0, It.IsAny<CancellationToken>())).ReturnsAsync("TestValue");

            // Second result set: execution plan
            var nextResultCalled = false;
            var planReadCalled = false;
            mockReader.Setup(x => x.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    if (!nextResultCalled)
                    {
                        nextResultCalled = true;
                        // After moving to next result set, reconfigure FieldCount and ReadAsync for the plan result set
                        mockReader.Setup(x => x.FieldCount).Returns(1);
                        mockReader.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                            .ReturnsAsync(() =>
                            {
                                if (!planReadCalled)
                                {
                                    planReadCalled = true;
                                    return true;
                                }
                                return false;
                            });
                        mockReader.Setup(x => x.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()))
                            .ReturnsAsync(planXml);
                        return true;
                    }
                    return false;
                });

            mockReader.Setup(x => x.InfoMessages).Returns(infoMessages ?? new List<string>());

            return mockReader;
        }
    }
}
