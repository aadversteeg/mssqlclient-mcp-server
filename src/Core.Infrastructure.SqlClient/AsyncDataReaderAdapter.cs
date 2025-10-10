using System.Data.Common;
using Core.Application.Interfaces;

namespace Core.Infrastructure.SqlClient
{
    /// <summary>
    /// Adapter class that wraps a DbDataReader to implement IAsyncDataReader
    /// Provides asynchronous access to data from SQL Server
    /// </summary>
    public class AsyncDataReaderAdapter : IAsyncDataReader
    {
        private readonly DbDataReader _reader;

        public AsyncDataReaderAdapter(DbDataReader reader)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        // Core async methods
        public Task<bool> ReadAsync(CancellationToken cancellationToken = default) => 
            _reader.ReadAsync(cancellationToken);

        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) => 
            _reader.NextResultAsync(cancellationToken);

        // Field async methods
        public Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken = default) => 
            _reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        
        public Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken = default) => 
            _reader.IsDBNullAsync(ordinal, cancellationToken);

        // Schema information methods
        public int FieldCount => _reader.FieldCount;
        public bool IsClosed => _reader.IsClosed;
        public string GetName(int ordinal) => _reader.GetName(ordinal);
        public int GetOrdinal(string name) => _reader.GetOrdinal(name);
        public Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);
        public string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);
        
        // Schema helper methods
        public IEnumerable<string> GetColumnNames()
        {
            for (int i = 0; i < FieldCount; i++)
            {
                yield return GetName(i);
            }
        }
        
        public IEnumerable<(string Name, Type Type, string TypeName)> GetColumnSchema()
        {
            for (int i = 0; i < FieldCount; i++)
            {
                yield return (GetName(i), GetFieldType(i), GetDataTypeName(i));
            }
        }
        
        public void Close() => _reader.Close();

        // IDisposable implementation
        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
