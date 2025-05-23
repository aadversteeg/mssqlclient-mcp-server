namespace Core.Application.Models
{
    /// <summary>
    /// Represents immutable information about a database stored procedure parameter.
    /// </summary>
    public sealed record StoredProcedureParameterInfo(
        string Name,
        string DataType,
        int Length,
        int Precision,
        int Scale,
        bool IsOutput,
        bool IsNullable,
        string? DefaultValue);

    /// <summary>
    /// Represents immutable information about a database stored procedure.
    /// </summary>
    public sealed record StoredProcedureInfo(
        string SchemaName,
        string Name,
        DateTime CreateDate,
        DateTime ModifyDate,
        string Owner,
        IReadOnlyCollection<StoredProcedureParameterInfo> Parameters,
        bool IsFunction,
        DateTime? LastExecutionTime,
        int? ExecutionCount,
        int? AverageDurationMs);
}