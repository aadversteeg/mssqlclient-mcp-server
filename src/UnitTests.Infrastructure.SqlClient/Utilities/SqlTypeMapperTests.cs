using Core.Infrastructure.SqlClient.Utilities;
using FluentAssertions;
using System;
using System.Data;
using System.Text.Json;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient.Utilities
{
    /// <summary>
    /// Unit tests for SqlTypeMapper utility class.
    /// </summary>
    public class SqlTypeMapperTests
    {
        [Fact(DisplayName = "STM-001: CreateSqlParameter converts int correctly")]
        public void STM001()
        {
            // Arrange
            var value = 123;
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "int", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Int);
            param.Value.Should().Be(123);
            param.ParameterName.Should().Be("@TestParam");
            param.Direction.Should().Be(ParameterDirection.Input);
        }
        
        [Fact(DisplayName = "STM-002: CreateSqlParameter converts string to int")]
        public void STM002()
        {
            // Arrange
            var value = "123";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "int", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Int);
            param.Value.Should().Be(123);
        }
        
        [Fact(DisplayName = "STM-003: CreateSqlParameter handles null values")]
        public void STM003()
        {
            // Arrange
            object? value = null;
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "int", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Int);
            param.Value.Should().Be(DBNull.Value);
        }
        
        [Fact(DisplayName = "STM-004: CreateSqlParameter converts bigint correctly")]
        public void STM004()
        {
            // Arrange
            var value = 9223372036854775807L; // long.MaxValue
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "bigint", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.BigInt);
            param.Value.Should().Be(9223372036854775807L);
        }
        
        [Fact(DisplayName = "STM-005: CreateSqlParameter converts decimal with precision and scale")]
        public void STM005()
        {
            // Arrange
            var value = 123.45m;
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "decimal", 0, 10, 2, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Decimal);
            param.Value.Should().Be(123.45m);
            param.Precision.Should().Be(10);
            param.Scale.Should().Be(2);
        }
        
        [Fact(DisplayName = "STM-006: CreateSqlParameter converts bit to boolean")]
        public void STM006()
        {
            // Arrange
            var value = true;
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "bit", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Bit);
            param.Value.Should().Be(true);
        }
        
        [Fact(DisplayName = "STM-007: CreateSqlParameter converts string bit to boolean")]
        public void STM007()
        {
            // Arrange
            var value = "1";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "bit", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Bit);
            param.Value.Should().Be(true);
        }
        
        [Fact(DisplayName = "STM-008: CreateSqlParameter converts datetime string")]
        public void STM008()
        {
            // Arrange
            var value = "2024-01-01T10:30:00";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "datetime", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.DateTime);
            param.Value.Should().Be(new DateTime(2024, 1, 1, 10, 30, 0));
        }
        
        [Fact(DisplayName = "STM-009: CreateSqlParameter converts date to date only")]
        public void STM009()
        {
            // Arrange
            var value = new DateTime(2024, 1, 1, 10, 30, 0);
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "date", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Date);
            param.Value.Should().Be(new DateTime(2024, 1, 1)); // Time part should be removed
        }
        
        [Fact(DisplayName = "STM-010: CreateSqlParameter converts varchar with length")]
        public void STM010()
        {
            // Arrange
            var value = "test string";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "varchar", 50, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.VarChar);
            param.Value.Should().Be("test string");
            param.Size.Should().Be(50);
        }
        
        [Fact(DisplayName = "STM-011: CreateSqlParameter converts nvarchar with correct size")]
        public void STM011()
        {
            // Arrange
            var value = "test string";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "nvarchar", 100, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.NVarChar);
            param.Value.Should().Be("test string");
            param.Size.Should().Be(50); // nvarchar uses 2 bytes per character, so 100/2 = 50
        }
        
        [Fact(DisplayName = "STM-012: CreateSqlParameter converts uniqueidentifier")]
        public void STM012()
        {
            // Arrange
            var guidString = "550e8400-e29b-41d4-a716-446655440000";
            var expectedGuid = new Guid(guidString);
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", guidString, "uniqueidentifier", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.UniqueIdentifier);
            param.Value.Should().Be(expectedGuid);
        }
        
        [Fact(DisplayName = "STM-013: CreateSqlParameter sets output direction")]
        public void STM013()
        {
            // Arrange
            var value = 123;
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "int", 0, 0, 0, true);
            
            // Assert
            param.Direction.Should().Be(ParameterDirection.InputOutput);
        }
        
        [Fact(DisplayName = "STM-014: CreateSqlParameter sets output direction for null value")]
        public void STM014()
        {
            // Arrange
            object? value = null;
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "int", 0, 0, 0, true);
            
            // Assert
            param.Direction.Should().Be(ParameterDirection.Output);
        }
        
        [Fact(DisplayName = "STM-015: CreateSqlParameter converts JSON element")]
        public void STM015()
        {
            // Arrange
            var json = """{"value": 123}""";
            var element = JsonSerializer.Deserialize<JsonDocument>(json)!.RootElement.GetProperty("value");
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", element, "int", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Int);
            param.Value.Should().Be(123);
        }
        
        [Fact(DisplayName = "STM-016: CreateSqlParameter converts base64 to binary")]
        public void STM016()
        {
            // Arrange
            var originalBytes = new byte[] { 1, 2, 3, 4, 5 };
            var base64String = Convert.ToBase64String(originalBytes);
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", base64String, "varbinary", 50, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.VarBinary);
            param.Value.Should().BeEquivalentTo(originalBytes);
            param.Size.Should().Be(50);
        }
        
        [Fact(DisplayName = "STM-017: CreateSqlParameter handles invalid int conversion")]
        public void STM017()
        {
            // Arrange
            var value = "not a number";
            
            // Act & Assert
            var act = () => SqlTypeMapper.CreateSqlParameter("@TestParam", value, "int", 0, 0, 0, false);
            act.Should().Throw<InvalidCastException>()
                .WithMessage("Cannot convert value 'not a number' to Int32");
        }
        
        [Fact(DisplayName = "STM-018: CreateSqlParameter handles unknown data type")]
        public void STM018()
        {
            // Arrange
            var value = "some value";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "unknowntype", 0, 0, 0, false);
            
            // Assert
            param.Value.Should().Be("some value");
        }
        
        [Fact(DisplayName = "STM-019: CreateSqlParameter converts time string to TimeSpan")]
        public void STM019()
        {
            // Arrange
            var value = "14:30:00";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "time", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.Time);
            param.Value.Should().Be(new TimeSpan(14, 30, 0));
        }
        
        [Fact(DisplayName = "STM-020: CreateSqlParameter converts DateTimeOffset")]
        public void STM020()
        {
            // Arrange
            var value = "2024-01-01T14:30:00+02:00";
            
            // Act
            var param = SqlTypeMapper.CreateSqlParameter("@TestParam", value, "datetimeoffset", 0, 0, 0, false);
            
            // Assert
            param.SqlDbType.Should().Be(SqlDbType.DateTimeOffset);
            param.Value.Should().Be(DateTimeOffset.Parse(value));
        }
    }
}