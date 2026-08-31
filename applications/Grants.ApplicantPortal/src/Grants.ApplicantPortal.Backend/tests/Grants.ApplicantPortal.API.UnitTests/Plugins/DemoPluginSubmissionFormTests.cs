using System.Text.Json;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins.Demo;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UnitTests.Plugins;

/// <summary>
/// Tests for the DEMO plugin's SUBMISSIONFORM handling (Demo.SubmissionForm.cs).
/// Verifies the fixture is returned and that retrieval goes through the shared
/// cache-aside service on-demand (only when invoked, never pre-seeded).
/// </summary>
public class DemoPluginSubmissionFormTests
{
    private readonly DemoPlugin _sut;
    private readonly IDistributedCache _cache;
    private readonly IPluginCacheService _pluginCacheService;

    public DemoPluginSubmissionFormTests()
    {
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheOptions = Options.Create(new ProfileCacheOptions
        {
            CacheKeyPrefix = "profile:",
            CacheExpiryMinutes = 60,
            SlidingExpiryMinutes = 15
        });
        _pluginCacheService = new PluginCacheService(_cache, cacheOptions, NullLogger<PluginCacheService>.Instance);

        _sut = new DemoPlugin(NullLogger<DemoPlugin>.Instance, _cache, cacheOptions, _pluginCacheService);
    }

    private static ProfilePopulationMetadata BuildMetadata(Guid profileId, string submissionId) =>
        new(profileId, "DEMO", "PROGRAM1", "SUBMISSIONFORM", "subject",
            new Dictionary<string, object> { ["SubmissionId"] = submissionId });

    [Fact]
    public async Task PopulateProfileAsync_SubmissionForm_ReturnsFixtureSchemaAndData()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid().ToString();
        var metadata = BuildMetadata(profileId, submissionId);

        var result = await _sut.PopulateProfileAsync(metadata);

        result.Key.Should().Be("SUBMISSIONFORM");
        result.Data.Should().BeOfType<SubmissionFormResponse>();

        var form = (SubmissionFormResponse)result.Data;
        form.Schema.Should().NotBeNull();
        form.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task PopulateProfileAsync_SubmissionForm_IsCachedOnDemand_OnlyAfterFirstInvocation()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid().ToString();
        var metadata = BuildMetadata(profileId, submissionId);

        var cacheKey = _pluginCacheService.BuildCacheKey(profileId, "DEMO", $"PROGRAM1:SUBMISSIONFORM:{submissionId}");

        // Not cached before the endpoint/plugin is invoked
        (await _cache.GetStringAsync(cacheKey)).Should().BeNull();

        var first = await _sut.PopulateProfileAsync(metadata);
        first.CacheStatus.Should().Be("MISS");

        // Cached exactly once it has been invoked
        (await _cache.GetStringAsync(cacheKey)).Should().NotBeNull();

        var second = await _sut.PopulateProfileAsync(metadata);
        second.CacheStatus.Should().Be("HIT");
    }

    [Fact]
    public async Task PopulateProfileAsync_SubmissionForm_UsesDistinctCacheSegmentsPerSubmission()
    {
        var profileId = Guid.NewGuid();
        var submissionIdA = Guid.NewGuid().ToString();
        var submissionIdB = Guid.NewGuid().ToString();

        await _sut.PopulateProfileAsync(BuildMetadata(profileId, submissionIdA));
        var resultB = await _sut.PopulateProfileAsync(BuildMetadata(profileId, submissionIdB));

        // A different submission id is still a cache miss even though profile/provider/key match
        resultB.CacheStatus.Should().Be("MISS");
    }

    [Fact]
    public async Task PopulateProfileAsync_SubmissionForm_ReturnsSubmissionSpecificData_ForKnownSubmissions()
    {
        // Two of the 7 known static demo submissions defined in SubmissionsData —
        // "Community Health Initiative" (Program 1) and "STEM Education Excellence
        // Initiative" (Program 2). The PDF content must correspond to whichever
        // submission was actually requested, not identical fixture data for both.
        const string communityHealthSubmissionId = "a1234e56-789a-bc01-23de-f4567890ab12";
        const string stemEducationSubmissionId = "c5678e90-123a-bc45-67de-f8901234ab56";

        var profileId = Guid.NewGuid();

        var communityHealthResult = await _sut.PopulateProfileAsync(
            BuildMetadata(profileId, communityHealthSubmissionId));
        var stemEducationResult = await _sut.PopulateProfileAsync(
            BuildMetadata(profileId, stemEducationSubmissionId));

        var communityHealthForm = (SubmissionFormResponse)communityHealthResult.Data;
        var stemEducationForm = (SubmissionFormResponse)stemEducationResult.Data;

        var communityHealthData = ExtractSubmissionData(communityHealthForm);
        var stemEducationData = ExtractSubmissionData(stemEducationForm);

        // The two known submissions must produce genuinely different content —
        // not just a different echoed-back submissionId.
        communityHealthData.OrganizationName.Should().NotBe(stemEducationData.OrganizationName);
        communityHealthData.ProgramType.Should().NotBe(stemEducationData.ProgramType);
        communityHealthData.ProjectSummary.Should().NotBe(stemEducationData.ProjectSummary);

        communityHealthData.ProgramType.Should().Be("communityHealth");
        stemEducationData.ProgramType.Should().Be("stemEducation");

        communityHealthData.ProjectSummary.Should().Contain("B1234E56");
        stemEducationData.ProjectSummary.Should().Contain("D5678E90");
    }

    private static (string OrganizationName, string ProgramType, string ProjectSummary) ExtractSubmissionData(
        SubmissionFormResponse form)
    {
        var dataType = form.Data.GetType();
        var innerData = dataType.GetProperty("data")!.GetValue(form.Data)!;
        var innerType = innerData.GetType();

        var organizationName = (string)innerType.GetProperty("organizationName")!.GetValue(innerData)!;
        var programType = (string)innerType.GetProperty("programType")!.GetValue(innerData)!;
        var projectSummary = (string)innerType.GetProperty("projectSummary")!.GetValue(innerData)!;

        return (organizationName, programType, projectSummary);
    }
}
