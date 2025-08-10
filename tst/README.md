# MSSQL Client MCP Integration Tests

This project contains integration tests for the MSSQL Client MCP Server, using Docker containers to provide a consistent, isolated testing environment.

## Overview

The integration tests:

1. Dynamically spin up Docker containers for SQL Server and the MCP Server
2. Use port allocation to avoid conflicts with existing services
3. Execute tests against the containerized services
4. Clean up containers when tests complete

## Prerequisites

- Docker and Docker Compose
- .NET 9.0 SDK
- SQL Server Docker image (pulled automatically)

## Running the Tests

### Local Development

To run the integration tests locally:

1. Navigate to the test directory:
   ```
   cd /mnt/d/ai/mcp/src/mssqlclient-mcp-server/tst
   ```

2. Build the test project:
   ```
   dotnet build IntegrationTests/IntegrationTests.csproj
   ```

3. Run the tests:
   ```
   dotnet test
   ```

The tests will automatically:
- Allocate free ports for services
- Start Docker containers
- Run the tests
- Clean up containers when finished

### Configuration Options

You can customize the test environment by modifying `appsettings.test.json`:

- `UseExistingContainers`: Set to `true` to use already-running containers (useful for debugging)
- `UseLocalSqlServer`: Set to `true` to use a local SQL Server instance instead of Docker
- `LocalSqlServerConnectionString`: Connection string for local SQL Server
- Port ranges and container settings can also be customized

### Running with Existing Containers

If you want to debug the tests without rebuilding containers each time:

1. Start the containers manually:
   ```
   cd /mnt/d/ai/mcp/src/mssqlclient-mcp-server/tst/IntegrationTests
   docker-compose -f Docker/docker-compose.yml up -d
   ```

2. Set `UseExistingContainers` to `true` in `appsettings.test.json`

3. Run the tests:
   ```
   dotnet test
   ```

4. When finished, stop the containers:
   ```
   docker-compose -f Docker/docker-compose.yml down -v
   ```

## Test Structure

### Directory Organization

- `/Docker`: Contains Docker-related files (docker-compose.yml, Dockerfile.mcp)
- `/Fixtures`: Test fixtures for Docker and MCP Server management
- `/Tests`: Actual test classes

### Key Components

- `PortManager`: Handles dynamic port allocation to avoid conflicts
- `DockerFixture`: Manages Docker container lifecycle
- `McpServerFixture`: Handles MCP Server connections
- `MssqlClientTests`: Tests for the MSSQL Client MCP Server

## Adding New Tests

When adding new tests:

1. Create a new file in the `/Tests` directory
2. Inherit from `IClassFixture<McpServerFixture>` to use the MCP server
3. Use the `[Collection("Docker Resources")]` attribute to ensure tests don't run in parallel
4. Follow the AAA pattern (Arrange, Act, Assert)
5. Use descriptive test names with the pattern `[INITIALS]-[NUMBER]: Description`

## CI/CD Integration

This test suite can be integrated with GitHub Actions for continuous integration. The workflow file can be found in `.github/workflows/integration-tests.yml`.

## Troubleshooting

### Port Conflicts

If you encounter port conflicts:

1. Check if the ports specified in `appsettings.test.json` are already in use
2. Modify the port ranges to use different ports
3. Kill any orphaned Docker containers from previous test runs

### Docker Issues

If containers fail to start:

1. Check Docker logs: `docker logs mssql-test-sql-server`
2. Verify Docker is running properly
3. Try running `docker-compose -f Docker/docker-compose.yml down -v` to clean up any orphaned resources

### Using Test Output for Debugging

Test output logs can help identify issues:

1. Run tests with increased verbosity:
   ```
   dotnet test -v n
   ```

2. Check Docker container logs:
   ```
   docker logs mssql-test-mcp-server
   ```