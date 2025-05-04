using Core.Infrastructure.McpServer.Configuration;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;

namespace Ave.McpServer.MSSQL.Tools
{
    [McpServerToolType]
    public class GetViewDefinitionTool
    {
        private readonly string? _connectionString;

        public GetViewDefinitionTool(DatabaseConfiguration dbConfig)
        {
            _connectionString = dbConfig.ConnectionString;
            Console.Error.WriteLine($"GetViewDefinitionTool constructed with connection string: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
        }

        [McpServerTool(Name = "get_view_definition"), Description("Get the SQL definition of a view.")]
        public string GetViewDefinition(string viewName)
        {
            Console.Error.WriteLine($"GetViewDefinition called with viewName: {viewName}");
            Console.Error.WriteLine($"Connection string is: {(string.IsNullOrEmpty(_connectionString) ? "missing" : "present")}");
            
            if (string.IsNullOrEmpty(_connectionString))
            {
                return "Error: No connection string provided. Set the MSSQL_CONNECTIONSTRING environment variable.";
            }

            if (string.IsNullOrWhiteSpace(viewName))
            {
                return "Error: View name cannot be empty";
            }

            try
            {
                using SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                
                // Get schema and view name
                string schemaName = "dbo"; // Default schema
                string vName = viewName;
                
                // If there's a schema specifier in the view name
                if (viewName.Contains('.'))
                {
                    string[] parts = viewName.Split('.', 2);
                    schemaName = parts[0];
                    vName = parts[1];
                }
                
                // Check if the view exists
                string checkQuery = @"
                    SELECT 
                        COUNT(*) 
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    WHERE 
                        v.name = @ViewName
                        AND s.name = @SchemaName";
                
                using SqlCommand checkCommand = new SqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@ViewName", vName);
                checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                int viewCount = (int)checkCommand.ExecuteScalar();
                
                if (viewCount == 0)
                {
                    return $"Error: View '{schemaName}.{vName}' not found in the database.";
                }
                
                // Check if the view is encrypted
                string encryptedQuery = @"
                    SELECT 
                        m.is_encrypted 
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.sql_modules m ON v.object_id = m.object_id
                    WHERE 
                        v.name = @ViewName
                        AND s.name = @SchemaName";
                
                using SqlCommand encryptedCommand = new SqlCommand(encryptedQuery, connection);
                encryptedCommand.Parameters.AddWithValue("@ViewName", vName);
                encryptedCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                bool isEncrypted = Convert.ToBoolean(encryptedCommand.ExecuteScalar());
                
                if (isEncrypted)
                {
                    return $"The view '{schemaName}.{vName}' is encrypted and its definition cannot be viewed.";
                }
                
                // Get the view definition
                string definitionQuery = @"
                    SELECT 
                        m.definition
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    INNER JOIN 
                        sys.sql_modules m ON v.object_id = m.object_id
                    WHERE 
                        v.name = @ViewName
                        AND s.name = @SchemaName";
                
                using SqlCommand definitionCommand = new SqlCommand(definitionQuery, connection);
                definitionCommand.Parameters.AddWithValue("@ViewName", vName);
                definitionCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                string? definition = (string?)definitionCommand.ExecuteScalar();
                
                if (string.IsNullOrEmpty(definition))
                {
                    return $"The view '{schemaName}.{vName}' has no definition.";
                }
                
                // Get view columns
                string columnQuery = @"
                    SELECT 
                        c.name AS ColumnName,
                        t.name AS DataType,
                        c.max_length AS MaxLength,
                        c.precision AS Precision,
                        c.scale AS Scale,
                        c.is_nullable AS IsNullable,
                        CASE 
                            WHEN c.is_computed = 1 THEN 'Yes'
                            ELSE 'No'
                        END AS IsComputed
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    INNER JOIN 
                        sys.columns c ON v.object_id = c.object_id
                    INNER JOIN 
                        sys.types t ON c.user_type_id = t.user_type_id
                    WHERE 
                        v.name = @ViewName
                        AND s.name = @SchemaName
                    ORDER BY 
                        c.column_id";
                
                using SqlCommand columnCommand = new SqlCommand(columnQuery, connection);
                columnCommand.Parameters.AddWithValue("@ViewName", vName);
                columnCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                // Get view metadata
                string metadataQuery = @"
                    SELECT 
                        v.create_date AS CreateDate,
                        v.modify_date AS ModifyDate,
                        OBJECT_DEFINITION(v.object_id) AS ViewDefinition,
                        OBJECTPROPERTY(v.object_id, 'IsSchemaBound') AS IsSchemaBound,
                        OBJECTPROPERTYEX(v.object_id, 'BaseType') AS BaseType,
                        v.has_opaque_metadata AS HasOpaqueMetadata
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    WHERE 
                        v.name = @ViewName
                        AND s.name = @SchemaName";
                
                using SqlCommand metadataCommand = new SqlCommand(metadataQuery, connection);
                metadataCommand.Parameters.AddWithValue("@ViewName", vName);
                metadataCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                // Get view indexes
                string indexesQuery = @"
                    SELECT 
                        i.name AS IndexName,
                        i.type_desc AS IndexType,
                        i.is_unique AS IsUnique,
                        i.is_primary_key AS IsPrimaryKey
                    FROM 
                        sys.views v
                    INNER JOIN 
                        sys.schemas s ON v.schema_id = s.schema_id
                    LEFT JOIN 
                        sys.indexes i ON v.object_id = i.object_id
                    WHERE 
                        v.name = @ViewName
                        AND s.name = @SchemaName
                        AND i.name IS NOT NULL
                    ORDER BY 
                        i.name";
                
                using SqlCommand indexesCommand = new SqlCommand(indexesQuery, connection);
                indexesCommand.Parameters.AddWithValue("@ViewName", vName);
                indexesCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                // Build the response in markdown format
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"# View Definition: {schemaName}.{vName}");
                sb.AppendLine();
                
                // Metadata section
                sb.AppendLine("## View Metadata");
                sb.AppendLine();
                
                using (var metadataReader = metadataCommand.ExecuteReader())
                {
                    if (metadataReader.Read())
                    {
                        DateTime createDate = metadataReader.GetDateTime(metadataReader.GetOrdinal("CreateDate"));
                        DateTime modifyDate = metadataReader.GetDateTime(metadataReader.GetOrdinal("ModifyDate"));
                        bool isSchemaBound = Convert.ToBoolean(metadataReader["IsSchemaBound"]);
                        string baseType = metadataReader["BaseType"].ToString() ?? "Unknown";
                        bool hasOpaqueMetadata = Convert.ToBoolean(metadataReader["HasOpaqueMetadata"]);
                        
                        sb.AppendLine($"- **Created**: {createDate}");
                        sb.AppendLine($"- **Last Modified**: {modifyDate}");
                        sb.AppendLine($"- **Schema Bound**: {(isSchemaBound ? "Yes" : "No")}");
                        sb.AppendLine($"- **Base Type**: {baseType}");
                        sb.AppendLine($"- **Has Opaque Metadata**: {(hasOpaqueMetadata ? "Yes" : "No")}");
                    }
                }
                
                // View Columns section
                sb.AppendLine();
                sb.AppendLine("## View Columns");
                sb.AppendLine();
                sb.AppendLine("| Column Name | Data Type | Max Length | Precision | Scale | Nullable | Computed |");
                sb.AppendLine("|-------------|-----------|------------|-----------|-------|----------|----------|");
                
                using (var columnReader = columnCommand.ExecuteReader())
                {
                    while (columnReader.Read())
                    {
                        string columnName = columnReader["ColumnName"].ToString() ?? "";
                        string dataType = columnReader["DataType"].ToString() ?? "";
                        short maxLength = columnReader.GetInt16(columnReader.GetOrdinal("MaxLength"));
                        byte precision = columnReader.GetByte(columnReader.GetOrdinal("Precision"));
                        byte scale = columnReader.GetByte(columnReader.GetOrdinal("Scale"));
                        bool isNullable = columnReader.GetBoolean(columnReader.GetOrdinal("IsNullable"));
                        string isComputed = columnReader["IsComputed"].ToString() ?? "No";
                        
                        // Format max length for character types
                        string maxLengthStr = maxLength.ToString();
                        if (dataType.Contains("char") || dataType.Contains("binary"))
                        {
                            maxLengthStr = maxLength == -1 ? "MAX" : maxLength.ToString();
                        }
                        
                        sb.AppendLine($"| {columnName} | {dataType} | {maxLengthStr} | {precision} | {scale} | {(isNullable ? "Yes" : "No")} | {isComputed} |");
                    }
                }
                
                // Indexes section
                sb.AppendLine();
                sb.AppendLine("## View Indexes");
                sb.AppendLine();
                
                using (var indexesReader = indexesCommand.ExecuteReader())
                {
                    if (indexesReader.HasRows)
                    {
                        sb.AppendLine("| Index Name | Type | Unique | Primary Key |");
                        sb.AppendLine("|------------|------|--------|-------------|");
                        
                        while (indexesReader.Read())
                        {
                            string indexName = indexesReader["IndexName"].ToString() ?? "";
                            string indexType = indexesReader["IndexType"].ToString() ?? "";
                            bool isUnique = indexesReader.GetBoolean(indexesReader.GetOrdinal("IsUnique"));
                            bool isPrimaryKey = indexesReader.GetBoolean(indexesReader.GetOrdinal("IsPrimaryKey"));
                            
                            sb.AppendLine($"| {indexName} | {indexType} | {(isUnique ? "Yes" : "No")} | {(isPrimaryKey ? "Yes" : "No")} |");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No indexes found for this view.");
                    }
                }
                
                // View Definition section
                sb.AppendLine();
                sb.AppendLine("## View Definition");
                sb.AppendLine();
                sb.AppendLine("```sql");
                sb.AppendLine(definition);
                sb.AppendLine("```");
                
                // Get dependencies
                sb.AppendLine();
                sb.AppendLine("## View Dependencies");
                sb.AppendLine();
                
                string dependencyQuery = @"
                    WITH view_dependencies AS (
                        SELECT 
                            OBJECT_SCHEMA_NAME(referencing_id) AS ReferencingSchema,
                            OBJECT_NAME(referencing_id) AS ReferencingEntity,
                            OBJECT_SCHEMA_NAME(referenced_id) AS ReferencedSchema,
                            OBJECT_NAME(referenced_id) AS ReferencedEntity,
                            o.type_desc AS ReferencedType
                        FROM 
                            sys.sql_expression_dependencies d
                        INNER JOIN 
                            sys.objects o ON d.referenced_id = o.object_id
                        WHERE 
                            referencing_id = OBJECT_ID(@SchemaName + '.' + @ViewName)
                    )
                    SELECT 
                        ReferencedSchema,
                        ReferencedEntity,
                        ReferencedType
                    FROM 
                        view_dependencies
                    ORDER BY 
                        ReferencedSchema, 
                        ReferencedEntity";
                
                using SqlCommand dependencyCommand = new SqlCommand(dependencyQuery, connection);
                dependencyCommand.Parameters.AddWithValue("@ViewName", vName);
                dependencyCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                using (var dependencyReader = dependencyCommand.ExecuteReader())
                {
                    if (dependencyReader.HasRows)
                    {
                        sb.AppendLine("This view references the following database objects:");
                        sb.AppendLine();
                        sb.AppendLine("| Schema | Object | Type |");
                        sb.AppendLine("|--------|--------|------|");
                        
                        while (dependencyReader.Read())
                        {
                            string referencedSchema = dependencyReader["ReferencedSchema"]?.ToString() ?? "";
                            string referencedEntity = dependencyReader["ReferencedEntity"]?.ToString() ?? "";
                            string referencedType = dependencyReader["ReferencedType"]?.ToString() ?? "";
                            
                            sb.AppendLine($"| {referencedSchema} | {referencedEntity} | {referencedType} |");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No direct dependencies found for this view.");
                    }
                }
                
                // Get referencing objects
                string referencingQuery = @"
                    WITH view_referencing AS (
                        SELECT 
                            OBJECT_SCHEMA_NAME(referencing_id) AS ReferencingSchema,
                            OBJECT_NAME(referencing_id) AS ReferencingEntity,
                            o.type_desc AS ReferencingType
                        FROM 
                            sys.sql_expression_dependencies d
                        INNER JOIN 
                            sys.objects o ON d.referencing_id = o.object_id
                        WHERE 
                            referenced_id = OBJECT_ID(@SchemaName + '.' + @ViewName)
                    )
                    SELECT 
                        ReferencingSchema,
                        ReferencingEntity,
                        ReferencingType
                    FROM 
                        view_referencing
                    ORDER BY 
                        ReferencingSchema, 
                        ReferencingEntity";
                
                using SqlCommand referencingCommand = new SqlCommand(referencingQuery, connection);
                referencingCommand.Parameters.AddWithValue("@ViewName", vName);
                referencingCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                sb.AppendLine();
                sb.AppendLine("## Objects Referencing This View");
                sb.AppendLine();
                
                using (var referencingReader = referencingCommand.ExecuteReader())
                {
                    if (referencingReader.HasRows)
                    {
                        sb.AppendLine("The following database objects reference this view:");
                        sb.AppendLine();
                        sb.AppendLine("| Schema | Object | Type |");
                        sb.AppendLine("|--------|--------|------|");
                        
                        while (referencingReader.Read())
                        {
                            string referencingSchema = referencingReader["ReferencingSchema"]?.ToString() ?? "";
                            string referencingEntity = referencingReader["ReferencingEntity"]?.ToString() ?? "";
                            string referencingType = referencingReader["ReferencingType"]?.ToString() ?? "";
                            
                            sb.AppendLine($"| {referencingSchema} | {referencingEntity} | {referencingType} |");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No objects found that reference this view.");
                    }
                }
                
                // Get view permissions
                string permissionsQuery = @"
                    SELECT 
                        dp.state_desc AS Permission,
                        dp.permission_name AS PermissionName,
                        CASE 
                            WHEN dp.grantee_principal_id = 0 THEN 'Public'
                            ELSE USER_NAME(dp.grantee_principal_id)
                        END AS GranteeName
                    FROM 
                        sys.database_permissions dp
                    JOIN 
                        sys.objects o ON dp.major_id = o.object_id
                    JOIN 
                        sys.schemas s ON o.schema_id = s.schema_id
                    WHERE 
                        o.name = @ViewName
                        AND s.name = @SchemaName
                        AND dp.class = 1 -- Object or column
                    ORDER BY 
                        GranteeName, 
                        Permission, 
                        PermissionName";
                
                using SqlCommand permissionsCommand = new SqlCommand(permissionsQuery, connection);
                permissionsCommand.Parameters.AddWithValue("@ViewName", vName);
                permissionsCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                
                sb.AppendLine();
                sb.AppendLine("## View Permissions");
                sb.AppendLine();
                
                using (var permissionsReader = permissionsCommand.ExecuteReader())
                {
                    if (permissionsReader.HasRows)
                    {
                        sb.AppendLine("| State | Permission | Grantee |");
                        sb.AppendLine("|-------|------------|---------|");
                        
                        while (permissionsReader.Read())
                        {
                            string permission = permissionsReader["Permission"]?.ToString() ?? "";
                            string permissionName = permissionsReader["PermissionName"]?.ToString() ?? "";
                            string granteeName = permissionsReader["GranteeName"]?.ToString() ?? "";
                            
                            sb.AppendLine($"| {permission} | {permissionName} | {granteeName} |");
                        }
                    }
                    else
                    {
                        sb.AppendLine("No specific permissions found for this view.");
                    }
                }
                
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error retrieving view definition: {ex.Message}\n\n{ex.StackTrace}";
            }
        }
    }
}
