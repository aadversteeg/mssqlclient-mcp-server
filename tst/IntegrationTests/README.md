# MCP Integration Tests

This project contains integration tests for the SQL Server MCP server. These tests verify that the MCP server can properly connect to SQL Server and execute commands.

## Prerequisites

- .NET 9 SDK
- Docker (for running containers)

## Running the Tests

### Running All Tests

The tests automatically start and manage all required Docker containers:

```shell
cd /mnt/d/ai/mcp/src/mssqlclient-mcp-server/tst/IntegrationTests
dotnet test
```

This will:
1. Automatically start a SQL Server container using Docker
2. Automatically build the MCP server if needed
3. Run ALL tests, ensuring everything passes
4. Clean up all containers when done

### Running Specific Test Categories

You can use test filters to run specific categories of tests:

```shell
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only SQL integration tests
dotnet test --filter "Category=SQL"

# Run only MCP integration tests
dotnet test --filter "Category=MCP"

# Run all integration tests
dotnet test --filter "TestType=Integration"
```

The system has multiple robust features:
- If docker-compose fails, it will try direct `docker run` commands
- If container startup fails, it will use fallback connection strings
- SQL tests include retry logic to handle container startup delays
- MCP tests will automatically build the MCP server if it's not found
- Test categories can be filtered using the --filter option

Everything is automatic - no need to manually start containers or build anything!

### MCP Integration Tests

The MCP integration tests (MCP-INT-xxx) run automatically with all other tests:

1. If the MCP server executable isn't found, it will be built automatically
2. All MCP integration tests will be run against the SQL Server
3. The tests verify MCP methods for database operations

These tests ensure that the MCP server can properly communicate with SQL Server and execute commands.


## Test Design

The tests are designed to handle both master database and user database connections:

1. **Master Database Tests**
   - List all databases on the server
   - List tables in a specific database
   - Execute queries in specific databases
   - Get table schema from specific databases

2. **User Database Tests**
   - List tables in the current database
   - Execute queries in the current database
   - Get table schema for tables in the current database

The tests automatically adapt to the connection type (master or user database) and use the appropriate MCP methods.

## MCP Method Names

The MCP server uses different method names depending on whether it's connected to the master database or a user database:

### Master Database Methods
- `list_databases` - List all databases on the server
- `list_tables_in_database` - List tables in a specific database
- `execute_query_in_database` - Execute a query in a specific database
- `get_table_schema_in_database` - Get schema for a table in a specific database

### User Database Methods
- `list_tables` - List tables in the current database
- `execute_query` - Execute a query in the current database
- `get_table_schema` - Get schema for a table in the current database

## Test Execution Requirements

The tests have different requirements:

- SQL Connection tests require a running SQL Server instance (automatically started via Docker)
- MCP Integration tests require both SQL Server and the MCP server executable (automatically built if needed)
- MCP Client unit tests don't require any external services (they use mocking)

All tests are run by default, with robust retry logic and clear error reporting if anything fails.