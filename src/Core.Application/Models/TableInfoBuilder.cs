using System;

namespace Core.Application.Models
{
    /// <summary>
    /// Builder class for creating TableInfo objects with optional properties.
    /// </summary>
    public class TableInfoBuilder
    {
        private string _schema = string.Empty;
        private string _name = string.Empty;
        private long? _rowCount;
        private double? _sizeMB;
        private DateTime _createDate = DateTime.MinValue;
        private DateTime _modifyDate = DateTime.MinValue;
        private int? _indexCount;
        private int? _foreignKeyCount;
        private string _tableType = "Normal";

        /// <summary>
        /// Sets the schema name of the table.
        /// </summary>
        /// <param name="schema">The schema name.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithSchema(string schema)
        {
            _schema = schema ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Sets the name of the table.
        /// </summary>
        /// <param name="name">The table name.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithName(string name)
        {
            _name = name ?? string.Empty;
            return this;
        }

        /// <summary>
        /// Sets the row count of the table.
        /// </summary>
        /// <param name="rowCount">The row count or null if not available.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithRowCount(long? rowCount)
        {
            _rowCount = rowCount;
            return this;
        }

        /// <summary>
        /// Sets the size in megabytes of the table.
        /// </summary>
        /// <param name="sizeMB">The size in megabytes or null if not available.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithSizeMB(double? sizeMB)
        {
            _sizeMB = sizeMB;
            return this;
        }

        /// <summary>
        /// Sets the creation date of the table.
        /// </summary>
        /// <param name="createDate">The creation date.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithCreateDate(DateTime createDate)
        {
            _createDate = createDate;
            return this;
        }

        /// <summary>
        /// Sets the last modification date of the table.
        /// </summary>
        /// <param name="modifyDate">The last modification date.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithModifyDate(DateTime modifyDate)
        {
            _modifyDate = modifyDate;
            return this;
        }

        /// <summary>
        /// Sets the index count of the table.
        /// </summary>
        /// <param name="indexCount">The index count or null if not available.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithIndexCount(int? indexCount)
        {
            _indexCount = indexCount;
            return this;
        }

        /// <summary>
        /// Sets the foreign key count of the table.
        /// </summary>
        /// <param name="foreignKeyCount">The foreign key count or null if not available.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithForeignKeyCount(int? foreignKeyCount)
        {
            _foreignKeyCount = foreignKeyCount;
            return this;
        }

        /// <summary>
        /// Sets the table type.
        /// </summary>
        /// <param name="tableType">The table type.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public TableInfoBuilder WithTableType(string tableType)
        {
            _tableType = tableType ?? "Normal";
            return this;
        }

        /// <summary>
        /// Builds a TableInfo object with the properties specified.
        /// </summary>
        /// <returns>A new TableInfo instance.</returns>
        public TableInfo Build()
        {
            return new TableInfo(
                _schema,
                _name,
                _rowCount,
                _sizeMB,
                _createDate,
                _modifyDate,
                _indexCount,
                _foreignKeyCount,
                _tableType);
        }

        /// <summary>
        /// Creates a builder for constructing TableInfo objects.
        /// </summary>
        /// <returns>A new TableInfoBuilder instance.</returns>
        public static TableInfoBuilder Create()
        {
            return new TableInfoBuilder();
        }

        /// <summary>
        /// Creates a builder initialized with values from an existing TableInfo.
        /// </summary>
        /// <param name="tableInfo">The TableInfo to copy values from.</param>
        /// <returns>A new TableInfoBuilder instance with copied values.</returns>
        public static TableInfoBuilder CreateFrom(TableInfo tableInfo)
        {
            if (tableInfo == null)
                throw new ArgumentNullException(nameof(tableInfo));

            return new TableInfoBuilder()
                .WithSchema(tableInfo.Schema)
                .WithName(tableInfo.Name)
                .WithRowCount(tableInfo.RowCount)
                .WithSizeMB(tableInfo.SizeMB)
                .WithCreateDate(tableInfo.CreateDate)
                .WithModifyDate(tableInfo.ModifyDate)
                .WithIndexCount(tableInfo.IndexCount)
                .WithForeignKeyCount(tableInfo.ForeignKeyCount)
                .WithTableType(tableInfo.TableType);
        }
    }
}