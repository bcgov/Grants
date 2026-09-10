using Ardalis.Result;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.UseCases.Submissions.RetrieveForm;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Grants.ApplicantPortal.API.UnitTests.UseCases.Submissions.RetrieveForm;

/// <summary>
/// Tests for <see cref="RetrieveSubmissionFormQueryHandler"/>. Verifies that the
/// handler first confirms ownership of the requested SubmissionId against the
/// caller's own SUBMISSIONINFO list (IDOR prevention), only fetching SUBMISSIONFORM
/// when ownership is confirmed, and threads the submission id through as additional
/// data, hard-codes the SUBMISSIONFORM key, and passes through whatever
/// <see cref="IProfileDataRetrievalService"/> returns (success, not-found, or error)
/// without altering the shape.
/// </summary>
public class RetrieveSubmissionFormQueryHandlerTests
{
    private const string SubmissionsKey = "SUBMISSIONINFO";
    private const string SubmissionFormKey = "SUBMISSIONFORM";

    private readonly IProfileDataRetrievalService _profileDataRetrievalService;
    private readonly RetrieveSubmissionFormQueryHandler _sut;

    public RetrieveSubmissionFormQueryHandlerTests()
    {
        _profileDataRetrievalService = Substitute.For<IProfileDataRetrievalService>();
        _sut = new RetrieveSubmissionFormQueryHandler(
            _profileDataRetrievalService,
            NullLogger<RetrieveSubmissionFormQueryHandler>.Instance);
    }

    private static RetrieveSubmissionFormQuery BuildQuery(Guid profileId, Guid submissionId) =>
        new(profileId, "DEMO", "PROGRAM1", submissionId, "test-subject");

    /// <summary>
    /// Builds a SUBMISSIONINFO-shaped payload (matching SubmissionsResponse/SubmissionResponse)
    /// containing the given submission ids, mirroring the real "submissions" array shape.
    /// </summary>
    private static object BuildSubmissionsListData(params Guid[] submissionIds) => new
    {
        submissions = submissionIds.Select(id => new { id = id.ToString() }).ToArray()
    };

    private void MockOwnershipList(Guid profileId, Result<ProfileData> result) =>
        _profileDataRetrievalService
            .RetrieveProfileDataAsync(profileId, "DEMO", "PROGRAM1", SubmissionsKey, "test-subject",
                Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(result);

    private void MockFormFetch(Guid profileId, Result<ProfileData> result) =>
        _profileDataRetrievalService
            .RetrieveProfileDataAsync(profileId, "DEMO", "PROGRAM1", SubmissionFormKey, "test-subject",
                Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(result);

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenSubmissionBelongsToCaller_AndFormRetrievalSucceeds()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var query = BuildQuery(profileId, submissionId);

        var ownershipData = new ProfileData(profileId, "DEMO", "PROGRAM1", SubmissionsKey, BuildSubmissionsListData(submissionId, Guid.NewGuid()));
        MockOwnershipList(profileId, Result.Success(ownershipData));

        var expected = new ProfileData(profileId, "DEMO", "PROGRAM1", SubmissionFormKey, new { schema = new { }, data = new { } });
        MockFormFetch(profileId, Result.Success(expected));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_PassesSubmissionIdThroughAdditionalData_WhenOwnershipConfirmed()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var query = BuildQuery(profileId, submissionId);

        var ownershipData = new ProfileData(profileId, "DEMO", "PROGRAM1", SubmissionsKey, BuildSubmissionsListData(submissionId));
        MockOwnershipList(profileId, Result.Success(ownershipData));
        MockFormFetch(profileId, Result.Success(new ProfileData(profileId, "DEMO", "PROGRAM1", SubmissionFormKey, new { })));

        await _sut.Handle(query, CancellationToken.None);

        await _profileDataRetrievalService.Received(1).RetrieveProfileDataAsync(
            profileId,
            "DEMO",
            "PROGRAM1",
            SubmissionFormKey,
            "test-subject",
            Arg.Is<Dictionary<string, object>>(d => d["SubmissionId"].ToString() == submissionId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsForbidden_AndNeverFetchesForm_WhenSubmissionDoesNotBelongToCaller()
    {
        // IDOR case: the requested SubmissionId belongs to a different profile, so it does not
        // appear in the caller's own SUBMISSIONINFO list.
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var someoneElsesSubmissionId = Guid.NewGuid();
        var query = BuildQuery(profileId, submissionId);

        var ownershipData = new ProfileData(profileId, "DEMO", "PROGRAM1", SubmissionsKey, BuildSubmissionsListData(someoneElsesSubmissionId));
        MockOwnershipList(profileId, Result.Success(ownershipData));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Forbidden);

        // The plugin must never be asked for SUBMISSIONFORM data once ownership fails.
        await _profileDataRetrievalService.DidNotReceive().RetrieveProfileDataAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), SubmissionFormKey, Arg.Any<string>(),
            Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_AndNeverFetchesForm_WhenOwnershipListRetrievalReturnsNotFound()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var query = BuildQuery(profileId, submissionId);

        MockOwnershipList(profileId, Result<ProfileData>.NotFound($"Profile with ID {profileId} not found"));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);

        await _profileDataRetrievalService.DidNotReceive().RetrieveProfileDataAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), SubmissionFormKey, Arg.Any<string>(),
            Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsError_WhenPluginUnreachableOrCallFails_DuringFormRetrieval()
    {
        var profileId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var query = BuildQuery(profileId, submissionId);

        var ownershipData = new ProfileData(profileId, "DEMO", "PROGRAM1", SubmissionsKey, BuildSubmissionsListData(submissionId));
        MockOwnershipList(profileId, Result.Success(ownershipData));
        MockFormFetch(profileId, Result<ProfileData>.Error("Unable to retrieve the requested data. Please try again later."));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Error);
        result.Errors.Should().Contain("Unable to retrieve the requested data. Please try again later.");
    }
}
