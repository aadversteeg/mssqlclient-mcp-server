using System.Diagnostics;

namespace Core.Application.Models
{
    /// <summary>
    /// Context for tracking tool call timeout and calculating remaining time for operations.
    /// </summary>
    public class ToolCallTimeoutContext
    {
        private readonly Stopwatch _stopwatch;
        private readonly int _totalTimeoutSeconds;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the ToolCallTimeoutContext class.
        /// </summary>
        /// <param name="totalTimeoutSeconds">Total timeout in seconds for the tool call</param>
        /// <param name="cancellationToken">Cancellation token that will be cancelled when timeout is exceeded</param>
        public ToolCallTimeoutContext(int totalTimeoutSeconds, CancellationToken cancellationToken)
        {
            _totalTimeoutSeconds = totalTimeoutSeconds;
            _cancellationToken = cancellationToken;
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets the total timeout in seconds for the tool call.
        /// </summary>
        public int TotalTimeoutSeconds => _totalTimeoutSeconds;

        /// <summary>
        /// Gets the elapsed time since the context was created.
        /// </summary>
        public TimeSpan ElapsedTime => _stopwatch.Elapsed;

        /// <summary>
        /// Gets the remaining time before the total timeout is exceeded.
        /// </summary>
        public TimeSpan RemainingTime => TimeSpan.FromSeconds(_totalTimeoutSeconds) - ElapsedTime;

        /// <summary>
        /// Gets the cancellation token that will be cancelled when the timeout is exceeded.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        /// <summary>
        /// Calculates the effective timeout for a command based on the remaining time.
        /// </summary>
        /// <param name="defaultTimeoutSeconds">The default command timeout in seconds</param>
        /// <returns>The effective timeout in seconds, ensuring at least 1 second</returns>
        public int GetEffectiveCommandTimeout(int defaultTimeoutSeconds)
        {
            var remainingSeconds = (int)Math.Ceiling(RemainingTime.TotalSeconds);
            
            // Ensure at least 1 second timeout
            if (remainingSeconds < 1)
                return 1;
            
            // Return the minimum of default timeout and remaining time
            return Math.Min(defaultTimeoutSeconds, remainingSeconds);
        }

        /// <summary>
        /// Checks if the timeout has been exceeded.
        /// </summary>
        /// <returns>True if the timeout has been exceeded, otherwise false</returns>
        public bool IsTimeoutExceeded => RemainingTime <= TimeSpan.Zero;

        /// <summary>
        /// Creates a timeout exceeded error message.
        /// </summary>
        /// <returns>A formatted error message indicating the timeout was exceeded</returns>
        public string CreateTimeoutExceededMessage()
        {
            var elapsedSeconds = (int)Math.Ceiling(ElapsedTime.TotalSeconds);
            return $"Total tool timeout of {_totalTimeoutSeconds}s exceeded after {elapsedSeconds}s";
        }
    }
}