@echo off
cd /d %~dp0Core.Infrastructure.McpServer\Tools

echo Removing unnecessary tool files...

del GetDefaultConstraintDefinitionTool.cs
del GetFunctionDefinitionTool.cs
del GetStoredProcedureDefinitionTool.cs
del GetTableConstraintsTool.cs
del GetTableIndexesTool.cs
del GetTableStatisticsTool.cs
del GetTriggerDefinitionTool.cs
del GetViewDefinitionTool.cs
del ListFunctionsTool.cs
del ListRelationshipsTool.cs
del ListSchemasTool.cs
del ListStoredProceduresTool.cs
del ListTriggersTool.cs
del ListViewsTool.cs

echo Cleanup complete!