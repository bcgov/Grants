using Ardalis.Result;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Unity;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Grants.ApplicantPortal.API.UnitTests.Plugins;

/// <summary>
/// Tests for the UNITY plugin's SUBMISSIONFORM handling (Unity.SubmissionForm.cs).
/// Verifies the cache segment/query shape used to call the (currently unreachable in
/// dev) Unity endpoint, and that a failed/unreachable call is surfaced as a
/// catchable exception — never an unhandled crash — so that
/// <see cref="ProfileDataRetrievalService"/> can convert it into an Ardalis
/// <c>Result.Error</c> instead of a raw exception reaching the endpoint.
/// </summary>
public class UnityPluginSubmissionFormTests
{
    private readonly IExternalServiceClient _externalServiceClient;
    private readonly IDistributedCache _cache;
    private readonly IPluginCacheService _pluginCacheService;
    private readonly UnityPlugin _sut;

    public UnityPluginSubmissionFormTests()
    {
        _externalServiceClient = Substitute.For<IExternalServiceClient>();

        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheOptions = Options.Create(new ProfileCacheOptions
        {
            CacheKeyPrefix = "profile:",
            CacheExpiryMinutes = 60,
            SlidingExpiryMinutes = 15
        });
        _pluginCacheService = new PluginCacheService(_cache, cacheOptions, NullLogger<PluginCacheService>.Instance);

        _sut = new UnityPlugin(NullLogger<UnityPlugin>.Instance, _externalServiceClient, _pluginCacheService);
    }

    private static ProfilePopulationMetadata BuildMetadata(Guid profileId, string provider, string submissionId) =>
        new(profileId, "UNITY", provider, "SUBMISSIONFORM", "test-subject",
            new Dictionary<string, object> { ["SubmissionId"] = submissionId });

    [Fact]
    public async Task PopulateProfileAsync_SubmissionForm_UsesExpectedCacheSegmentAndQueryParameters()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid().ToString();
        var metadata = BuildMetadata(profileId, "PROV1", submissionId);

        _externalServiceClient
            .CallAsync("UNITY", Arg.Any<ExternalServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalServiceResponse<string>
            {
                IsSuccess = true,
                Data = """{"data":{"schema":{},"data":{}}}""",
                StatusCode = 200
            });

        await _sut.PopulateProfileAsync(metadata);

        // Cache key follows the {Provider}:SUBMISSIONFORM:{SubmissionId} segment convention
        var expectedCacheKey = _pluginCacheService.BuildCacheKey(profileId, "UNITY", $"PROV1:SUBMISSIONFORM:{submissionId}");
        (await _cache.GetStringAsync(expectedCacheKey)).Should().NotBeNull();

        await _externalServiceClient.Received(1).CallAsync(
            "UNITY",
            Arg.Is<ExternalServiceRequest>(r =>
                r.Endpoint == "/api/app/applicant-profiles/profile" &&
                r.QueryParameters != null &&
                r.QueryParameters["TenantId"] == "PROV1" &&
                r.QueryParameters["Key"] == "SUBMISSIONFORMDATA" &&
                r.QueryParameters["ProfileId"] == profileId.ToString() &&
                r.QueryParameters["Subject"] == "test-subject" &&
                r.QueryParameters["SubmissionId"] == submissionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PopulateProfileAsync_SubmissionForm_UnreachableUnity_ThrowsControlledException_NotRawFailure()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid().ToString();
        var metadata = BuildMetadata(profileId, "PROV1", submissionId);

        // Simulate the Unity endpoint being unreachable in dev (connection refused / timeout)
        _externalServiceClient
            .CallAsync("UNITY", Arg.Any<ExternalServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalServiceResponse<string>
            {
                IsSuccess = false,
                ErrorMessage = "Request timeout",
                StatusCode = 408
            });

        var act = () => _sut.PopulateProfileAsync(metadata);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Request timeout*");
    }

    [Fact]
    public async Task RetrieveProfileDataAsync_SubmissionForm_UnreachableUnity_ReturnsResultError_NeverThrows()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid().ToString();

        _externalServiceClient
            .CallAsync("UNITY", Arg.Any<ExternalServiceRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ExternalServiceResponse<string>
            {
                IsSuccess = false,
                ErrorMessage = "Connection refused",
                StatusCode = 0
            });

        var profileRepository = Substitute.For<IReadRepository<Profile>>();
        profileRepository.GetByIdAsync(profileId, Arg.Any<CancellationToken>())
            .Returns(new Profile { Id = profileId, Subject = "test-subject", Issuer = "test-issuer" });

        var pluginFactory = Substitute.For<IProfilePluginFactory>();
        pluginFactory.GetPlugin("UNITY").Returns(_sut);

        var service = new ProfileDataRetrievalService(
            pluginFactory,
            profileRepository,
            NullLogger<ProfileDataRetrievalService>.Instance);

        var additionalData = new Dictionary<string, object> { ["SubmissionId"] = submissionId };

        var act = () => service.RetrieveProfileDataAsync(
            profileId, "UNITY", "PROV1", "SUBMISSIONFORM", "test-subject", additionalData, CancellationToken.None);

        var result = await act.Should().NotThrowAsync();
        result.Subject.Status.Should().Be(ResultStatus.Error);
        // The detailed exception message (plugin name, downstream reason, ids) must never reach
        // the client — only a generic message is returned; the detail is logged server-side instead.
        result.Subject.Errors.Should().Contain("Unable to retrieve the requested data. Please try again later.");
        result.Subject.Errors.Should().NotContain(e => e.Contains("Connection refused"));
    }
}
