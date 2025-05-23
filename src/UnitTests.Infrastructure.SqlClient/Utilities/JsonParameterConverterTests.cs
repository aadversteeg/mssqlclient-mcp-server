using Core.Infrastructure.SqlClient.Utilities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient.Utilities
{
    public class JsonParameterConverterTests
    {
        [Fact(DisplayName = "JPC-001: ConvertParameters handles empty dictionary")]
        public void JPC001()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact(DisplayName = "JPC-002: ConvertParameters handles null input")]
        public void JPC002()
        {
            // Arrange
            Dictionary<string, object?> parameters = null;

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact(DisplayName = "JPC-003: ConvertParameters handles string values")]
        public void JPC003()
        {
            // Arrange
            var json = "{\"Name\": \"InkoopOrder\"}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("Name");
            result["Name"].Should().BeOfType<string>();
            result["Name"].Should().Be("InkoopOrder");
        }

        [Fact(DisplayName = "JPC-004: ConvertParameters handles integer values")]
        public void JPC004()
        {
            // Arrange
            var json = "{\"Id\": 42}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("Id");
            result["Id"].Should().BeOfType<int>();
            result["Id"].Should().Be(42);
        }

        [Fact(DisplayName = "JPC-005: ConvertParameters handles boolean values")]
        public void JPC005()
        {
            // Arrange
            var json = "{\"IsActive\": true}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("IsActive");
            result["IsActive"].Should().BeOfType<bool>();
            result["IsActive"].Should().Be(true);
        }

        [Fact(DisplayName = "JPC-006: ConvertParameters handles decimal values")]
        public void JPC006()
        {
            // Arrange
            var json = "{\"Price\": 42.99}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("Price");
            // Note: Numeric types can be converted to various .NET types depending on value
            result["Price"].Should().BeAssignableTo<double>();
            Convert.ToDecimal(result["Price"]).Should().Be(42.99m);
        }

        [Fact(DisplayName = "JPC-007: ConvertParameters handles null values")]
        public void JPC007()
        {
            // Arrange
            var json = "{\"NullValue\": null}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("NullValue");
            result["NullValue"].Should().BeNull();
        }

        [Fact(DisplayName = "JPC-008: ConvertParameters handles array values")]
        public void JPC008()
        {
            // Arrange
            var json = "{\"Items\": [1, 2, 3]}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("Items");
            result["Items"].Should().BeAssignableTo<object[]>();
            var array = result["Items"] as object[];
            array.Should().NotBeNull();
            array.Length.Should().Be(3);
            array[0].Should().BeOfType<int>();
            array[0].Should().Be(1);
        }

        [Fact(DisplayName = "JPC-009: ConvertParameters handles nested object values")]
        public void JPC009()
        {
            // Arrange
            var json = "{\"Address\": {\"Street\": \"Main St\", \"Number\": 123}}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("Address");
            result["Address"].Should().BeOfType<string>(); // Nested objects become JSON strings
            (result["Address"] as string).Should().Contain("Main St");
            (result["Address"] as string).Should().Contain("123");
        }

        [Fact(DisplayName = "JPC-010: ConvertParameters preserves non-JsonElement values")]
        public void JPC010()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                ["Name"] = "DirectString",
                ["Id"] = 100,
                ["IsActive"] = true
            };

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("Name");
            result["Name"].Should().BeOfType<string>();
            result["Name"].Should().Be("DirectString");
            
            result.Should().ContainKey("Id");
            result["Id"].Should().Be(100);
            
            result.Should().ContainKey("IsActive");
            result["IsActive"].Should().Be(true);
        }

        [Fact(DisplayName = "JPC-011: ConvertParameters handles mixed types")]
        public void JPC011()
        {
            // Arrange
            var jsonPart = "{\"JsonString\": \"FromJson\", \"JsonNumber\": 42}";
            var jsonParameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(jsonPart);
            
            var parameters = new Dictionary<string, object?>
            {
                ["DirectString"] = "DirectValue",
                ["DirectNumber"] = 100
            };
            
            // Add JSON parameters to the direct parameters
            foreach (var param in jsonParameters)
            {
                parameters.Add(param.Key, param.Value);
            }

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("DirectString");
            result["DirectString"].Should().BeOfType<string>();
            result["DirectString"].Should().Be("DirectValue");
            
            result.Should().ContainKey("DirectNumber");
            result["DirectNumber"].Should().Be(100);
            
            result.Should().ContainKey("JsonString");
            result["JsonString"].Should().BeOfType<string>();
            result["JsonString"].Should().Be("FromJson");
            
            result.Should().ContainKey("JsonNumber");
            result["JsonNumber"].Should().BeOfType<int>();
            result["JsonNumber"].Should().Be(42);
        }

        [Fact(DisplayName = "JPC-012: ConvertParameters is case insensitive for parameter names")]
        public void JPC012()
        {
            // Arrange
            var json = "{\"NAME\": \"Value\"}";
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

            // Act
            var result = JsonParameterConverter.ConvertParameters(parameters);

            // Assert
            result.Should().ContainKey("NAME");
            // Should be able to access using different case
            result.Should().ContainKey("name");
            result["name"].Should().Be("Value");
        }
    }
}