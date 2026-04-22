using Ardalis.Result;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

/// <summary>
/// Tests that <see cref="ContactManagementService"/> enforces resource ownership
/// validation before delegating to the plugin. Ensures IDOR attacks are blocked.
/// </summary>
public class ContactManagementServiceSecurityTests
{
    private readonly IProfilePluginFactory _pluginFactory;
    private readonly IResourceOwnershipValidator _ownershipValidator;
    private readonly ContactManagementService _sut;
    private readonly IContactManagementPlugin _contactPlugin;

    private static readonly Guid ProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly ProfileContext DefaultContext = new(ProfileId, "UNITY", "PROV1", "test-subject");

    public ContactManagementServiceSecurityTests()
    {
        _pluginFactory = Substitute.For<IProfilePluginFactory>();
        _ownershipValidator = Substitute.For<IResourceOwnershipValidator>();

        // Create a mock that implements both IProfilePlugin and IContactManagementPlugin
        var plugin = Substitute.For<IProfilePlugin, IContactManagementPlugin>();
        _contactPlugin = (IContactManagementPlugin)plugin;
        _pluginFactory.GetPlugin("UNITY").Returns(plugin);

        _sut = new ContactManagementService(
            _pluginFactory,
            _ownershipValidator,
            NullLogger<ContactManagementService>.Instance);
    }

    [Fact]
    public async Task CreateContact_ReturnsForbidden_WhenApplicantOwnershipFails()
    {
        var applicantId = Guid.NewGuid();
        var request = new CreateContactRequest("Test", "Individual", false, ApplicantId: applicantId);

        _ownershipValidator.ValidateApplicantOwnershipAsync(applicantId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotOwned());

        var result = await _sut.CreateContactAsync(request, DefaultContext);

        result.Status.Should().Be(ResultStatus.Forbidden);
        await _contactPlugin.DidNotReceive().CreateContactAsync(Arg.Any<CreateContactRequest>(), Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditContact_ReturnsForbidden_WhenContactOwnershipFails()
    {
        var contactId = Guid.NewGuid();
        var request = new EditContactRequest(contactId, "Test", "Individual", false);

        _ownershipValidator.ValidateContactOwnershipAsync(contactId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotOwned());

        var result = await _sut.EditContactAsync(request, DefaultContext);

        result.Status.Should().Be(ResultStatus.Forbidden);
        await _contactPlugin.DidNotReceive().EditContactAsync(Arg.Any<EditContactRequest>(), Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditContact_ReturnsInvalid_WhenContactIsNotEditable()
    {
        var contactId = Guid.NewGuid();
        var request = new EditContactRequest(contactId, "Test", "Individual", false);

        _ownershipValidator.ValidateContactOwnershipAsync(contactId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotEditable());

        var result = await _sut.EditContactAsync(request, DefaultContext);

        result.Status.Should().Be(ResultStatus.Invalid);
        await _contactPlugin.DidNotReceive().EditContactAsync(Arg.Any<EditContactRequest>(), Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteContact_ReturnsForbidden_WhenContactOwnershipFails()
    {
        var contactId = Guid.NewGuid();

        _ownershipValidator.ValidateContactOwnershipAsync(contactId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotOwned());

        var result = await _sut.DeleteContactAsync(contactId, Guid.NewGuid(), DefaultContext);

        result.Status.Should().Be(ResultStatus.Forbidden);
    }
}
