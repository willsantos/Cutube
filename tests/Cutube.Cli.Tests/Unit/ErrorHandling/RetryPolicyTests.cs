using FluentAssertions;
using Xunit;
using Cutube.Cli.ErrorHandling;
using System.Net;

namespace Cutube.Tests.Unit.ErrorHandling;

public class RetryPolicyTests
{
    [Fact]
    public async Task ExecuteAsync_FirstAttemptSuccess_ReturnsImmediately()
    {
        var policy = new RetryPolicy();
        var callCount = 0;

        var result = await policy.ExecuteAsync(async () =>
        {
            callCount++;
            return await Task.FromResult("success");
        });

        result.Should().Be("success");
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_NetworkError_RetriesThreeTimes()
    {
        var policy = new RetryPolicy(maxRetries: 3);
        var attemptCount = 0;

        var result = await policy.ExecuteAsync<string>(async () =>
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                throw new HttpRequestException("Network error");
            }
            return await Task.FromResult("success after retries");
        });

        result.Should().Be("success after retries");
        attemptCount.Should().Be(4);
    }

    [Fact]
    public async Task ExecuteAsync_ExceedsMaxRetries_ThrowsLastException()
    {
        var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));

        var act = () => policy.ExecuteAsync<string>(async () =>
        {
            await Task.Delay(10);
            throw new HttpRequestException("Persistent network error");
        });

        // ExecuteAsync retries 2 times then throws the original HttpRequestException
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ExecuteAsync_NonRetryableException_FailsImmediately()
    {
        var policy = new RetryPolicy();
        var attemptCount = 0;

        var act = () => policy.ExecuteAsync<string>(async () =>
        {
            attemptCount++;
            throw new ArgumentException("Invalid argument");
        });

        var exception = await act.Should().ThrowAsync<ArgumentException>();
        attemptCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutException_Retries()
    {
        var policy = new RetryPolicy(maxRetries: 2, initialDelay: TimeSpan.FromMilliseconds(10));
        var attemptCount = 0;

        var act = async () => await policy.ExecuteAsync<string>(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new TimeoutException("Request timed out");
            }
            return await Task.FromResult("success");
        });

        var result = await act();
        result.Should().Be("success");
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_IOException_Retries()
    {
        var policy = new RetryPolicy(maxRetries: 2);
        var attemptCount = 0;

        var act = async () => await policy.ExecuteAsync<string>(async () =>
        {
            attemptCount++;
            if (attemptCount < 2)
            {
                throw new IOException("File locked");
            }
            return await Task.FromResult("success");
        });

        var result = await act();
        result.Should().Be("success");
        attemptCount.Should().Be(2);
    }

    [Fact]
    public void RetryPolicy_CustomPredicate_UsesCustomLogic()
    {
        var customPolicy = new RetryPolicy(
            shouldRetryPredicate: ex => ex is InvalidOperationException
        );

        customPolicy.ShouldRetryPredicate(new InvalidOperationException()).Should().BeTrue();
        customPolicy.ShouldRetryPredicate(new HttpRequestException()).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_CustomPredicate_OnlyRetriesMatchingExceptions()
    {
        var policy = new RetryPolicy(
            maxRetries: 3,
            shouldRetryPredicate: ex => ex is InvalidOperationException
        );
        var attemptCount = 0;

        var act = () => policy.ExecuteAsync<string>(async () =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new InvalidOperationException("Retry this");
            }
            return await Task.FromResult("success");
        });

        var result = await act();
        result.Should().Be("success");
        attemptCount.Should().Be(3);
    }

    [Fact]
    public async Task ExecuteAsync_ExponentialBackoff_WaitsLongerEachRetry()
    {
        var delays = new List<long>();
        var policy = new RetryPolicy(
            maxRetries: 3,
            initialDelay: TimeSpan.FromMilliseconds(50),
            maxDelay: TimeSpan.FromMilliseconds(200)
        );

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await policy.ExecuteAsync<string>(async () =>
            {
                delays.Add(sw.ElapsedMilliseconds);
                await Task.Delay(10);
                throw new HttpRequestException("Network error");
            });
        }
        catch
        {
            // Expected after retries exhausted
        }

        // Should have 4 attempts (1 initial + 3 retries)
        delays.Should().HaveCount(4);
        // Delays between attempts should increase (exponential backoff)
        var timeBetweenAttempts = new List<long>();
        for (int i = 1; i < delays.Count; i++)
        {
            timeBetweenAttempts.Add(delays[i] - delays[i-1]);
        }
        // Second retry after longer delay (exponential backoff)
        timeBetweenAttempts[1].Should().BeGreaterThan(timeBetweenAttempts[0]);
    }
}
