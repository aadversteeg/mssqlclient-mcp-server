# SQL Server MCP Client

A comprehensive Microsoft SQL Server client implementing the Model Context Protocol (MCP). This server provides extensive SQL Server capabilities including query execution, schema discovery, and stored procedure management through a simple MCP interface.

## Overview

The SQL Server MCP client is built with .NET Core using the Model Context Protocol C# SDK ([github.com/modelcontextprotocol/csharp-sdk](https://github.com/modelcontextprotocol/csharp-sdk)). It provides tools for executing SQL queries, managing stored procedures, listing tables, and retrieving comprehensive schema information from SQL Server databases. The server is designed to be lightweight yet powerful, demonstrating how to create a robust MCP server with practical database functionality. It can be deployed either directly on a machine or as a Docker container.

The MCP client operates in one of two modes:
- **Database Mode**: When a specific database is specified in the connection string, only operations within that database context are available
- **Server Mode**: When no database is specified in the connection string, server-wide operations across all databases are available

## Features

### Core Database Operations
- Execute SQL queries on connected SQL Server databases
- List all tables with schema and row count information
- Retrieve detailed schema information for specific tables
- Comprehensive stored procedure management and execution

### Stored Procedure Support
- **Parameter Discovery**: Get detailed parameter information in table or JSON Schema format
- **Type-Safe Execution**: Automatic JSON-to-SQL type conversion based on parameter metadata
- **Rich Metadata**: Support for input/output parameters, default values, and data type constraints
- **Cross-Database Operations**: Execute procedures across different databases (Server Mode)

### Advanced Features
- **JSON Schema Output**: Parameter metadata compatible with validation tools
- **Case-Insensitive Parameters**: Flexible parameter naming with @ prefix normalization
- **SQL Server Feature Detection**: Comprehensive capability reporting
- **Two-Mode Architecture**: Optimized for both single-database and multi-database scenarios
- **Configurable Timeouts**: Default and per-operation timeout control with runtime management tools
- **Background Session Management**: Execute long-running queries and procedures with session-based monitoring

### Security & Configuration
- Configurable tool enablement for security
- Environment-based configuration
- Comprehensive error handling with standardized error messages
- Input validation against SQL Server metadata

## Getting Started

### Prerequisites

- .NET 9.0 SDK (for local development/deployment)
- Docker (for container deployment)

### Build Instructions (for development)

If you want to build the project from source:

1. Clone this repository:
   ```bash
   git clone https://github.com/aadversteeg/mssqlclient-mcp-server.git
   ```

2. Navigate to the source directory:
   ```bash
   cd mssqlclient-mcp-server/src
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run the tests:
   ```bash
   dotnet test
   ```

## Docker Support

### Docker Hub

The SQL Server MCP Client is available on Docker Hub.

```bash
# Pull the latest version
docker pull aadversteeg/mssqlclient-mcp-server:latest
```

### Manual Docker Build

If you need to build the Docker image yourself:

```bash
# Navigate to the repository root
cd mssqlclient-mcp-server

# Build the Docker image
docker build -f src/Core.Infrastructure.McpServer/Dockerfile -t mssqlclient-mcp-server:latest src/

# Run the locally built image
docker run -d --name mssql-mcp -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" mssqlclient-mcp-server:latest
```

### Local Registry Push

To push to your local registry:

```bash
# Build the Docker image
docker build -f src/Core.Infrastructure.McpServer/Dockerfile -t localhost:5000/mssqlclient-mcp-server:latest src/

# Push to local registry
docker push localhost:5000/mssqlclient-mcp-server:latest
```

#### Using Local Registry

If you have pushed the image to local registry running on port 5000, you can pull from it:

```bash
# Pull from local registry
docker pull localhost:5000/mssqlclient-mcp-server:latest
```

## MCP Protocol Usage

### Client Integration

To connect to the SQL Server MCP Client from your applications:

1. Use the Model Context Protocol C# SDK or any MCP-compatible client
2. Configure your client to connect to the server's endpoint
3. Call the available tools described below

### Available Tools

The available tools differ depending on which mode the server is operating in, with some tools available in both modes:

## Common Tools (Available in Both Modes)

#### server_capabilities

Returns detailed information about the capabilities and features of the connected SQL Server instance.

Example request:
```json
{
  "name": "server_capabilities",
  "parameters": {}
}
```

Example response in Server Mode:
```json
{
  "version": "Microsoft SQL Server 2019",
  "majorVersion": 15,
  "minorVersion": 0,
  "buildNumber": 4123,
  "edition": "Enterprise Edition",
  "isAzureSqlDatabase": false,
  "isAzureVmSqlServer": false,
  "isOnPremisesSqlServer": true,
  "toolMode": "server",
  "features": {
    "supportsPartitioning": true,
    "supportsColumnstoreIndex": true,
    "supportsJson": true,
    "supportsInMemoryOLTP": true,
    "supportsRowLevelSecurity": true,
    "supportsDynamicDataMasking": true,
    "supportsDataCompression": true,
    "supportsDatabaseSnapshots": true,
    "supportsQueryStore": true,
    "supportsResumableIndexOperations": true,
    "supportsGraphDatabase": true,
    "supportsAlwaysEncrypted": true,
    "supportsExactRowCount": true,
    "supportsDetailedIndexMetadata": true,
    "supportsTemporalTables": true
  }
}
```

This tool is useful for:
- Determining which features are available in your SQL Server instance
- Debugging compatibility issues
- Understanding which query patterns will be used
- Verifying whether you're in server or database mode

#### get_command_timeout

Returns current timeout configuration settings.

Example request:
```json
{
  "name": "get_command_timeout",
  "parameters": {}
}
```

Example response:
```json
{
  "defaultCommandTimeoutSeconds": 30,
  "connectionTimeoutSeconds": 15,
  "maxConcurrentSessions": 10,
  "sessionCleanupIntervalMinutes": 60,
  "totalToolCallTimeoutSeconds": 120,
  "timestamp": "2024-12-19 10:30:45 UTC"
}
```

#### set_command_timeout

Updates the default command timeout for all new operations.

**Note:** When `TotalToolCallTimeoutSeconds` is configured, the effective timeout will be the minimum of this value and the remaining total timeout. This ensures operations complete within the total tool call timeout limit.

Parameters:
- `timeoutSeconds` (required): New timeout in seconds (1-3600)

Example request:
```json
{
  "name": "set_command_timeout",
  "parameters": {
    "timeoutSeconds": 120
  }
}
```

Example response:
```json
{
  "message": "Default command timeout updated successfully",
  "oldTimeoutSeconds": 30,
  "newTimeoutSeconds": 120,
  "note": "This change only affects new operations. Existing sessions will continue with their original timeout settings.",
  "timestamp": "2024-12-19 10:31:00 UTC"
}
```

#### Session Management Tools

These tools allow management of long-running queries and stored procedures through background sessions. They are particularly useful when operations would exceed the `TotalToolCallTimeoutSeconds` limit or when you need to run multiple operations concurrently.

##### get_session_status

Check the status of a running query or stored procedure session.

Parameters:
- `sessionId` (required): The session ID to check

Example request:
```json
{
  "name": "get_session_status",
  "parameters": {
    "sessionId": 12345
  }
}
```

Example response:
```json
{
  "sessionId": 12345,
  "type": "query",
  "query": "SELECT * FROM LargeTable",
  "databaseName": "Northwind",
  "startTime": "2024-12-19 10:30:00 UTC",
  "endTime": "2024-12-19 10:35:23 UTC",
  "duration": "323.5 seconds",
  "status": "completed",
  "isRunning": false,
  "rowCount": 1500000,
  "error": null,
  "timeoutSeconds": 600
}
```

##### get_session_results

Get results from a completed or running query/stored procedure session.

Parameters:
- `sessionId` (required): The session ID to get results from
- `maxRows` (optional): Maximum number of rows to return

Example request:
```json
{
  "name": "get_session_results",
  "parameters": {
    "sessionId": 12345,
    "maxRows": 100
  }
}
```

Example response:
```json
{
  "sessionId": 12345,
  "type": "query",
  "status": "completed",
  "rowCount": 1500000,
  "results": "| CustomerID | CompanyName | ContactName |\n| ---------- | ----------- | ----------- |\n| ALFKI | Alfreds Futterkiste | Maria Anders |\n...\n... (showing first 100 rows of 1500000 total)",
  "maxRowsApplied": 100
}
```

##### stop_session

Stop a running query or stored procedure session.

Parameters:
- `sessionId` (required): The session ID to stop

Example request:
```json
{
  "name": "stop_session",
  "parameters": {
    "sessionId": 12345
  }
}
```

Example response:
```json
{
  "sessionId": 12345,
  "status": "cancelled",
  "message": "Session cancelled successfully",
  "timestamp": "2024-12-19 10:32:15 UTC"
}
```

##### list_sessions

List all query and stored procedure sessions.

Parameters:
- `status` (optional): Filter by status - "all" (default), "running", or "completed"

Example request:
```json
{
  "name": "list_sessions",
  "parameters": {
    "status": "running"
  }
}
```

Example response:
```json
{
  "filter": "running",
  "totalSessions": 2,
  "sessions": [
    {
      "sessionId": 12345,
      "type": "query",
      "query": "SELECT * FROM LargeTable...",
      "databaseName": "Northwind",
      "startTime": "2024-12-19 10:30:00 UTC",
      "duration": "45.2 seconds",
      "status": "running",
      "isRunning": true,
      "rowCount": 0,
      "hasError": false
    },
    {
      "sessionId": 12346,
      "type": "storedprocedure",
      "query": "GenerateMonthlyReport",
      "databaseName": "Sales",
      "startTime": "2024-12-19 10:25:00 UTC",
      "duration": "320.1 seconds",
      "status": "running",
      "isRunning": true,
      "rowCount": 0,
      "hasError": false
    }
  ],
  "timestamp": "2024-12-19 10:30:45 UTC"
}
```

## Database Mode Tools

When connected with a specific database in the connection string, the following tools are available:

#### execute_query

Executes a SQL query on the connected SQL Server database.

Parameters:
- `query` (required): The SQL query to execute.
- `timeoutSeconds` (optional): Command timeout in seconds. Overrides the default timeout.

Example request:
```json
{
  "name": "execute_query",
  "parameters": {
    "query": "SELECT TOP 5 * FROM Customers"
  }
}
```

Example response:
```
| CustomerID | CompanyName                      | ContactName        |
| ---------- | -------------------------------- | ------------------ |
| ALFKI      | Alfreds Futterkiste              | Maria Anders       |
| ANATR      | Ana Trujillo Emparedados y h...  | Ana Trujillo       |
| ANTON      | Antonio Moreno Taquería          | Antonio Moreno     |
| AROUT      | Around the Horn                  | Thomas Hardy       |
| BERGS      | Berglunds snabbköp               | Christina Berglund |

Total rows: 5
```

#### list_tables

Lists all tables in the connected SQL Server database with schema and row count information.

Example request:
```json
{
  "name": "list_tables",
  "parameters": {}
}
```

Example response:
```
Available Tables:

Schema | Table Name | Row Count
------ | ---------- | ---------
dbo    | Customers  | 91
dbo    | Products   | 77
dbo    | Orders     | 830
dbo    | Employees  | 9
```

#### get_table_schema

Gets the schema of a table from the connected SQL Server database.

Parameters:
- `tableName` (required): The name of the table to get schema information for.

Example request:
```json
{
  "name": "get_table_schema",
  "parameters": {
    "tableName": "Customers"
  }
}
```

Example response:
```
Schema for table: Customers

Column Name | Data Type | Max Length | Is Nullable
----------- | --------- | ---------- | -----------
CustomerID  | nchar     | 5          | NO
CompanyName | nvarchar  | 40         | NO
ContactName | nvarchar  | 30         | YES
ContactTitle| nvarchar  | 30         | YES
Address     | nvarchar  | 60         | YES
City        | nvarchar  | 15         | YES
Region      | nvarchar  | 15         | YES
PostalCode  | nvarchar  | 10         | YES
Country     | nvarchar  | 15         | YES
Phone       | nvarchar  | 24         | YES
Fax         | nvarchar  | 24         | YES
```

#### list_stored_procedures

Lists all stored procedures in the current database with detailed information.

Example request:
```json
{
  "name": "list_stored_procedures",
  "parameters": {}
}
```

Example response:
```
Available Stored Procedures in 'Northwind':

Schema   | Procedure Name                  | Parameters | Last Execution    | Execution Count | Created Date
-------- | ------------------------------- | ---------- | ----------------- | --------------- | -------------------
dbo      | GetCustomerOrders               | 2          | 2024-01-15 10:30:00 | 145           | 2023-12-01 09:00:00
dbo      | UpdateProductPrice              | 3          | 2024-01-14 16:45:00 | 89            | 2023-11-15 14:30:00
dbo      | CreateNewCustomer               | 5          | N/A               | N/A           | 2024-01-10 11:20:00
```

#### get_stored_procedure_definition

Gets the SQL definition of a stored procedure.

Parameters:
- `procedureName` (required): The name of the stored procedure.

Example request:
```json
{
  "name": "get_stored_procedure_definition",
  "parameters": {
    "procedureName": "GetCustomerOrders"
  }
}
```

#### get_stored_procedure_parameters

Gets parameter information for a stored procedure in table or JSON Schema format.

Parameters:
- `procedureName` (required): The name of the stored procedure.
- `format` (optional): Output format - "table" (default) or "json".

Example request (table format):
```json
{
  "name": "get_stored_procedure_parameters",
  "parameters": {
    "procedureName": "CreateNewCustomer",
    "format": "table"
  }
}
```

Example response (table format):
```
Parameters for stored procedure: CreateNewCustomer

| Parameter | Type | Required | Direction | Default |
|-----------|------|----------|-----------|---------|
| CompanyName | nvarchar(40) | Yes | INPUT | - |
| ContactName | nvarchar(30) | No | INPUT | NULL |
| City | nvarchar(15) | No | INPUT | NULL |
| Country | nvarchar(15) | No | INPUT | USA |

Example usage:
```json
{
  "CompanyName": "Acme Corp",
  "ContactName": "John Doe",
  "City": "Seattle",
  "Country": "USA"
}
```
```

Example request (JSON Schema format):
```json
{
  "name": "get_stored_procedure_parameters",
  "parameters": {
    "procedureName": "CreateNewCustomer",
    "format": "json"
  }
}
```

Example response (JSON Schema format):
```json
{
  "procedureName": "CreateNewCustomer",
  "description": "Parameter schema for stored procedure CreateNewCustomer",
  "parameters": {
    "type": "object",
    "properties": {
      "CompanyName": {
        "type": "string",
        "maxLength": 40,
        "sqlType": "nvarchar(40)",
        "sqlParameter": "@CompanyName",
        "position": 1,
        "isOutput": false,
        "description": "Parameter @CompanyName of type nvarchar(40)"
      },
      "ContactName": {
        "type": "string",
        "maxLength": 30,
        "sqlType": "nvarchar(30)",
        "sqlParameter": "@ContactName",
        "position": 2,
        "isOutput": false,
        "hasDefault": true,
        "defaultValue": null,
        "description": "Parameter @ContactName of type nvarchar(30)"
      },
      "Country": {
        "type": "string",
        "maxLength": 15,
        "sqlType": "nvarchar(15)",
        "sqlParameter": "@Country",
        "position": 4,
        "isOutput": false,
        "hasDefault": true,
        "defaultValue": "USA",
        "description": "Parameter @Country of type nvarchar(15)"
      }
    },
    "required": ["CompanyName"],
    "additionalProperties": false
  },
  "returnValue": {
    "type": "integer",
    "sqlType": "int",
    "description": "Return code (0 for success)"
  }
}
```

#### execute_stored_procedure

Executes a stored procedure with automatic parameter type conversion.

Parameters:
- `procedureName` (required): The name of the stored procedure.
- `parameters` (required): JSON string containing parameter values.

Example request:
```json
{
  "name": "execute_stored_procedure",
  "parameters": {
    "procedureName": "CreateNewCustomer",
    "parameters": "{\"CompanyName\": \"Acme Corp\", \"ContactName\": \"John Doe\", \"City\": \"Seattle\"}"
  }
}
```

Features:
- Automatic JSON-to-SQL type conversion based on stored procedure metadata
- Support for both `@ParameterName` and `ParameterName` formats
- Case-insensitive parameter matching
- Comprehensive error messages with parameter validation
- Support for output parameters and return values

#### start_query

Start a SQL query in the background on the connected database. Returns a session ID to check progress. Best for long-running queries.

Parameters:
- `query` (required): The SQL query to execute
- `timeoutSeconds` (optional): Optional timeout in seconds. If not specified, uses the default timeout

Example request:
```json
{
  "name": "start_query",
  "parameters": {
    "query": "SELECT * FROM LargeTable WHERE ProcessingDate >= '2024-01-01'",
    "timeoutSeconds": 600
  }
}
```

Example response:
```json
{
  "sessionId": 12345,
  "startTime": "2024-12-19 10:30:00 UTC",
  "query": "SELECT * FROM LargeTable WHERE ProcessingDate >= '2024-01-01'",
  "databaseName": "connected database",
  "timeoutSeconds": 600,
  "status": "running",
  "message": "Query started successfully. Use get_session_status to check progress."
}
```

#### start_stored_procedure

Start a stored procedure execution in the background. Returns a session ID to check progress. Best for long-running procedures.

Parameters:
- `procedureName` (required): The name of the stored procedure to execute
- `parameters` (optional): JSON object containing the parameters for the stored procedure (default: "{}")
- `timeoutSeconds` (optional): Optional timeout in seconds. If not specified, uses the default timeout

Example request:
```json
{
  "name": "start_stored_procedure",
  "parameters": {
    "procedureName": "GenerateMonthlyReport",
    "parameters": "{\"Month\": 12, \"Year\": 2024, \"IncludeDetails\": true}",
    "timeoutSeconds": 1200
  }
}
```

Example response:
```json
{
  "sessionId": 12346,
  "startTime": "2024-12-19 10:35:00 UTC",
  "procedureName": "GenerateMonthlyReport",
  "databaseName": "connected database",
  "parameters": {"Month": 12, "Year": 2024, "IncludeDetails": true},
  "timeoutSeconds": 1200,
  "status": "running",
  "message": "Stored procedure started successfully. Use get_session_status to check progress."
}
```

## Server Mode Tools

When connected without a specific database in the connection string, the following additional tools are available:

#### list_databases

Lists all databases on the SQL Server instance.

Example request:
```json
{
  "name": "list_databases",
  "parameters": {}
}
```

Example response:
```
Available Databases:

Name       | State  | Size (MB) | Owner     | Compatibility
---------- | ------ | --------- | --------- | -------------
master     | ONLINE | 10.25     | sa        | 160
tempdb     | ONLINE | 25.50     | sa        | 160
model      | ONLINE | 8.00      | sa        | 160
msdb       | ONLINE | 15.75     | sa        | 160
Northwind  | ONLINE | 45.25     | sa        | 160
```

#### execute_query_in_database

Executes a SQL query in a specific database.

Parameters:
- `databaseName` (required): The name of the database to execute the query in.
- `query` (required): The SQL query to execute.

Example request:
```json
{
  "name": "execute_query_in_database",
  "parameters": {
    "databaseName": "Northwind",
    "query": "SELECT TOP 5 * FROM Customers"
  }
}
```

#### list_tables_in_database

Lists all tables in a specific database.

Parameters:
- `databaseName` (required): The name of the database to list tables from.

Example request:
```json
{
  "name": "list_tables_in_database",
  "parameters": {
    "databaseName": "Northwind"
  }
}
```

#### get_table_schema_in_database

Gets the schema of a table from a specific database.

Parameters:
- `databaseName` (required): The name of the database containing the table.
- `tableName` (required): The name of the table to get schema information for.

Example request:
```json
{
  "name": "get_table_schema_in_database",
  "parameters": {
    "databaseName": "Northwind",
    "tableName": "Customers"
  }
}
```

#### list_stored_procedures_in_database

Lists all stored procedures in a specific database.

Parameters:
- `databaseName` (required): The name of the database to list stored procedures from.

Example request:
```json
{
  "name": "list_stored_procedures_in_database",
  "parameters": {
    "databaseName": "Northwind"
  }
}
```

#### get_stored_procedure_definition_in_database

Gets the SQL definition of a stored procedure from a specific database.

Parameters:
- `databaseName` (required): The name of the database containing the stored procedure.
- `procedureName` (required): The name of the stored procedure.

Example request:
```json
{
  "name": "get_stored_procedure_definition_in_database",
  "parameters": {
    "databaseName": "Northwind",
    "procedureName": "GetCustomerOrders"
  }
}
```

#### get_stored_procedure_parameters (Server Mode)

Gets parameter information for a stored procedure from any database.

Parameters:
- `procedureName` (required): The name of the stored procedure.
- `databaseName` (optional): The name of the database containing the stored procedure.
- `format` (optional): Output format - "table" (default) or "json".

Example request:
```json
{
  "name": "get_stored_procedure_parameters",
  "parameters": {
    "procedureName": "CreateNewCustomer",
    "databaseName": "Northwind",
    "format": "json"
  }
}
```

#### execute_stored_procedure_in_database

Executes a stored procedure in a specific database with automatic parameter type conversion.

Parameters:
- `databaseName` (required): The name of the database containing the stored procedure.
- `procedureName` (required): The name of the stored procedure.
- `parameters` (required): JSON string containing parameter values.

Example request:
```json
{
  "name": "execute_stored_procedure_in_database",
  "parameters": {
    "databaseName": "Northwind",
    "procedureName": "CreateNewCustomer",
    "parameters": "{\"CompanyName\": \"Acme Corp\", \"ContactName\": \"John Doe\"}"
  }
}
```

#### start_query_in_database

Start a SQL query in the background for a specific database. Returns a session ID to check progress. Best for long-running queries (server mode).

Parameters:
- `databaseName` (required): The name of the database to execute the query in
- `query` (required): The SQL query to execute
- `timeoutSeconds` (optional): Optional timeout in seconds. If not specified, uses the default timeout

Example request:
```json
{
  "name": "start_query_in_database",
  "parameters": {
    "databaseName": "DataWarehouse",
    "query": "EXEC sp_refreshview 'vw_SalesSummary'; SELECT * FROM vw_SalesSummary",
    "timeoutSeconds": 900
  }
}
```

Example response:
```json
{
  "sessionId": 12347,
  "startTime": "2024-12-19 10:40:00 UTC",
  "query": "EXEC sp_refreshview 'vw_SalesSummary'; SELECT * FROM vw_SalesSummary",
  "databaseName": "DataWarehouse",
  "timeoutSeconds": 900,
  "status": "running",
  "message": "Query started successfully. Use get_session_status to check progress."
}
```

#### start_stored_procedure_in_database

Start a stored procedure execution in the background for a specific database. Returns a session ID to check progress. Best for long-running procedures (server mode).

Parameters:
- `databaseName` (required): The name of the database containing the stored procedure
- `procedureName` (required): The name of the stored procedure to execute
- `parameters` (optional): JSON object containing the parameters for the stored procedure (default: "{}")
- `timeoutSeconds` (optional): Optional timeout in seconds. If not specified, uses the default timeout

Example request:
```json
{
  "name": "start_stored_procedure_in_database",
  "parameters": {
    "databaseName": "Analytics",
    "procedureName": "sp_BuildDataMart",
    "parameters": "{\"StartDate\": \"2024-01-01\", \"EndDate\": \"2024-12-31\", \"RebuildIndexes\": true}",
    "timeoutSeconds": 3600
  }
}
```

Example response:
```json
{
  "sessionId": 12348,
  "startTime": "2024-12-19 10:45:00 UTC",
  "procedureName": "sp_BuildDataMart",
  "databaseName": "Analytics",
  "parameters": {"StartDate": "2024-01-01", "EndDate": "2024-12-31", "RebuildIndexes": true},
  "timeoutSeconds": 3600,
  "status": "running",
  "message": "Stored procedure started successfully. Use get_session_status to check progress."
}
```

## Configuration

### Tool Security Configuration

The server provides granular control over which potentially dangerous operations are available:

#### Query Execution Security

By default, SQL query execution tools are disabled for security reasons. To enable these tools, set the `EnableExecuteQuery` configuration setting to `true`.

#### Stored Procedure Execution Security

By default, stored procedure execution tools are disabled for security reasons. To enable these tools, set the `EnableExecuteStoredProcedure` configuration setting to `true`.

#### Session-Based Execution Security

By default, session-based query execution tools are disabled for security reasons. To enable these tools, set the `EnableStartQuery` configuration setting to `true`.

By default, session-based stored procedure execution tools are disabled for security reasons. To enable these tools, set the `EnableStartStoredProcedure` configuration setting to `true`.

These can be configured in several ways:

1. In the `appsettings.json` file:
```json
{
  "DatabaseConfiguration": {
    "EnableExecuteQuery": true,
    "EnableExecuteStoredProcedure": true,
    "EnableStartQuery": true,
    "EnableStartStoredProcedure": true
  }
}
```

2. As environment variables when running the container:
```bash
docker run \
  -e "DatabaseConfiguration__EnableExecuteQuery=true" \
  -e "DatabaseConfiguration__EnableExecuteStoredProcedure=true" \
  -e "DatabaseConfiguration__EnableStartQuery=true" \
  -e "DatabaseConfiguration__EnableStartStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;..." \
  aadversteeg/mssqlclient-mcp-server:latest
```

3. In the Claude Desktop configuration:
```json
"mssql": {
  "command": "dotnet",
  "args": [
    "YOUR_PATH_TO_DLL\\Core.Infrastructure.McpServer.dll"
  ],
  "env": {
    "MSSQL_CONNECTIONSTRING": "Server=your_server;...",
    "DatabaseConfiguration__EnableExecuteQuery": "true",
    "DatabaseConfiguration__EnableExecuteStoredProcedure": "true",
    "DatabaseConfiguration__EnableStartQuery": "true",
    "DatabaseConfiguration__EnableStartStoredProcedure": "true"
  }
}
```

When these settings are `false` (the default), the respective execution tools will not be registered and will not be available to clients. This provides additional security layers when you only want to allow read-only operations.

### Timeout Configuration

The SQL Server MCP Client provides comprehensive timeout configuration at multiple levels to handle various workload requirements.

#### Default Timeout Settings

Configure default timeouts in `appsettings.json`:

```json
{
  "DatabaseConfiguration": {
    "DefaultCommandTimeoutSeconds": 30,
    "ConnectionTimeoutSeconds": 15,
    "MaxConcurrentSessions": 10,
    "SessionCleanupIntervalMinutes": 60,
    "TotalToolCallTimeoutSeconds": 120
  }
}
```

**Timeout settings:**
- `DefaultCommandTimeoutSeconds`: Default timeout for SQL command execution (default: 30 seconds)
- `ConnectionTimeoutSeconds`: Timeout for establishing SQL connections (default: 15 seconds)
- `MaxConcurrentSessions`: Maximum number of concurrent query sessions (default: 10)
- `SessionCleanupIntervalMinutes`: Interval for cleaning up completed sessions (default: 60 minutes)
- `TotalToolCallTimeoutSeconds`: Maximum time allowed for any tool call to complete (default: 120 seconds, set to null to disable)

These can also be set via environment variables:

```bash
# Docker example
docker run \
  -e "DatabaseConfiguration__DefaultCommandTimeoutSeconds=60" \
  -e "DatabaseConfiguration__ConnectionTimeoutSeconds=30" \
  -e "DatabaseConfiguration__TotalToolCallTimeoutSeconds=180" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;..." \
  aadversteeg/mssqlclient-mcp-server:latest

# Claude Desktop configuration
"mssql": {
  "command": "docker",
  "args": ["run", "--rm", "-i",
    "-e", "DatabaseConfiguration__DefaultCommandTimeoutSeconds=60",
    "-e", "DatabaseConfiguration__ConnectionTimeoutSeconds=30",
    "-e", "DatabaseConfiguration__TotalToolCallTimeoutSeconds=180",
    "-e", "MSSQL_CONNECTIONSTRING=Server=your_server;...",
    "aadversteeg/mssqlclient-mcp-server:latest"
  ]
}
```

#### Tool Call Timeout Management

The `TotalToolCallTimeoutSeconds` setting provides a safety mechanism to prevent tools from running indefinitely:

**How it works:**
- Sets a maximum time limit for any single tool call to complete
- If exceeded, the operation is cancelled with a clear timeout error message
- Helps prevent hanging operations and ensures responsive behavior
- Works in conjunction with per-operation timeouts for fine-grained control

**Configuration considerations:**
- **MCP Client Limits**: Most MCP clients (like Claude Desktop) have connection timeouts of 2-5 minutes
- **Best Practice**: Set `TotalToolCallTimeoutSeconds` below your client's timeout for best user experience (typically 90-120 seconds)
- **Long Operations**: For operations requiring more time, use session-based tools (`start_query`, `start_stored_procedure`)
- **Disable if needed**: Set to `null` to disable the total timeout limit

**Example configuration:**
```json
{
  "TotalToolCallTimeoutSeconds": 90,  // 1.5 minutes - good for most operations
  "DefaultCommandTimeoutSeconds": 30  // Default timeout for individual SQL commands
}
```

This configuration ensures:
- No tool call runs longer than 90 seconds total
- Individual SQL commands default to 30 seconds timeout
- Long-running operations should use session-based tools instead

#### Runtime Timeout Management

The server provides tools to manage timeouts dynamically:

##### get_command_timeout

Returns current timeout configuration settings.

Example request:
```json
{
  "name": "get_command_timeout",
  "parameters": {}
}
```

Example response:
```json
{
  "defaultCommandTimeoutSeconds": 30,
  "connectionTimeoutSeconds": 15,
  "maxConcurrentSessions": 10,
  "sessionCleanupIntervalMinutes": 60,
  "totalToolCallTimeoutSeconds": 120,
  "timestamp": "2024-12-19 10:30:45 UTC"
}
```

##### set_command_timeout

Updates the default command timeout for all new operations. Existing operations continue with their original timeout.

Parameters:
- `timeoutSeconds` (required): New timeout in seconds (1-3600)

Example request:
```json
{
  "name": "set_command_timeout",
  "parameters": {
    "timeoutSeconds": 120
  }
}
```

Example response:
```json
{
  "message": "Default command timeout updated successfully",
  "oldTimeoutSeconds": 30,
  "newTimeoutSeconds": 120,
  "note": "This change only affects new operations. Existing sessions will continue with their original timeout settings.",
  "timestamp": "2024-12-19 10:31:00 UTC"
}
```

#### Per-Operation Timeouts

Most database operations support an optional `timeoutSeconds` parameter that overrides the default timeout for that specific operation:

```json
// Long-running query with 5-minute timeout
{
  "name": "execute_query",
  "parameters": {
    "query": "SELECT * FROM LargeTable WITH (NOLOCK)",
    "timeoutSeconds": 300
  }
}

// Complex stored procedure with 10-minute timeout
{
  "name": "execute_stored_procedure",
  "parameters": {
    "procedureName": "GenerateMonthlyReport",
    "parameters": "{}",
    "timeoutSeconds": 600
  }
}

// Quick table list with 10-second timeout
{
  "name": "list_tables",
  "parameters": {
    "timeoutSeconds": 10
  }
}
```

**Tools supporting per-operation timeouts:**
- All query execution tools (`execute_query`, `execute_query_in_database`)
- All stored procedure tools (`execute_stored_procedure`, `execute_stored_procedure_in_database`, `get_stored_procedure_parameters`)
- All schema discovery tools (`list_tables`, `get_table_schema`, `list_stored_procedures`)
- Session management tools (`start_query`, `start_stored_procedure`, `start_query_in_database`, `start_stored_procedure_in_database`)

#### Best Practices

1. **Default Configuration**: Set reasonable defaults in `appsettings.json` based on your typical workload
2. **Total Timeout**: Set `TotalToolCallTimeoutSeconds` to 90-120 seconds for optimal MCP client compatibility
3. **Long Operations**: Use per-operation timeouts for known long-running queries or procedures
4. **Dynamic Adjustment**: Use `set_command_timeout` when working with varying workloads throughout the day
5. **Monitoring**: Use `get_command_timeout` to verify current settings before running critical operations
6. **Background Operations**: For operations that exceed timeout limits, use the session-based tools:
   - `start_query` / `start_query_in_database` for long-running queries
   - `start_stored_procedure` / `start_stored_procedure_in_database` for long-running procedures
   - Monitor progress with `get_session_status`
   - Retrieve results with `get_session_results`
   - Cancel if needed with `stop_session`
   - These tools bypass the `TotalToolCallTimeoutSeconds` limit and run in the background

#### Timeout Limits

- **Command Timeout**: 1-3600 seconds (1 hour maximum)
- **Connection Timeout**: Configured at startup only (no runtime changes)
- **Per-Operation Override**: Always takes precedence over default settings

### Database Connection String

The SQL Server connection string is required to connect to your database. This connection string should include server information, authentication details, and any required connection options.

You can set the connection string using the `MSSQL_CONNECTIONSTRING` environment variable:

```bash
# Database Mode with all execution types enabled
docker run \
  -e "DatabaseConfiguration__EnableExecuteQuery=true" \
  -e "DatabaseConfiguration__EnableExecuteStoredProcedure=true" \
  -e "DatabaseConfiguration__EnableStartQuery=true" \
  -e "DatabaseConfiguration__EnableStartStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" \
  aadversteeg/mssqlclient-mcp-server:latest

# Server Mode with all execution types enabled
docker run \
  -e "DatabaseConfiguration__EnableExecuteQuery=true" \
  -e "DatabaseConfiguration__EnableExecuteStoredProcedure=true" \
  -e "DatabaseConfiguration__EnableStartQuery=true" \
  -e "DatabaseConfiguration__EnableStartStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;User Id=your_user;Password=your_password;TrustServerCertificate=True;" \
  aadversteeg/mssqlclient-mcp-server:latest
```

#### Server Mode vs Database Mode

The MCP server automatically detects the mode based on the connection string:

- **Server Mode**: When no database is specified in the connection string (no `Database=` or `Initial Catalog=` parameter)
- **Database Mode**: When a specific database is specified in the connection string

Example connection strings:

```
# Database Mode - Connects to specific database
Server=database.example.com;Database=Northwind;User Id=sa;Password=YourPassword;TrustServerCertificate=True;

# Server Mode - No specific database
Server=database.example.com;User Id=sa;Password=YourPassword;TrustServerCertificate=True;

# Database Mode with Windows Authentication
Server=database.example.com;Database=Northwind;Integrated Security=SSPI;TrustServerCertificate=True;

# Server Mode with specific port
Server=database.example.com,1433;User Id=sa;Password=YourPassword;TrustServerCertificate=True;
```

If no connection string is provided, the server will return an error message when attempting to use the tools.

> **Note:** Integrated Security (Windows Authentication) is not supported when running in Docker containers. Use SQL Server authentication instead.

## Configuring Claude Desktop

### Using Local Installation

To configure Claude Desktop to use a locally installed SQL Server MCP client:

1. Add the server configuration to the `mcpServers` section in your Claude Desktop configuration:
```json
"mssql": {
  "command": "dotnet",
  "args": [
    "YOUR_PATH_TO_DLL\\Core.Infrastructure.McpServer.dll"
  ],
  "env": {
    "MSSQL_CONNECTIONSTRING": "Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;",
    "DatabaseConfiguration__EnableExecuteQuery": "true",
    "DatabaseConfiguration__EnableExecuteStoredProcedure": "true",
    "DatabaseConfiguration__EnableStartQuery": "true",
    "DatabaseConfiguration__EnableStartStoredProcedure": "true"
  }
}
```

2. Save the file and restart Claude Desktop

### Using Docker Container

To use the SQL Server MCP client from a Docker container with Claude Desktop:

1. Add the server configuration to the `mcpServers` section in your Claude Desktop configuration:
```json
"mssql": {
  "command": "docker",
  "args": [
    "run",
    "--rm",
    "-i",
    "-e", "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;",
    "-e", "DatabaseConfiguration__EnableExecuteQuery=true",
    "-e", "DatabaseConfiguration__EnableExecuteStoredProcedure=true",
    "-e", "DatabaseConfiguration__EnableStartQuery=true",
    "-e", "DatabaseConfiguration__EnableStartStoredProcedure=true",
    "aadversteeg/mssqlclient-mcp-server:latest"
  ]
}
```

2. Save the file and restart Claude Desktop

> **Note for Windows Users with Local SQL Server:** When using Docker Desktop on Windows to connect to a local SQL Server instance, ensure that TCP/IP is enabled in SQL Server Configuration Manager (SQL Server Network Configuration → Protocols for MSSQLSERVER → TCP/IP) and that SQL Server is configured to listen on port 1433 (TCP/IP Properties → IP Addresses → IPAll → TCP Port: 1433). Restart the SQL Server service after making these changes.

## Architecture

### Interface Design

The server implements a three-tier interface architecture for clean separation of concerns:

1. **IDatabaseService** (Core Layer)
   - Low-level database operations without timeout context
   - Direct SQL Server communication
   - Connection and command management

2. **IServerDatabase** (Server Mode Layer)
   - Server-wide operations across databases
   - Includes timeout context management
   - Database switching and cross-database queries

3. **IDatabaseContext** (Database Mode Layer)
   - Database-scoped operations
   - Simplified interface for single-database scenarios
   - Includes timeout context management

### Timeout Management

The server uses a unified timeout management system:

- **ToolCallTimeoutContext**: Nullable parameter in all high-level interfaces
- **Simplified API**: Single method signature with optional timeout context
- **Clean Design**: No method overloading - nullable parameters provide flexibility
- **Consistent Error Handling**: Standardized error format: `"Error: SQL error while {operation}: {message}"`

### Type System

The server includes a sophisticated type mapping system that converts JSON values to appropriate SQL Server types based on stored procedure parameter metadata:

- **Automatic Type Detection**: Uses SQL Server's `sys.parameters` metadata as the authoritative source
- **Rich Type Support**: Handles all major SQL Server data types including varchar, nvarchar, int, decimal, datetime, uniqueidentifier, etc.
- **Validation**: Provides detailed error messages for type mismatches and constraint violations
- **Default Values**: Supports parameters with default values and optional parameters

### Parameter Handling

- **Case-Insensitive**: Parameter names are matched case-insensitively
- **Flexible Naming**: Supports both `@ParameterName` and `ParameterName` formats
- **Normalization**: Automatic parameter name normalization and validation
- **JSON Schema**: Generates JSON Schema compatible output for parameter validation

### Security Model

The server implements a multi-layered security approach:

1. **Tool-Level Security**: Individual tools can be enabled/disabled via configuration
2. **Parameter Validation**: All inputs are validated against SQL Server metadata
3. **SQL Injection Protection**: Uses parameterized queries throughout
4. **Connection Security**: Supports all SQL Server authentication methods

### Technology Stack

- **Framework**: .NET 9.0 with C# 13
- **Language Features**: Nullable reference types, async/await, records
- **Database Access**: Microsoft.Data.SqlClient
- **MCP SDK**: Model Context Protocol C# SDK
- **Testing**: xUnit with Moq for comprehensive unit testing
- **Containerization**: Multi-stage Docker builds for optimized images

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.