namespace Core.Application.Interfaces
{
    /// <summary>
    /// Interface for asynchronous data reading operations
    /// Provides a wrapper around DbDataReader's asynchronous capabilities
    /// </summary>
    public interface IAsyncDataReader : IDisposable
    {
        // Core async methods
        Task<bool> ReadAsync(CancellationToken cancellationToken = default);
        Task<bool> NextResultAsync(CancellationToken cancellationToken = default);
        
        // Field async methods
        Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken = default);
        Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken = default);
        
        // Schema information methods
        int FieldCount { get; }
        bool IsClosed { get; }
        string GetName(int ordinal);
        int GetOrdinal(string name);
        Type GetFieldType(int ordinal);
        string GetDataTypeName(int ordinal);
        
        // Schema helper methods
        IEnumerable<string> GetColumnNames();
        IEnumerable<(string Name, Type Type, string TypeName)> GetColumnSchema();
        
        // Close method
        void Close();
    }
}
