namespace Core.Application.Models
{
    /// <summary>
    /// Options for controlling which SQL Server statistics are collected during query execution.
    /// </summary>
    public record QueryStatisticsOptions(
        bool IncludeIoStats = false,
        bool IncludeExecutionPlan = false);
}
