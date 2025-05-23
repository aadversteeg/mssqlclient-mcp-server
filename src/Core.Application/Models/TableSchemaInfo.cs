namespace Core.Application.Models
{
    /// <summary>
    /// Represents information about a column in a database table schema.
    /// </summary>
    public sealed record TableColumnInfo(
        string ColumnName,
        string DataType,
        string MaxLength,
        string IsNullable);

    /// <summary>
    /// Represents immutable information about a database table schema.
    /// </summary>
    public sealed record TableSchemaInfo(
        string TableName,
        string DatabaseName,
        IEnumerable<TableColumnInfo> Columns);
}