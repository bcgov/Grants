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
/// Tests that <see cref="AddressManagementService"/> enforces resource ownership
/// validation before delegating to the plugin. Ensures IDOR attacks are blocked.
/// </summary>
public class AddressManagementServiceSecurityTests
{
    private readonly IProfilePluginFactory _pluginFactory;
    private readonly IResourceOwnershipValidator _ownershipValidator;
    private readonly AddressManagementService _sut;
    private readonly IAddressManagementPlugin _addressPlugin;

    private static readonly Guid ProfileId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly ProfileContext DefaultContext = new(ProfileId, "UNITY", "PROV1", "test-subject");

    public AddressManagementServiceSecurityTests()
    {
        _pluginFactory = Substitute.For<IProfilePluginFactory>();
        _ownershipValidator = Substitute.For<IResourceOwnershipValidator>();

        // Create a mock that implements both IProfilePlugin and IAddressManagementPlugin
        var plugin = Substitute.For<IProfilePlugin, IAddressManagementPlugin>();
        _addressPlugin = (IAddressManagementPlugin)plugin;
        _pluginFactory.GetPlugin("UNITY").Returns(plugin);

        _sut = new AddressManagementService(
            _pluginFactory,
            _ownershipValidator,
            NullLogger<AddressManagementService>.Instance);
    }

    [Fact]
    public async Task CreateAddress_ReturnsForbidden_WhenApplicantOwnershipFails()
    {
        var applicantId = Guid.NewGuid();
        var request = new CreateAddressRequest("Mailing", "123 Main St", "Victoria", "BC", "V8W 1A1", false, ApplicantId: applicantId);

        _ownershipValidator.ValidateApplicantOwnershipAsync(applicantId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotOwned());

        var result = await _sut.CreateAddressAsync(request, DefaultContext);

        result.Status.Should().Be(ResultStatus.Forbidden);
        await _addressPlugin.DidNotReceive().CreateAddressAsync(Arg.Any<CreateAddressRequest>(), Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditAddress_ReturnsForbidden_WhenAddressOwnershipFails()
    {
        var addressId = Guid.NewGuid();
        var request = new EditAddressRequest(addressId, "Mailing", "123 Main St", "Victoria", "BC", "V8W 1A1", false);

        _ownershipValidator.ValidateAddressOwnershipAsync(addressId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotOwned());

        var result = await _sut.EditAddressAsync(request, DefaultContext);

        result.Status.Should().Be(ResultStatus.Forbidden);
        await _addressPlugin.DidNotReceive().EditAddressAsync(Arg.Any<EditAddressRequest>(), Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditAddress_ReturnsInvalid_WhenAddressIsNotEditable()
    {
        var addressId = Guid.NewGuid();
        var request = new EditAddressRequest(addressId, "Mailing", "123 Main St", "Victoria", "BC", "V8W 1A1", false);

        _ownershipValidator.ValidateAddressOwnershipAsync(addressId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotEditable());

        var result = await _sut.EditAddressAsync(request, DefaultContext);

        result.Status.Should().Be(ResultStatus.Invalid);
        await _addressPlugin.DidNotReceive().EditAddressAsync(Arg.Any<EditAddressRequest>(), Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAddress_ReturnsForbidden_WhenAddressOwnershipFails()
    {
        var addressId = Guid.NewGuid();

        _ownershipValidator.ValidateAddressOwnershipAsync(addressId, DefaultContext, Arg.Any<CancellationToken>())
            .Returns(OwnershipValidationResult.NotOwned());

        var result = await _sut.DeleteAddressAsync(addressId, Guid.NewGuid(), DefaultContext);

        result.Status.Should().Be(ResultStatus.Forbidden);
    }
}
