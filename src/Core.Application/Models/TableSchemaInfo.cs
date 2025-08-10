
namespace Core.Application.Models
{
    /// <summary>
    /// Represents information about a column in a database table schema.
    /// </summary>
    public sealed record TableColumnInfo(
        string ColumnName,
        string DataType,
        string MaxLength,
        string IsNullable,
        string MsDescription);

    /// <summary>
    /// Represents immutable information about a database table schema.
    /// </summary>
    public sealed record TableSchemaInfo(
        string TableName,
        string DatabaseName,
        string MsDescription,
        IEnumerable<TableColumnInfo> Columns)
    {
        /// <summary>
        /// Returns empty string if MsDescription is empty,
        /// or returns a `({MsDescription})` string.
        /// </summary>
        /// <returns></returns>
        public string GetMsDescriptionWithBrackets()
        {
            if (string.IsNullOrEmpty(MsDescription))
            {
                return string.Empty;
            }

            return $"({MsDescription})";
        }
    }
}