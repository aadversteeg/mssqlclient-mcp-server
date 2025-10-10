using Core.Application.Models;
using FluentAssertions;
using System;
using System.Threading;
using Xunit;

namespace UnitTests.Application
{
    public class ToolCallTimeoutContextTests
    {
        [Fact(DisplayName = "TCTC-001: ToolCallTimeoutContext calculates remaining time correctly")]
        public void TCTC001()
        {
            // Arrange
            var totalTimeoutSeconds = 60;
            using var tokenSource = new CancellationTokenSource();
            var context = new ToolCallTimeoutContext(totalTimeoutSeconds, tokenSource.Token);
            
            // Act - let some time pass
            Thread.Sleep(100); // 0.1 seconds
            var remainingTime = context.RemainingTime;
            
            // Assert
            remainingTime.Should().BeLessThan(TimeSpan.FromSeconds(60));
            remainingTime.Should().BeGreaterThan(TimeSpan.FromSeconds(59));
            context.IsTimeoutExceeded.Should().BeFalse();
        }
        
        [Fact(DisplayName = "TCTC-002: GetEffectiveCommandTimeout returns minimum of default and remaining time")]
        public void TCTC002()
        {
            // Arrange
            var totalTimeoutSeconds = 10;
            using var tokenSource = new CancellationTokenSource();
            var context = new ToolCallTimeoutContext(totalTimeoutSeconds, tokenSource.Token);
            
            // Act
            var effectiveTimeout1 = context.GetEffectiveCommandTimeout(30); // Should return 10 (remaining)
            Thread.Sleep(2000); // Wait 2 seconds
            var effectiveTimeout2 = context.GetEffectiveCommandTimeout(30); // Should return ~8 (remaining)
            var effectiveTimeout3 = context.GetEffectiveCommandTimeout(5);  // Should return 5 (default is smaller)
            
            // Assert
            effectiveTimeout1.Should().Be(10);
            effectiveTimeout2.Should().BeInRange(7, 9); // Allow some variance for timing
            effectiveTimeout3.Should().Be(5);
        }
        
        [Fact(DisplayName = "TCTC-003: GetEffectiveCommandTimeout ensures at least 1 second")]
        public void TCTC003()
        {
            // Arrange
            var totalTimeoutSeconds = 1;
            using var tokenSource = new CancellationTokenSource();
            var context = new ToolCallTimeoutContext(totalTimeoutSeconds, tokenSource.Token);
            
            // Act - wait for timeout to be exceeded
            Thread.Sleep(1500); // Wait 1.5 seconds
            var effectiveTimeout = context.GetEffectiveCommandTimeout(30);
            
            // Assert
            effectiveTimeout.Should().Be(1); // Should be at least 1 second
            context.IsTimeoutExceeded.Should().BeTrue();
        }
        
        [Fact(DisplayName = "TCTC-004: CreateTimeoutExceededMessage returns formatted error")]
        public void TCTC004()
        {
            // Arrange
            var totalTimeoutSeconds = 5;
            using var tokenSource = new CancellationTokenSource();
            var context = new ToolCallTimeoutContext(totalTimeoutSeconds, tokenSource.Token);
            
            // Act
            Thread.Sleep(100); // Let some time pass
            var message = context.CreateTimeoutExceededMessage();
            
            // Assert
            message.Should().Contain("Total tool timeout of 5s exceeded after");
            message.Should().Contain("s");
        }
    }
    
    public class ToolCallTimeoutFactoryTests
    {
        [Fact(DisplayName = "TCTF-001: CreateTimeout returns null when TotalToolCallTimeoutSeconds is null")]
        public void TCTF001()
        {
            // Arrange
            var config = new DatabaseConfiguration
            {
                TotalToolCallTimeoutSeconds = null
            };
            
            // Act
            var (context, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(config);
            
            // Assert
            context.Should().BeNull();
            tokenSource.Should().BeNull();
        }
        
        [Fact(DisplayName = "TCTF-002: CreateTimeout creates context and token source when timeout is configured")]
        public void TCTF002()
        {
            // Arrange
            var config = new DatabaseConfiguration
            {
                TotalToolCallTimeoutSeconds = 30
            };
            
            // Act
            var (context, tokenSource) = ToolCallTimeoutFactory.CreateTimeout(config);
            
            // Assert
            context.Should().NotBeNull();
            tokenSource.Should().NotBeNull();
            context!.TotalTimeoutSeconds.Should().Be(30);
            
            // Cleanup
            tokenSource!.Dispose();
        }
        
        [Fact(DisplayName = "TCTF-003: CombineTokens returns existing token when no timeout context")]
        public void TCTF003()
        {
            // Arrange
            using var existingTokenSource = new CancellationTokenSource();
            var existingToken = existingTokenSource.Token;
            
            // Act
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(null, existingToken);
            
            // Assert
            combinedToken.Should().Be(existingToken);
        }
        
        [Fact(DisplayName = "TCTF-004: CombineTokens returns timeout token when no existing token")]
        public void TCTF004()
        {
            // Arrange
            using var tokenSource = new CancellationTokenSource();
            var context = new ToolCallTimeoutContext(30, tokenSource.Token);
            
            // Act
            var combinedToken = ToolCallTimeoutFactory.CombineTokens(context);
            
            // Assert
            combinedToken.Should().Be(context.CancellationToken);
        }
    }
}