namespace Core.Application.Models
{
    /// <summary>
    /// Represents immutable information about a database table.
    /// </summary>
    public sealed record TableInfo(
        string Schema,
        string Name,
        long? RowCount,
        double? SizeMB,
        DateTime CreateDate,
        DateTime ModifyDate,
        int? IndexCount,
        int? ForeignKeyCount,
        string TableType);
}