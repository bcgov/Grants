using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.UseCases;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class MessagingOptionsTests
{
    [Fact]
    public void MessagingOptions_HasCorrectSectionName()
    {
        MessagingOptions.SectionName.Should().Be("Messaging");
    }

    [Fact]
    public void MessagingOptions_DefaultsAreReasonable()
    {
        var options = new MessagingOptions();

        options.RabbitMQ.Should().NotBeNull();
        options.Outbox.Should().NotBeNull();
        options.Inbox.Should().NotBeNull();
        options.DistributedLocks.Should().NotBeNull();
        options.BackgroundJobs.Should().NotBeNull();
    }

    [Fact]
    public void RabbitMQOptions_HasSensibleDefaults()
    {
        var options = new RabbitMQOptions();

        options.HostName.Should().Be("localhost");
        options.Port.Should().Be(5672);
        options.UserName.Should().Be("guest");
        options.Password.Should().Be("guest");
        options.VirtualHost.Should().Be("/");
        options.RetryCount.Should().BeGreaterThan(0);
        options.RetryDelay.Should().BeGreaterThan(TimeSpan.Zero);
        options.ConnectionTimeout.Should().BeGreaterThan(TimeSpan.Zero);
        options.UseSsl.Should().BeFalse();
    }

    [Fact]
    public void OutboxOptions_HasSensibleDefaults()
    {
        var options = new OutboxOptions();

        options.PollingIntervalSeconds.Should().BeGreaterThan(0);
        options.BatchSize.Should().BeGreaterThan(0);
        options.MaxRetries.Should().BeGreaterThan(0);
        options.RetentionDays.Should().BeGreaterThan(0);
        options.CleanupIntervalHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public void InboxOptions_HasSensibleDefaults()
    {
        var options = new InboxOptions();

        options.PollingIntervalSeconds.Should().BeGreaterThan(0);
        options.BatchSize.Should().BeGreaterThan(0);
        options.MaxRetries.Should().BeGreaterThan(0);
        options.RetentionDays.Should().BeGreaterThan(0);
        options.CleanupIntervalHours.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DistributedLockOptions_HasSensibleDefaults()
    {
        var options = new DistributedLockOptions();

        options.DefaultTimeoutMinutes.Should().BeGreaterThan(0);
        options.RenewalIntervalMinutes.Should().BeGreaterThan(0);
        options.WaitTimeoutSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BackgroundJobOptions_HasSensibleDefaults()
    {
        var options = new BackgroundJobOptions();

        options.MaxConcurrency.Should().BeGreaterThan(0);
        options.Enabled.Should().BeTrue();
        options.MisfireThresholdSeconds.Should().BeGreaterThan(0);
        options.BaseBackoffSeconds.Should().BeGreaterThan(0);
        options.MaxBackoffSeconds.Should().BeGreaterOrEqualTo(options.BaseBackoffSeconds);
        options.BackoffMultiplier.Should().BeGreaterThan(1.0);
        options.LogEveryNthFailure.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ProfileCacheOptions_HasCorrectSectionName()
    {
        ProfileCacheOptions.SectionName.Should().Be("ProfileCache");
    }

    [Fact]
    public void ProfileCacheOptions_HasSensibleDefaults()
    {
        var options = new ProfileCacheOptions();

        options.CacheKeyPrefix.Should().NotBeNullOrWhiteSpace();
        options.CacheExpiryMinutes.Should().BeGreaterThan(0);
        options.SlidingExpiryMinutes.Should().BeGreaterThan(0);
        options.SlidingExpiryMinutes.Should().BeLessThan(options.CacheExpiryMinutes);
    }
}
