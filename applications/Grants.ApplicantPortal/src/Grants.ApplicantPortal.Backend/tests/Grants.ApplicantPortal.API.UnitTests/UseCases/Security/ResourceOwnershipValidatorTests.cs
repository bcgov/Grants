using System.Text.Json;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.UseCases.Security;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Grants.ApplicantPortal.API.UnitTests.UseCases.Security;

/// <summary>
/// Tests for <see cref="ResourceOwnershipValidator"/>. Validates that ownership
/// checks correctly parse cached profile data and enforce fail-closed behavior.
/// </summary>
public class ResourceOwnershipValidatorTests
{
    private readonly IPluginCacheService _cacheService;
    private readonly IProfilePluginFactory _pluginFactory;
    private readonly ResourceOwnershipValidator _sut;

    private static readonly Guid ProfileId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly ProfileContext DefaultContext = new(ProfileId, "UNITY", "PROV1", "test-subject");

    public ResourceOwnershipValidatorTests()
    {
        _cacheService = Substitute.For<IPluginCacheService>();
        _pluginFactory = Substitute.For<IProfilePluginFactory>();

        _sut = new ResourceOwnershipValidator(
            _cacheService,
            _pluginFactory,
            NullLogger<ResourceOwnershipValidator>.Instance);
    }

    #region Contact Ownership

    [Fact]
    public async Task ValidateContactOwnership_ReturnsSuccess_WhenContactFoundInCache()
    {
        var contactId = Guid.NewGuid();
        var data = new { contacts = new[] { new { contactId = contactId.ToString(), isEditable = true } } };
        SetupCachedData("PROV1:CONTACTINFO", data);

        var result = await _sut.ValidateContactOwnershipAsync(contactId, DefaultContext);

        result.IsOwned.Should().BeTrue();
        result.IsEditable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateContactOwnership_ReturnsNotEditable_WhenContactIsNotEditable()
    {
        var contactId = Guid.NewGuid();
        var data = new { contacts = new[] { new { contactId = contactId.ToString(), isEditable = false } } };
        SetupCachedData("PROV1:CONTACTINFO", data);

        var result = await _sut.ValidateContactOwnershipAsync(contactId, DefaultContext);

        result.IsOwned.Should().BeTrue();
        result.IsEditable.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateContactOwnership_ReturnsNotOwned_WhenContactNotInCache()
    {
        var data = new { contacts = new[] { new { contactId = Guid.NewGuid().ToString(), isEditable = true } } };
        SetupCachedData("PROV1:CONTACTINFO", data);

        var result = await _sut.ValidateContactOwnershipAsync(Guid.NewGuid(), DefaultContext);

        result.IsOwned.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateContactOwnership_ReturnsNotOwned_WhenNoCachedDataAndHydrationFails()
    {
        SetupNoCachedData("PROV1:CONTACTINFO");
        _pluginFactory.GetPlugin("UNITY").Returns((IProfilePlugin?)null);

        var result = await _sut.ValidateContactOwnershipAsync(Guid.NewGuid(), DefaultContext);

        result.IsOwned.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not available");
    }

    #endregion

    #region Address Ownership

    [Fact]
    public async Task ValidateAddressOwnership_ReturnsSuccess_WhenAddressFoundInCache()
    {
        var addressId = Guid.NewGuid();
        var data = new { addresses = new[] { new { id = addressId.ToString(), isEditable = true } } };
        SetupCachedData("PROV1:ADDRESSINFO", data);

        var result = await _sut.ValidateAddressOwnershipAsync(addressId, DefaultContext);

        result.IsOwned.Should().BeTrue();
        result.IsEditable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAddressOwnership_ReturnsNotOwned_WhenAddressNotInCache()
    {
        var data = new { addresses = new[] { new { id = Guid.NewGuid().ToString(), isEditable = true } } };
        SetupCachedData("PROV1:ADDRESSINFO", data);

        var result = await _sut.ValidateAddressOwnershipAsync(Guid.NewGuid(), DefaultContext);

        result.IsOwned.Should().BeFalse();
    }

    #endregion

    #region Organization Ownership

    [Fact]
    public async Task ValidateOrganizationOwnership_ReturnsSuccess_WhenOrgFoundInOrgsArray()
    {
        var orgId = Guid.NewGuid();
        var data = new { organizations = new[] { new { id = orgId.ToString() } } };
        SetupCachedData("PROV1:ORGINFO", data);

        var result = await _sut.ValidateOrganizationOwnershipAsync(orgId, DefaultContext);

        result.IsOwned.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOrganizationOwnership_ReturnsSuccess_WhenMultipleOrgsInArray()
    {
        var orgId = Guid.NewGuid();
        var data = new { organizations = new[] { new { id = Guid.NewGuid().ToString() }, new { id = orgId.ToString() } } };
        SetupCachedData("PROV1:ORGINFO", data);

        var result = await _sut.ValidateOrganizationOwnershipAsync(orgId, DefaultContext);

        result.IsOwned.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOrganizationOwnership_ReturnsNotOwned_WhenOrgNotInCache()
    {
        var data = new { organizations = new[] { new { id = Guid.NewGuid().ToString() } } };
        SetupCachedData("PROV1:ORGINFO", data);

        var result = await _sut.ValidateOrganizationOwnershipAsync(Guid.NewGuid(), DefaultContext);

        result.IsOwned.Should().BeFalse();
    }

    #endregion

    #region Applicant Ownership

    [Fact]
    public async Task ValidateApplicantOwnership_ReturnsNotOwned_WhenApplicantIdIsEmpty()
    {
        var result = await _sut.ValidateApplicantOwnershipAsync(Guid.Empty, DefaultContext);

        result.IsOwned.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public async Task ValidateApplicantOwnership_ReturnsSuccess_WhenApplicantIdMatchesOrgId()
    {
        var applicantId = Guid.NewGuid();
        var data = new { organizations = new[] { new { id = applicantId.ToString() } } };
        SetupCachedData("PROV1:ORGINFO", data);

        var result = await _sut.ValidateApplicantOwnershipAsync(applicantId, DefaultContext);

        result.IsOwned.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApplicantOwnership_ReturnsSuccess_WhenApplicantIdFoundAmongMultipleOrgs()
    {
        var applicantId = Guid.NewGuid();
        var data = new { organizations = new[] { new { id = Guid.NewGuid().ToString() }, new { id = applicantId.ToString() } } };
        SetupCachedData("PROV1:ORGINFO", data);

        var result = await _sut.ValidateApplicantOwnershipAsync(applicantId, DefaultContext);

        result.IsOwned.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApplicantOwnership_ReturnsNotOwned_WhenApplicantIdNotInOrgs()
    {
        var data = new { organizations = new[] { new { id = Guid.NewGuid().ToString() } } };
        SetupCachedData("PROV1:ORGINFO", data);

        var result = await _sut.ValidateApplicantOwnershipAsync(Guid.NewGuid(), DefaultContext);

        result.IsOwned.Should().BeFalse();
    }

    #endregion

    #region Hydration Fallback

    [Fact]
    public async Task ValidateContactOwnership_HydratesFromPlugin_WhenCacheEmpty()
    {
        var contactId = Guid.NewGuid();
        SetupNoCachedData("PROV1:CONTACTINFO");

        // Setup plugin to return hydrated data
        var plugin = Substitute.For<IProfilePlugin>();
        var hydratedData = new ProfileData(
            ProfileId, "UNITY", "PROV1", "CONTACTINFO",
            new { contacts = new[] { new { contactId = contactId.ToString(), isEditable = true } } });
        plugin.PopulateProfileAsync(Arg.Any<ProfilePopulationMetadata>(), Arg.Any<CancellationToken>())
            .Returns(hydratedData);
        _pluginFactory.GetPlugin("UNITY").Returns(plugin);

        var result = await _sut.ValidateContactOwnershipAsync(contactId, DefaultContext);

        result.IsOwned.Should().BeTrue();
        result.IsEditable.Should().BeTrue();
    }

    #endregion

    #region Data Shape Variants (SerializeToJsonElement)

    [Fact]
    public async Task ValidateContactOwnership_ReturnsSuccess_WhenDataIsDoubleSerializedJsonElement()
    {
        // Simulates a plugin that serializes the payload as a JSON string stored inside a JsonElement
        // (i.e. JsonElement of ValueKind.String whose string value is a JSON object).
        var contactId = Guid.NewGuid();
        var innerJson = JsonSerializer.Serialize(new { contacts = new[] { new { contactId = contactId.ToString(), isEditable = true } } });
        var doubleSerializedElement = JsonSerializer.SerializeToElement(innerJson); // ValueKind.String
        var profileData = new ProfileData(ProfileId, "UNITY", "PROV1", "PROV1:CONTACTINFO", doubleSerializedElement);
        _cacheService.TryGetAsync<ProfileData>(ProfileId, "UNITY", "PROV1:CONTACTINFO", Arg.Any<CancellationToken>())
            .Returns(profileData);

        var result = await _sut.ValidateContactOwnershipAsync(contactId, DefaultContext);

        result.IsOwned.Should().BeTrue();
        result.IsEditable.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateOrganizationOwnership_ReturnsSuccess_WhenDataIsRawJsonString()
    {
        // Simulates a plugin that stores Data as a raw JSON string (not a JsonElement).
        var orgId = Guid.NewGuid();
        var rawJson = JsonSerializer.Serialize(new { organizations = new[] { new { id = orgId.ToString() } } });
        var profileData = new ProfileData(ProfileId, "UNITY", "PROV1", "PROV1:ORGINFO", rawJson);
        _cacheService.TryGetAsync<ProfileData>(ProfileId, "UNITY", "PROV1:ORGINFO", Arg.Any<CancellationToken>())
            .Returns(profileData);

        var result = await _sut.ValidateOrganizationOwnershipAsync(orgId, DefaultContext);

        result.IsOwned.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateContactOwnership_ReturnsNotOwned_WhenDataIsJsonElementStringWithMalformedJson()
    {
        // A JsonElement of ValueKind.String whose content starts with '{' but is not valid JSON.
        // Must be fail-closed: no exception, IsOwned = false.
        var contactId = Guid.NewGuid();
        var malformedJson = "{not: valid json!!!";
        var element = JsonSerializer.SerializeToElement(malformedJson); // ValueKind.String
        var profileData = new ProfileData(ProfileId, "UNITY", "PROV1", "PROV1:CONTACTINFO", element);
        _cacheService.TryGetAsync<ProfileData>(ProfileId, "UNITY", "PROV1:CONTACTINFO", Arg.Any<CancellationToken>())
            .Returns(profileData);

        Func<Task<OwnershipValidationResult>> act = () => _sut.ValidateContactOwnershipAsync(contactId, DefaultContext);

        var result = await act.Should().NotThrowAsync();
        result.Subject.IsOwned.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateContactOwnership_ReturnsNotOwned_WhenDataIsJsonElementStringWithNonJsonValue()
    {
        // A JsonElement of ValueKind.String containing a plain string (not JSON-like).
        // Must not throw and must be fail-closed.
        var contactId = Guid.NewGuid();
        var element = JsonSerializer.SerializeToElement("just a plain string value");
        var profileData = new ProfileData(ProfileId, "UNITY", "PROV1", "PROV1:CONTACTINFO", element);
        _cacheService.TryGetAsync<ProfileData>(ProfileId, "UNITY", "PROV1:CONTACTINFO", Arg.Any<CancellationToken>())
            .Returns(profileData);

        Func<Task<OwnershipValidationResult>> act = () => _sut.ValidateContactOwnershipAsync(contactId, DefaultContext);

        var result = await act.Should().NotThrowAsync();
        result.Subject.IsOwned.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private void SetupCachedData(string cacheSegment, object data)
    {
        var profileData = new ProfileData(ProfileId, "UNITY", "PROV1", cacheSegment, JsonSerializer.SerializeToElement(data));
        _cacheService.TryGetAsync<ProfileData>(ProfileId, "UNITY", cacheSegment, Arg.Any<CancellationToken>())
            .Returns(profileData);
    }

    private void SetupNoCachedData(string cacheSegment)
    {
        _cacheService.TryGetAsync<ProfileData>(ProfileId, "UNITY", cacheSegment, Arg.Any<CancellationToken>())
            .Returns((ProfileData?)null);
    }

    #endregion
}
