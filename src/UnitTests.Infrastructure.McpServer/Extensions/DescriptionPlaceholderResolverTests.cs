using Core.Application.Models;
using Core.Infrastructure.McpServer.Extensions;
using FluentAssertions;
using Xunit;

namespace UnitTests.Infrastructure.McpServer.Extensions
{
    public class DescriptionPlaceholderResolverTests
    {
        [Fact(DisplayName = "DPR-001: Known placeholder is replaced with config value")]
        public void DPR001()
        {
            // Arrange
            var config = new DatabaseConfiguration { MaxCellOutputLength = 100 };

            // Act
            var result = DescriptionPlaceholderResolver.Resolve("Cell output limited to {MaxCellOutputLength} characters.", config);

            // Assert
            result.Should().Be("Cell output limited to 100 characters.");
        }

        [Fact(DisplayName = "DPR-002: Unknown placeholder is left unchanged")]
        public void DPR002()
        {
            // Arrange
            var config = new DatabaseConfiguration();

            // Act
            var result = DescriptionPlaceholderResolver.Resolve("Value: {UnknownProperty}", config);

            // Assert
            result.Should().Be("Value: {UnknownProperty}");
        }

        [Fact(DisplayName = "DPR-003: Multiple placeholders in one string are all replaced")]
        public void DPR003()
        {
            // Arrange
            var config = new DatabaseConfiguration
            {
                MaxCellOutputLength = 50,
                DefaultCommandTimeoutSeconds = 30
            };

            // Act
            var result = DescriptionPlaceholderResolver.Resolve(
                "Cell limit: {MaxCellOutputLength}, timeout: {DefaultCommandTimeoutSeconds}s.", config);

            // Assert
            result.Should().Be("Cell limit: 50, timeout: 30s.");
        }

        [Fact(DisplayName = "DPR-004: String with no placeholders is returned unchanged")]
        public void DPR004()
        {
            // Arrange
            var config = new DatabaseConfiguration();
            const string template = "No placeholders here.";

            // Act
            var result = DescriptionPlaceholderResolver.Resolve(template, config);

            // Assert
            result.Should().Be(template);
        }

        [Fact(DisplayName = "DPR-005: Placeholder with value of zero is resolved correctly")]
        public void DPR005()
        {
            // Arrange
            var config = new DatabaseConfiguration { MaxCellOutputLength = 0 };

            // Act
            var result = DescriptionPlaceholderResolver.Resolve("Limit: {MaxCellOutputLength} (0 = no limit).", config);

            // Assert
            result.Should().Be("Limit: 0 (0 = no limit).");
        }

        [Fact(DisplayName = "DPR-006: Braces with non-word characters are not treated as placeholders")]
        public void DPR006()
        {
            // Arrange
            var config = new DatabaseConfiguration();

            // Act
            var result = DescriptionPlaceholderResolver.Resolve(@"Example: {""Key"": 123}", config);

            // Assert
            result.Should().Be(@"Example: {""Key"": 123}");
        }
    }
}
