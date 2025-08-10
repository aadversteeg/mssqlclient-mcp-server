using System.Threading;
using System.Threading.Tasks;

namespace Core.Infrastructure.SqlClient.Interfaces
{
    /// <summary>
    /// Interface for detecting SQL Server capabilities based on version and edition.
    /// </summary>
    public interface ISqlServerCapabilityDetector
    {
        /// <summary>
        /// Detects the capabilities of the SQL Server instance.
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>A SqlServerCapability object containing information about the server's capabilities</returns>
        Task<SqlServerCapability> DetectCapabilitiesAsync(CancellationToken cancellationToken = default);
    }
}