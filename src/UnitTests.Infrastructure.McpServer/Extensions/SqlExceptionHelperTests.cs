using Core.Infrastructure.McpServer.Extensions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Extensions
{
    public class SqlExceptionHelperTests
    {
        [Fact(DisplayName = "SQLH-001: IsTimeoutError returns true for error number -2")]
        public void SQLH001()
        {
            // Arrange
            var exception = CreateSqlException(-2);

            // Act
            var result = SqlExceptionHelper.IsTimeoutError(exception);

            // Assert
            Assert.True(result);
        }

        [Fact(DisplayName = "SQLH-002: IsTimeoutError returns true for error number -1")]
        public void SQLH002()
        {
            // Arrange
            var exception = CreateSqlException(-1);

            // Act
            var result = SqlExceptionHelper.IsTimeoutError(exception);

            // Assert
            Assert.True(result);
        }

        [Fact(DisplayName = "SQLH-003: IsTimeoutError returns true for error number 0")]
        public void SQLH003()
        {
            // Arrange
            var exception = CreateSqlException(0);

            // Act
            var result = SqlExceptionHelper.IsTimeoutError(exception);

            // Assert
            Assert.True(result);
        }

        [Fact(DisplayName = "SQLH-004: IsTimeoutError returns false for other error numbers")]
        public void SQLH004()
        {
            // Arrange
            var exception = CreateSqlException(547); // Foreign key constraint violation

            // Act
            var result = SqlExceptionHelper.IsTimeoutError(exception);

            // Assert
            Assert.False(result);
        }

        [Fact(DisplayName = "SQLH-005: IsTimeoutError returns false for error number 2627")]
        public void SQLH005()
        {
            // Arrange
            var exception = CreateSqlException(2627); // Primary key violation

            // Act
            var result = SqlExceptionHelper.IsTimeoutError(exception);

            // Assert
            Assert.False(result);
        }

        [Fact(DisplayName = "SQLH-006: IsTimeoutError returns false for error number 208")]
        public void SQLH006()
        {
            // Arrange
            var exception = CreateSqlException(208); // Invalid object name

            // Act
            var result = SqlExceptionHelper.IsTimeoutError(exception);

            // Assert
            Assert.False(result);
        }

        private static SqlException CreateSqlException(int errorNumber)
        {
            // Use reflection to create SqlException with specific error number
            // (SqlException doesn't have public constructors)
            var collection = Activator.CreateInstance(
                typeof(SqlErrorCollection),
                nonPublic: true) as SqlErrorCollection;

            // Try different constructor signatures for SqlError based on version
            // Version 6.0+ uses: int, byte, byte, string, string, string, int, uint, Exception
            SqlError? error = null;

            try
            {
                // Try newer constructor signature (v6.0+): errorNumber, errorState, errorClass, server, errorMessage, procedure, lineNumber, win32ErrorCode, innerException
                error = Activator.CreateInstance(
                    typeof(SqlError),
                    bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    binder: null,
                    args: new object?[] { errorNumber, (byte)0, (byte)0, "server", "error message", "procedure", 0, (uint)0, null },
                    culture: null) as SqlError;
            }
            catch
            {
                // Fallback to older signature if the newer one doesn't work
                error = Activator.CreateInstance(
                    typeof(SqlError),
                    bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                    binder: null,
                    args: new object[] { errorNumber, (byte)0, (byte)0, "", "", "", 0 },
                    culture: null) as SqlError;
            }

            typeof(SqlErrorCollection)
                .GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(collection, new object[] { error! });

            var exception = Activator.CreateInstance(
                typeof(SqlException),
                bindingAttr: System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                binder: null,
                args: new object[] { "", collection!, null!, Guid.NewGuid() },
                culture: null) as SqlException;

            return exception!;
        }
    }
}
