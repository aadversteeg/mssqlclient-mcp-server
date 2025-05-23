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

### Security & Configuration
- Configurable tool enablement for security
- Environment-based configuration
- Comprehensive error handling and validation

## Getting Started

### Prerequisites

- .NET 9.0 (for local development/deployment)
- Docker (for container deployment)

### Build Instructions (for development)

If you want to build the project from source:

1. Clone this repository:
   ```bash
   git clone https://github.com/aadversteeg/mssqlclient-mcp-server.git
   ```

2. Navigate to the project root directory:
   ```bash
   cd mssqlclient-mcp-server
   ```

3. Build the project using:
   ```bash
   dotnet build src/mssqlclient.sln
   ```

4. Run the tests:
   ```bash
   dotnet test src/mssqlclient.sln
   ```

## Docker Support

### Local Registry

The SQL Server MCP Client is available in your local registry at port 5000.

```bash
# Pull the latest version
docker pull localhost:5000/mssqlclient-mcp-server:latest
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

## Database Mode Tools

When connected with a specific database in the connection string, the following tools are available:

#### execute_query

Executes a SQL query on the connected SQL Server database.

Parameters:
- `query` (required): The SQL query to execute.

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

## Configuration

### Tool Security Configuration

The server provides granular control over which potentially dangerous operations are available:

#### Query Execution Security

By default, SQL query execution tools are disabled for security reasons. To enable these tools, set the `EnableExecuteQuery` configuration setting to `true`.

#### Stored Procedure Execution Security

By default, stored procedure execution tools are disabled for security reasons. To enable these tools, set the `EnableExecuteStoredProcedure` configuration setting to `true`.

These can be configured in several ways:

1. In the `appsettings.json` file:
```json
{
  "EnableExecuteQuery": true,
  "EnableExecuteStoredProcedure": true
}
```

2. As environment variables when running the container:
```bash
docker run \
  -e "EnableExecuteQuery=true" \
  -e "EnableExecuteStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;..." \
  localhost:5000/mssqlclient-mcp-server:latest
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
    "EnableExecuteQuery": "true",
    "EnableExecuteStoredProcedure": "true"
  }
}
```

When these settings are `false` (the default), the respective execution tools will not be registered and will not be available to clients. This provides additional security layers when you only want to allow read-only operations.

### Database Connection String

The SQL Server connection string is required to connect to your database. This connection string should include server information, authentication details, and any required connection options.

You can set the connection string using the `MSSQL_CONNECTIONSTRING` environment variable:

```bash
# Database Mode with both execution types enabled
docker run \
  -e "EnableExecuteQuery=true" \
  -e "EnableExecuteStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;Database=your_db;User Id=your_user;Password=your_password;TrustServerCertificate=True;" \
  localhost:5000/mssqlclient-mcp-server:latest

# Server Mode with both execution types enabled
docker run \
  -e "EnableExecuteQuery=true" \
  -e "EnableExecuteStoredProcedure=true" \
  -e "MSSQL_CONNECTIONSTRING=Server=your_server;User Id=your_user;Password=your_password;TrustServerCertificate=True;" \
  localhost:5000/mssqlclient-mcp-server:latest
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

> Integrated security will not work from a docker container!

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
    "EnableExecuteQuery": "true",
    "EnableExecuteStoredProcedure": "true"
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
    "-e", "EnableExecuteQuery=true",
    "-e", "EnableExecuteStoredProcedure=true",
    "localhost:5000/mssqlclient-mcp-server:latest"
  ]
}
```

2. Save the file and restart Claude Desktop

## Architecture

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

1. **Tool-Level Security**: Individual tools can be enabled/disabled
2. **Parameter Validation**: All inputs are validated against SQL Server metadata
3. **SQL Injection Protection**: Uses parameterized queries throughout
4. **Connection Security**: Supports all SQL Server authentication methods

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.