using System.Text.Json;
using Ardalis.Result;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.UseCases.Addresses.SetAsPrimary;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grants.ApplicantPortal.API.UnitTests.UseCases.Addresses;

/// <summary>
/// Tests that <see cref="SetAsPrimaryAddressHandler"/> returns the primary address of every
/// address type after a successful mutation, and maps domain failures to Ardalis results.
/// </summary>
public class SetAsPrimaryAddressHandlerTests
{
  private readonly IAddressManagementService _addressManagementService = Substitute.For<IAddressManagementService>();
  private readonly IPluginCacheService _cacheService = Substitute.For<IPluginCacheService>();
  private readonly SetAsPrimaryAddressHandler _sut;

  private static readonly Guid _profileId = Guid.Parse("77777777-7777-7777-7777-777777777777");
  private const string PluginId = "UNITY";
  private const string Provider = "PROGRAM1";

  private static readonly Guid _physicalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid _mailingId = Guid.Parse("22222222-2222-2222-2222-222222222222");

  public SetAsPrimaryAddressHandlerTests()
  {
    _sut = new SetAsPrimaryAddressHandler(
      _addressManagementService,
      _cacheService,
      NullLogger<SetAsPrimaryAddressHandler>.Instance);
  }

  [Fact]
  public async Task ReturnsPrimaryAddressPerType_WhenTheMutationSucceeds()
  {
    _addressManagementService
      .SetAsPrimaryAddressAsync(_mailingId, Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>())
      .Returns(Result.Success());

    GivenCachedAddresses(
      $"{{\"id\":\"{_physicalId}\",\"addressType\":\"Physical\",\"isPrimary\":true}}",
      $"{{\"id\":\"{_mailingId}\",\"addressType\":\"Mailing\",\"isPrimary\":true}}");

    var result = await _sut.Handle(Command(_mailingId), CancellationToken.None);

    result.IsSuccess.Should().BeTrue();
    result.Value.AddressId.Should().Be(_mailingId);
    result.Value.PrimaryAddressIdsByType.Should().HaveCount(2);
    result.Value.PrimaryAddressIdsByType["Physical"].Should().Be(_physicalId);
    result.Value.PrimaryAddressIdsByType["Mailing"].Should().Be(_mailingId);
  }

  [Fact]
  public async Task ReturnsNotFound_WhenTheAddressDoesNotExist()
  {
    _addressManagementService
      .SetAsPrimaryAddressAsync(_mailingId, Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>())
      .Returns(Result.NotFound());

    var result = await _sut.Handle(Command(_mailingId), CancellationToken.None);

    result.Status.Should().Be(ResultStatus.NotFound);
  }

  [Fact]
  public async Task ReturnsForbidden_WhenOwnershipValidationFails()
  {
    // The service returns Forbidden when the address belongs to another applicant. Mapping it
    // to Invalid would surface an IDOR attempt as an empty 422 instead of a 403, and would
    // diverge from the Create, Edit and Delete handlers.
    _addressManagementService
      .SetAsPrimaryAddressAsync(_mailingId, Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>())
      .Returns(Result.Forbidden());

    var result = await _sut.Handle(Command(_mailingId), CancellationToken.None);

    result.Status.Should().Be(ResultStatus.Forbidden);
  }

  [Fact]
  public async Task ReturnsInvalid_WhenTheServiceRejectsTheRequest()
  {
    _addressManagementService
      .SetAsPrimaryAddressAsync(_mailingId, Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>())
      .Returns(Result.Invalid(new ValidationError("Address is not editable")));

    var result = await _sut.Handle(Command(_mailingId), CancellationToken.None);

    result.Status.Should().Be(ResultStatus.Invalid);
    result.ValidationErrors.Should().ContainSingle(e => e.ErrorMessage == "Address is not editable");
  }

  [Fact]
  public async Task ReturnsError_WhenTheServiceThrows()
  {
    _addressManagementService
      .SetAsPrimaryAddressAsync(_mailingId, Arg.Any<ProfileContext>(), Arg.Any<CancellationToken>())
      .Returns<Result>(_ => throw new InvalidOperationException("boom"));

    var result = await _sut.Handle(Command(_mailingId), CancellationToken.None);

    result.Status.Should().Be(ResultStatus.Error);
    result.Errors.Should().Contain("An unexpected error occurred while setting the address as primary");
  }

  private static SetAsPrimaryAddressCommand Command(Guid addressId)
    => new(addressId, _profileId, PluginId, Provider, "test-subject");

  private void GivenCachedAddresses(params string[] addresses)
  {
    var json = "{\"addresses\":[" + string.Join(",", addresses) + "]}";
    using var document = JsonDocument.Parse(json);
    var cached = new ProfileData(_profileId, PluginId, Provider, "ADDRESSINFO", document.RootElement.Clone());

    _cacheService
      .TryGetAsync<ProfileData>(_profileId, PluginId, $"{Provider}:ADDRESSINFO", Arg.Any<CancellationToken>())
      .Returns(cached);
  }
}
