using Core.Infrastructure.SqlClient.Utilities;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace UnitTests.Infrastructure.SqlClient.Utilities
{
    /// <summary>
    /// Unit tests for ParameterNormalizer utility class.
    /// </summary>
    public class ParameterNormalizerTests
    {
        [Fact(DisplayName = "PN-001: NormalizeParameterName removes @ prefix")]
        public void PN001()
        {
            // Arrange
            var parameterName = "@CustomerID";
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterName(parameterName);
            
            // Assert
            result.Should().Be("CustomerID");
        }
        
        [Fact(DisplayName = "PN-002: NormalizeParameterName keeps name without @ prefix")]
        public void PN002()
        {
            // Arrange
            var parameterName = "CustomerID";
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterName(parameterName);
            
            // Assert
            result.Should().Be("CustomerID");
        }
        
        [Fact(DisplayName = "PN-003: NormalizeParameterName handles null")]
        public void PN003()
        {
            // Arrange
            string? parameterName = null;
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterName(parameterName!);
            
            // Assert
            result.Should().BeNull();
        }
        
        [Fact(DisplayName = "PN-004: NormalizeParameterName handles empty string")]
        public void PN004()
        {
            // Arrange
            var parameterName = "";
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterName(parameterName);
            
            // Assert
            result.Should().Be("");
        }
        
        [Fact(DisplayName = "PN-005: NormalizeParameterName handles whitespace")]
        public void PN005()
        {
            // Arrange
            var parameterName = "   ";
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterName(parameterName);
            
            // Assert
            result.Should().Be("   ");
        }
        
        [Fact(DisplayName = "PN-006: EnsureParameterPrefix adds @ prefix")]
        public void PN006()
        {
            // Arrange
            var parameterName = "CustomerID";
            
            // Act
            var result = ParameterNormalizer.EnsureParameterPrefix(parameterName);
            
            // Assert
            result.Should().Be("@CustomerID");
        }
        
        [Fact(DisplayName = "PN-007: EnsureParameterPrefix keeps existing @ prefix")]
        public void PN007()
        {
            // Arrange
            var parameterName = "@CustomerID";
            
            // Act
            var result = ParameterNormalizer.EnsureParameterPrefix(parameterName);
            
            // Assert
            result.Should().Be("@CustomerID");
        }
        
        [Fact(DisplayName = "PN-008: NormalizeParameterDictionary creates case-insensitive dictionary")]
        public void PN008()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "@CustomerID", 123 },
                { "OrderDate", "2024-01-01" },
                { "@Amount", 99.99 }
            };
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterDictionary(parameters);
            
            // Assert
            result.Should().HaveCount(3);
            result.Should().ContainKey("CustomerID");
            result.Should().ContainKey("OrderDate");
            result.Should().ContainKey("Amount");
            result["CustomerID"].Should().Be(123);
            result["OrderDate"].Should().Be("2024-01-01");
            result["Amount"].Should().Be(99.99);
        }
        
        [Fact(DisplayName = "PN-009: NormalizeParameterDictionary handles null input")]
        public void PN009()
        {
            // Arrange
            Dictionary<string, object?>? parameters = null;
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterDictionary(parameters!);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }
        
        [Fact(DisplayName = "PN-010: NormalizeParameterDictionary is case-insensitive")]
        public void PN010()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "customerid", 123 }
            };
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterDictionary(parameters);
            
            // Assert
            result.Should().ContainKey("CUSTOMERID");
            result.Should().ContainKey("customerid");
            result.Should().ContainKey("CustomerID");
        }
        
        [Fact(DisplayName = "PN-011: AreParameterNamesEqual compares ignoring @ and case")]
        public void PN011()
        {
            // Arrange & Act & Assert
            ParameterNormalizer.AreParameterNamesEqual("@CustomerID", "customerid").Should().BeTrue();
            ParameterNormalizer.AreParameterNamesEqual("CustomerID", "@CUSTOMERID").Should().BeTrue();
            ParameterNormalizer.AreParameterNamesEqual("@CustomerID", "@CustomerID").Should().BeTrue();
            ParameterNormalizer.AreParameterNamesEqual("CustomerID", "CustomerID").Should().BeTrue();
        }
        
        [Fact(DisplayName = "PN-012: AreParameterNamesEqual returns false for different names")]
        public void PN012()
        {
            // Arrange & Act & Assert
            ParameterNormalizer.AreParameterNamesEqual("@CustomerID", "OrderDate").Should().BeFalse();
            ParameterNormalizer.AreParameterNamesEqual("CustomerID", "Amount").Should().BeFalse();
        }
        
        [Fact(DisplayName = "PN-013: AreParameterNamesEqual handles null and empty")]
        public void PN013()
        {
            // Arrange & Act & Assert
            ParameterNormalizer.AreParameterNamesEqual(null!, null!).Should().BeTrue();
            ParameterNormalizer.AreParameterNamesEqual("", "").Should().BeTrue();
            ParameterNormalizer.AreParameterNamesEqual("   ", "   ").Should().BeTrue();
            ParameterNormalizer.AreParameterNamesEqual(null!, "test").Should().BeFalse();
            ParameterNormalizer.AreParameterNamesEqual("test", null!).Should().BeFalse();
            ParameterNormalizer.AreParameterNamesEqual("", "test").Should().BeFalse();
        }
        
        [Fact(DisplayName = "PN-014: TryGetParameterValue finds parameter ignoring case and @")]
        public void PN014()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "@CustomerID", 123 },
                { "orderdate", "2024-01-01" }
            };
            
            // Act & Assert
            ParameterNormalizer.TryGetParameterValue(parameters, "@customerid", out var value1).Should().BeTrue();
            value1.Should().Be(123);
            
            ParameterNormalizer.TryGetParameterValue(parameters, "ORDERDATE", out var value2).Should().BeTrue();
            value2.Should().Be("2024-01-01");
            
            ParameterNormalizer.TryGetParameterValue(parameters, "NonExistent", out var value3).Should().BeFalse();
            value3.Should().BeNull();
        }
        
        [Fact(DisplayName = "PN-015: TryGetParameterValue handles null parameters")]
        public void PN015()
        {
            // Arrange
            Dictionary<string, object?>? parameters = null;
            
            // Act
            var result = ParameterNormalizer.TryGetParameterValue(parameters!, "test", out var value);
            
            // Assert
            result.Should().BeFalse();
            value.Should().BeNull();
        }
        
        [Fact(DisplayName = "PN-016: TryGetParameterValue handles null parameter name")]
        public void PN016()
        {
            // Arrange
            var parameters = new Dictionary<string, object?> { { "test", 123 } };
            
            // Act
            var result = ParameterNormalizer.TryGetParameterValue(parameters, null!, out var value);
            
            // Assert
            result.Should().BeFalse();
            value.Should().BeNull();
        }
        
        [Fact(DisplayName = "PN-017: NormalizeParameterDictionary handles duplicate keys")]
        public void PN017()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "CustomerID", 123 },
                { "@CustomerID", 456 } // This should overwrite the first one after normalization
            };
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterDictionary(parameters);
            
            // Assert
            result.Should().HaveCount(1);
            result["CustomerID"].Should().Be(456); // Last one wins
        }
        
        [Fact(DisplayName = "PN-018: NormalizeParameterDictionary preserves null values")]
        public void PN018()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "@CustomerID", null },
                { "Notes", null }
            };
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterDictionary(parameters);
            
            // Assert
            result.Should().HaveCount(2);
            result["CustomerID"].Should().BeNull();
            result["Notes"].Should().BeNull();
        }
        
        [Fact(DisplayName = "PN-019: Multiple @ symbols are handled correctly")]
        public void PN019()
        {
            // Arrange
            var parameterName = "@@weird@@name";
            
            // Act
            var normalized = ParameterNormalizer.NormalizeParameterName(parameterName);
            var withPrefix = ParameterNormalizer.EnsureParameterPrefix(normalized);
            
            // Assert
            normalized.Should().Be("@weird@@name"); // Only first @ is removed
            withPrefix.Should().Be("@weird@@name"); // @ is not added again since it already starts with @
        }
        
        [Fact(DisplayName = "PN-020: Empty dictionary handling")]
        public void PN020()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();
            
            // Act
            var result = ParameterNormalizer.NormalizeParameterDictionary(parameters);
            
            // Assert
            result.Should().BeEmpty();
            result.Should().NotBeNull();
        }
    }
}