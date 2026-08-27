using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.UseCases.Addresses;

namespace Grants.ApplicantPortal.API.UnitTests.UseCases.Addresses;

/// <summary>
/// Tests that <see cref="PrimaryAddressResolver"/> resolves one primary address
/// PER ADDRESS TYPE rather than a single global primary.
/// </summary>
public class PrimaryAddressResolverTests
{
  private readonly IPluginCacheService _cacheService = Substitute.For<IPluginCacheService>();

  private static readonly Guid _profileId = Guid.Parse("44444444-4444-4444-4444-444444444444");
  private const string PluginId = "UNITY";
  private const string Provider = "PROGRAM1";

  private static readonly Guid _physicalOne = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid _physicalTwo = Guid.Parse("22222222-2222-2222-2222-222222222222");
  private static readonly Guid _mailingOne = Guid.Parse("33333333-3333-3333-3333-333333333333");
  private static readonly Guid _mailingTwo = Guid.Parse("55555555-5555-5555-5555-555555555555");
  private static readonly Guid _untyped = Guid.Parse("66666666-6666-6666-6666-666666666666");

  [Fact]
  public async Task ReturnsFlaggedPrimary_ForEveryAddressType()
  {
    var businessOne = Guid.NewGuid();
    var businessTwo = Guid.NewGuid();

    GivenCachedAddresses(
      Address(_physicalOne, "Physical", isPrimary: true),
      Address(_physicalTwo, "Physical", isPrimary: false),
      Address(_mailingOne, "Mailing", isPrimary: false),
      Address(_mailingTwo, "Mailing", isPrimary: true),
      Address(businessOne, "Business", isPrimary: false),
      Address(businessTwo, "Business", isPrimary: true));

    var result = await Resolve();

    // Grouping is generic over the type value, so a third type resolves like the first two.
    result.Should().HaveCount(3);
    result["Physical"].Should().Be(_physicalOne);
    result["Mailing"].Should().Be(_mailingTwo);
    result["Business"].Should().Be(businessTwo);
  }

  [Fact]
  public async Task InfersLatestCreatedAddress_PerType_OnlyForTypesWithoutAFlag()
  {
    var businessNewer = Guid.NewGuid();
    var businessOlder = Guid.NewGuid();

    GivenCachedAddresses(
      // Physical is flagged on the OLDER address, so the flag must beat recency.
      Address(_physicalOne, "Physical", isPrimary: true, creationTime: "2024-01-01T00:00:00+00:00"),
      Address(_physicalTwo, "Physical", isPrimary: false, creationTime: "2024-12-01T00:00:00+00:00"),
      // Mailing and Business carry no flag, so each infers its own most recent address.
      Address(_mailingOne, "Mailing", isPrimary: false, creationTime: "2024-03-01T00:00:00+00:00"),
      Address(_mailingTwo, "Mailing", isPrimary: false, creationTime: "2024-07-01T00:00:00+00:00"),
      Address(businessNewer, "Business", isPrimary: false, creationTime: "2024-09-01T00:00:00+00:00"),
      Address(businessOlder, "Business", isPrimary: false, creationTime: "2024-04-01T00:00:00+00:00"));

    var result = await Resolve();

    // Inference runs per group: a single global "most recent" would have picked PhysicalTwo.
    result.Should().HaveCount(3);
    result["Physical"].Should().Be(_physicalOne);
    result["Mailing"].Should().Be(_mailingTwo);
    result["Business"].Should().Be(businessNewer);
  }

  [Fact]
  public async Task FallsBackToFirstAddressOfType_WhenNoCreationTimeIsPresent()
  {
    GivenCachedAddresses(
      Address(_mailingOne, "Mailing", isPrimary: false),
      Address(_mailingTwo, "Mailing", isPrimary: false));

    var result = await Resolve();

    result["Mailing"].Should().Be(_mailingOne);
  }

  [Fact]
  public async Task ResolvesFirstFlaggedAddress_WhenOneTypeHasDuplicatePrimaries()
  {
    GivenCachedAddresses(
      Address(_mailingOne, "Mailing", isPrimary: true),
      Address(_mailingTwo, "Mailing", isPrimary: true),
      Address(_physicalOne, "Physical", isPrimary: true));

    var result = await Resolve();

    result.Should().HaveCount(2);
    result["Mailing"].Should().Be(_mailingOne);
    result["Physical"].Should().Be(_physicalOne);
  }

  [Fact]
  public async Task GroupsAddressesWithMissingOrEmptyType_WithoutFailing()
  {
    GivenCachedAddresses(
      "{\"id\":\"" + _untyped + "\",\"isPrimary\":true}",
      Address(_mailingOne, "", isPrimary: false),
      Address(_physicalOne, "Physical", isPrimary: true));

    var result = await Resolve();

    result["Physical"].Should().Be(_physicalOne);
    result[AddressTypeKey.Unknown].Should().Be(_untyped);
    result.Should().HaveCount(2);
  }

  [Fact]
  public async Task ComparesAddressTypeKeys_CaseInsensitively()
  {
    GivenCachedAddresses(
      Address(_physicalOne, "PHYSICAL", isPrimary: true),
      Address(_physicalTwo, "physical", isPrimary: false));

    var result = await Resolve();

    result.Should().HaveCount(1);
    result["Physical"].Should().Be(_physicalOne);
  }

  [Fact]
  public async Task ReturnsEmptyDictionary_WhenNothingIsCached()
  {
    _cacheService
      .TryGetAsync<ProfileData>(_profileId, PluginId, $"{Provider}:ADDRESSINFO", Arg.Any<CancellationToken>())
      .Returns((ProfileData?)null);

    var result = await Resolve();

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task ReturnsEmptyDictionary_WhenCachedDataHasNoAddressesArray()
  {
    var cached = BuildProfileData("{\"somethingElse\":[]}");
    _cacheService
      .TryGetAsync<ProfileData>(_profileId, PluginId, $"{Provider}:ADDRESSINFO", Arg.Any<CancellationToken>())
      .Returns(cached);

    var result = await Resolve();

    result.Should().BeEmpty();
  }

  [Fact]
  public async Task ReadsAddresses_WhenCachedDataIsStoredAsJsonString()
  {
    var json = "{\"addresses\":[" + Address(_physicalOne, "Physical", isPrimary: true) + "]}";
    var cached = new ProfileData(_profileId, PluginId, Provider, "ADDRESSINFO", json);

    _cacheService
      .TryGetAsync<ProfileData>(_profileId, PluginId, $"{Provider}:ADDRESSINFO", Arg.Any<CancellationToken>())
      .Returns(cached);

    var result = await Resolve();

    result["Physical"].Should().Be(_physicalOne);
  }

  private Task<IReadOnlyDictionary<string, Guid>> Resolve()
    => PrimaryAddressResolver.GetPrimaryAddressIdsByTypeAsync(
        _cacheService, _profileId, PluginId, Provider, CancellationToken.None);

  private void GivenCachedAddresses(params string[] addresses)
  {
    var cached = BuildProfileData("{\"addresses\":[" + string.Join(",", addresses) + "]}");

    _cacheService
      .TryGetAsync<ProfileData>(_profileId, PluginId, $"{Provider}:ADDRESSINFO", Arg.Any<CancellationToken>())
      .Returns(cached);
  }

  private static ProfileData BuildProfileData(string json)
  {
    using var document = JsonDocument.Parse(json);
    return new ProfileData(_profileId, PluginId, Provider, "ADDRESSINFO", document.RootElement.Clone());
  }

  private static string Address(Guid id, string? addressType, bool isPrimary, string? creationTime = null)
  {
    var parts = new List<string> { $"\"id\":\"{id}\"" };

    if (addressType != null)
      parts.Add($"\"addressType\":\"{addressType}\"");

    parts.Add($"\"isPrimary\":{(isPrimary ? "true" : "false")}");

    if (creationTime != null)
      parts.Add($"\"creationTime\":\"{creationTime}\"");

    return "{" + string.Join(",", parts) + "}";
  }
}
