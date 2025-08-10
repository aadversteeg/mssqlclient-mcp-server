namespace Core.Application.Models
{
    /// <summary>
    /// Represents immutable information about a database.
    /// </summary>
    public sealed record DatabaseInfo(
        string Name,
        string State,
        double? SizeMB,
        string Owner,
        string CompatibilityLevel,
        string CollationName,
        DateTime CreateDate,
        string RecoveryModel,
        bool IsReadOnly);
}