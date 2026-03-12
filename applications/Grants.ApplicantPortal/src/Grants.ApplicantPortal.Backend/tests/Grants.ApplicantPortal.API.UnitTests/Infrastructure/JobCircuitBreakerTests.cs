using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class JobCircuitBreakerTests
{
    private readonly JobCircuitBreaker _sut;

    public JobCircuitBreakerTests()
    {
        var options = Options.Create(new MessagingOptions
        {
            BackgroundJobs = new BackgroundJobOptions
            {
                BaseBackoffSeconds = 15,
                MaxBackoffSeconds = 300,
                BackoffMultiplier = 2.0,
                LogEveryNthFailure = 20
            }
        });

        _sut = new JobCircuitBreaker(options, NullLogger<JobCircuitBreaker>.Instance);
    }

    [Fact]
    public void ShouldExecute_ReturnsTrue_WhenNoFailures()
    {
        _sut.ShouldExecute("test-job").Should().BeTrue();
    }

    [Fact]
    public void ShouldExecute_ReturnsFalse_AfterFailure_WithinBackoffWindow()
    {
        _sut.RecordFailure("test-job", new Exception("db down"));

        // Immediately after failure, should be within backoff window
        _sut.ShouldExecute("test-job").Should().BeFalse();
    }

    [Fact]
    public void RecordSuccess_ResetsState_SoJobCanExecuteAgain()
    {
        _sut.RecordFailure("test-job", new Exception("db down"));
        _sut.ShouldExecute("test-job").Should().BeFalse();

        _sut.RecordSuccess("test-job");
        _sut.ShouldExecute("test-job").Should().BeTrue();
    }

    [Fact]
    public void DifferentJobKeys_AreTrackedIndependently()
    {
        _sut.RecordFailure("job-a", new Exception("fail"));

        _sut.ShouldExecute("job-a").Should().BeFalse();
        _sut.ShouldExecute("job-b").Should().BeTrue();
    }

    [Fact]
    public void BackoffCapsAtMaxBackoffSeconds()
    {
        // Drive failures high enough to exceed max
        // 15 * 2^9 = 7680 which exceeds 300, so it should cap
        for (int i = 0; i < 10; i++)
        {
            _sut.RecordFailure("test-job", new Exception("fail"));
        }

        // After many failures, should still be blocked (not overflowed or broken)
        _sut.ShouldExecute("test-job").Should().BeFalse();

        // Reset still works
        _sut.RecordSuccess("test-job");
        _sut.ShouldExecute("test-job").Should().BeTrue();
    }

    [Fact]
    public void MultipleFailuresThenRecovery_FullCycle()
    {
        // Healthy
        _sut.ShouldExecute("test-job").Should().BeTrue();

        // Fail 3 times
        _sut.RecordFailure("test-job", new Exception("fail 1"));
        _sut.RecordFailure("test-job", new Exception("fail 2"));
        _sut.RecordFailure("test-job", new Exception("fail 3"));

        // Should be blocked
        _sut.ShouldExecute("test-job").Should().BeFalse();

        // Recover
        _sut.RecordSuccess("test-job");

        // Should be open again
        _sut.ShouldExecute("test-job").Should().BeTrue();

        // Fail again — should block again
        _sut.RecordFailure("test-job", new Exception("fail 4"));
        _sut.ShouldExecute("test-job").Should().BeFalse();
    }
}
