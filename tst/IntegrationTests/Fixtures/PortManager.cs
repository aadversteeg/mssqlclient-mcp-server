using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace IntegrationTests.Fixtures
{
    /// <summary>
    /// Manages port allocation for Docker containers to avoid conflicts
    /// </summary>
    public class PortManager
    {
        private readonly HashSet<int> _allocatedPorts = new();
        private readonly int _startPort;
        private readonly int _endPort;

        /// <summary>
        /// Creates a new port manager with the specified port range
        /// </summary>
        /// <param name="startPort">The lower bound of the port range (inclusive)</param>
        /// <param name="endPort">The upper bound of the port range (inclusive)</param>
        public PortManager(int startPort, int endPort)
        {
            _startPort = startPort;
            _endPort = endPort;
        }

        /// <summary>
        /// Finds and reserves an available port in the configured range
        /// </summary>
        /// <returns>An available port number</returns>
        /// <exception cref="InvalidOperationException">Thrown when no ports are available in the range</exception>
        public int GetAvailablePort()
        {
            for (int port = _startPort; port <= _endPort; port++)
            {
                if (!_allocatedPorts.Contains(port) && IsPortAvailable(port))
                {
                    _allocatedPorts.Add(port);
                    return port;
                }
            }

            throw new InvalidOperationException($"No available ports in range {_startPort}-{_endPort}");
        }

        /// <summary>
        /// Gets an available port with retry logic using exponential backoff
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>An available port number</returns>
        /// <exception cref="InvalidOperationException">Thrown when port allocation fails after retries</exception>
        public int GetAvailablePortWithRetry(int maxRetries = 5)
        {
            var random = new Random();
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    return GetAvailablePort();
                }
                catch (InvalidOperationException)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                        throw;

                    // Wait with exponential backoff
                    var delay = (int)Math.Pow(2, retryCount) * 100 + random.Next(100);
                    Thread.Sleep(delay);
                }
            }

            throw new InvalidOperationException("Failed to allocate port after retries");
        }

        /// <summary>
        /// Checks if a port is available by attempting to create a TCP listener
        /// </summary>
        /// <param name="port">The port to check</param>
        /// <returns>True if the port is available, false otherwise</returns>
        private bool IsPortAvailable(int port)
        {
            try
            {
                // Attempt to create a TCP listener on the port
                using var tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start();
                tcpListener.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Release a port for reuse
        /// </summary>
        /// <param name="port">The port to release</param>
        public void ReleasePort(int port)
        {
            _allocatedPorts.Remove(port);
        }
    }
}