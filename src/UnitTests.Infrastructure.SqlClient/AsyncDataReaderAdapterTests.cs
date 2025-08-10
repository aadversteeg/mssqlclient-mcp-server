using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Application.Interfaces;
using Core.Infrastructure.SqlClient;
using FluentAssertions;
using Moq;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient
{
    public class AsyncDataReaderAdapterTests
    {
        private readonly Mock<DbDataReader> _readerMock;
        private readonly IAsyncDataReader _adapter;

        public AsyncDataReaderAdapterTests()
        {
            _readerMock = new Mock<DbDataReader>();
            _adapter = new AsyncDataReaderAdapter(_readerMock.Object);
        }

        [Fact(DisplayName = "ADAR-001: Constructor should throw an exception when reader is null")]
        public void ADAR001()
        {
            // Arrange
            DbDataReader reader = null!;
            
            // Act
            Action act = () => new AsyncDataReaderAdapter(reader);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("reader");
        }

        [Fact(DisplayName = "ADAR-002: ReadAsync should delegate to the underlying reader")]
        public async Task ADAR002()
        {
            // Arrange
            _readerMock.Setup(r => r.ReadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            bool result = await _adapter.ReadAsync();

            // Assert
            result.Should().BeTrue();
            _readerMock.Verify(r => r.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "ADAR-003: NextResultAsync should delegate to the underlying reader")]
        public async Task ADAR003()
        {
            // Arrange
            _readerMock.Setup(r => r.NextResultAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            bool result = await _adapter.NextResultAsync();

            // Assert
            result.Should().BeTrue();
            _readerMock.Verify(r => r.NextResultAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "ADAR-004: GetFieldValueAsync should delegate to the underlying reader")]
        public async Task ADAR004()
        {
            // Arrange
            string expectedValue = "TestValue";
            _readerMock.Setup(r => r.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedValue);

            // Act
            string result = await _adapter.GetFieldValueAsync<string>(0);

            // Assert
            result.Should().Be(expectedValue);
            _readerMock.Verify(r => r.GetFieldValueAsync<string>(0, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "ADAR-005: IsDBNullAsync should delegate to the underlying reader")]
        public async Task ADAR005()
        {
            // Arrange
            _readerMock.Setup(r => r.IsDBNullAsync(0, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            bool result = await _adapter.IsDBNullAsync(0);

            // Assert
            result.Should().BeTrue();
            _readerMock.Verify(r => r.IsDBNullAsync(0, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact(DisplayName = "ADAR-006: FieldCount property should delegate to the underlying reader")]
        public void ADAR006()
        {
            // Arrange
            int expectedValue = 5;
            _readerMock.Setup(r => r.FieldCount).Returns(expectedValue);

            // Act
            int result = _adapter.FieldCount;

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact(DisplayName = "ADAR-007: IsClosed property should delegate to the underlying reader")]
        public void ADAR007()
        {
            // Arrange
            bool expectedValue = true;
            _readerMock.Setup(r => r.IsClosed).Returns(expectedValue);

            // Act
            bool result = _adapter.IsClosed;

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact(DisplayName = "ADAR-008: GetName should delegate to the underlying reader")]
        public void ADAR008()
        {
            // Arrange
            string expectedValue = "ColumnName";
            _readerMock.Setup(r => r.GetName(0)).Returns(expectedValue);

            // Act
            string result = _adapter.GetName(0);

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact(DisplayName = "ADAR-009: GetOrdinal should delegate to the underlying reader")]
        public void ADAR009()
        {
            // Arrange
            int expectedValue = 3;
            _readerMock.Setup(r => r.GetOrdinal("ColumnName")).Returns(expectedValue);

            // Act
            int result = _adapter.GetOrdinal("ColumnName");

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact(DisplayName = "ADAR-010: GetFieldType should delegate to the underlying reader")]
        public void ADAR010()
        {
            // Arrange
            Type expectedValue = typeof(string);
            _readerMock.Setup(r => r.GetFieldType(0)).Returns(expectedValue);

            // Act
            Type result = _adapter.GetFieldType(0);

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact(DisplayName = "ADAR-011: GetDataTypeName should delegate to the underlying reader")]
        public void ADAR011()
        {
            // Arrange
            string expectedValue = "nvarchar";
            _readerMock.Setup(r => r.GetDataTypeName(0)).Returns(expectedValue);

            // Act
            string result = _adapter.GetDataTypeName(0);

            // Assert
            result.Should().Be(expectedValue);
        }

        [Fact(DisplayName = "ADAR-012: GetColumnNames should return the correct column names")]
        public void ADAR012()
        {
            // Arrange
            _readerMock.Setup(r => r.FieldCount).Returns(2);
            _readerMock.Setup(r => r.GetName(0)).Returns("Column1");
            _readerMock.Setup(r => r.GetName(1)).Returns("Column2");

            // Act
            var columnNames = _adapter.GetColumnNames().ToList();

            // Assert
            columnNames.Should().HaveCount(2);
            columnNames[0].Should().Be("Column1");
            columnNames[1].Should().Be("Column2");
        }

        [Fact(DisplayName = "ADAR-013: GetColumnSchema should return the correct schema information")]
        public void ADAR013()
        {
            // Arrange
            _readerMock.Setup(r => r.FieldCount).Returns(2);
            _readerMock.Setup(r => r.GetName(0)).Returns("Column1");
            _readerMock.Setup(r => r.GetName(1)).Returns("Column2");
            _readerMock.Setup(r => r.GetFieldType(0)).Returns(typeof(string));
            _readerMock.Setup(r => r.GetFieldType(1)).Returns(typeof(int));
            _readerMock.Setup(r => r.GetDataTypeName(0)).Returns("nvarchar");
            _readerMock.Setup(r => r.GetDataTypeName(1)).Returns("int");

            // Act
            var schema = _adapter.GetColumnSchema().ToList();

            // Assert
            schema.Should().HaveCount(2);
            schema[0].Name.Should().Be("Column1");
            schema[0].Type.Should().Be(typeof(string));
            schema[0].TypeName.Should().Be("nvarchar");
            schema[1].Name.Should().Be("Column2");
            schema[1].Type.Should().Be(typeof(int));
            schema[1].TypeName.Should().Be("int");
        }

        [Fact(DisplayName = "ADAR-014: Close should delegate to the underlying reader")]
        public void ADAR014()
        {
            // Act
            _adapter.Close();

            // Assert
            _readerMock.Verify(r => r.Close(), Times.Once);
        }

        [Fact(DisplayName = "ADAR-015: Dispose should delegate to the underlying reader")]
        public void ADAR015()
        {
            // Create a mock that can track whether Dispose was called
            var mockDisposable = new MockDisposable();
            var reader = new TestDbDataReader(mockDisposable);
            var adapter = new AsyncDataReaderAdapter(reader);
            
            // Act
            adapter.Dispose();

            // Assert
            mockDisposable.WasDisposed.Should().BeTrue("The underlying reader should be disposed");
        }
        
        // Helper classes for testing dispose behavior
        private class MockDisposable : IDisposable
        {
            public bool WasDisposed { get; private set; }
            
            public void Dispose()
            {
                WasDisposed = true;
            }
        }
        
        private class TestDbDataReader : DbDataReader
        {
            private readonly IDisposable _disposable;
            
            public TestDbDataReader(IDisposable disposable)
            {
                _disposable = disposable;
            }
            
            public override object this[int ordinal] => throw new NotImplementedException();
            public override object this[string name] => throw new NotImplementedException();
            public override int Depth => throw new NotImplementedException();
            public override int FieldCount => throw new NotImplementedException();
            public override bool HasRows => throw new NotImplementedException();
            public override bool IsClosed => throw new NotImplementedException();
            public override int RecordsAffected => throw new NotImplementedException();
            public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
            public override byte GetByte(int ordinal) => throw new NotImplementedException();
            public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
            public override char GetChar(int ordinal) => throw new NotImplementedException();
            public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
            public override string GetDataTypeName(int ordinal) => throw new NotImplementedException();
            public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
            public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
            public override double GetDouble(int ordinal) => throw new NotImplementedException();
            public override System.Collections.IEnumerator GetEnumerator() => throw new NotImplementedException();
            public override Type GetFieldType(int ordinal) => throw new NotImplementedException();
            public override float GetFloat(int ordinal) => throw new NotImplementedException();
            public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
            public override short GetInt16(int ordinal) => throw new NotImplementedException();
            public override int GetInt32(int ordinal) => throw new NotImplementedException();
            public override long GetInt64(int ordinal) => throw new NotImplementedException();
            public override string GetName(int ordinal) => throw new NotImplementedException();
            public override int GetOrdinal(string name) => throw new NotImplementedException();
            public override string GetString(int ordinal) => throw new NotImplementedException();
            public override object GetValue(int ordinal) => throw new NotImplementedException();
            public override int GetValues(object[] values) => throw new NotImplementedException();
            public override bool IsDBNull(int ordinal) => throw new NotImplementedException();
            public override bool NextResult() => throw new NotImplementedException();
            public override bool Read() => throw new NotImplementedException();
            
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _disposable.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}