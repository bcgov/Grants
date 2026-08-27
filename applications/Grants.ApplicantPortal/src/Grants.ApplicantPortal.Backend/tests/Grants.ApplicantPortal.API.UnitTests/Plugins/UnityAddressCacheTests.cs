using System.Text.Json;
using Grants.ApplicantPortal.API.Core;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Plugins.Unity;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Logging.Abstractions;

namespace Grants.ApplicantPortal.API.UnitTests.Plugins;

/// <summary>
/// Tests the Unity plugin's optimistic address cache patches. The patch routines are private,
/// so they are exercised through the public plugin operations and asserted on the JSON the
/// plugin writes back to the cache. Every patch must be scoped to a single address type.
/// </summary>
public class UnityAddressCacheTests
{
  private readonly IPluginCacheService _cacheService = Substitute.For<IPluginCacheService>();
  private readonly IMessagePublisher _messagePublisher = Substitute.For<IMessagePublisher>();
  private readonly UnityPlugin _sut;

  private static readonly Guid _profileId = Guid.Parse("88888888-8888-8888-8888-888888888888");
  private const string Provider = "PROGRAM1";
  private static readonly ProfileContext _context = new(_profileId, "UNITY", Provider, "test-subject");

  private static readonly Guid _physicalId = Guid.Parse("11111111-1111-1111-1111-111111111111");
  private static readonly Guid _mailingOldId = Guid.Parse("22222222-2222-2222-2222-222222222222");
  private static readonly Guid _mailingNewId = Guid.Parse("33333333-3333-3333-3333-333333333333");

  private ProfileData? _saved;

  private sealed record CachedAddress(string Id, string AddressType, bool? IsPrimary);

  public UnityAddressCacheTests()
  {
    _sut = new UnityPlugin(
      NullLogger<UnityPlugin>.Instance,
      Substitute.For<IExternalServiceClient>(),
      _cacheService,
      _messagePublisher);

    _cacheService
      .SetAsync(
        Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(),
        Arg.Do<ProfileData>(p => _saved = p), Arg.Any<CancellationToken>())
      .Returns(Task.CompletedTask);
  }

  [Fact]
  public async Task CreateAddress_ClearsPrimaryOfTheSameTypeOnly()
  {
    GivenCachedAddresses(
      Address(_physicalId, "Physical", isPrimary: true),
      Address(_mailingOldId, "Mailing", isPrimary: true));

    var result = await _sut.CreateAddressAsync(
      new CreateAddressRequest("Mailing", "9 Mailing Way", "Victoria", "BC", "V8W 1A1", true, ApplicantId: Guid.NewGuid()),
      _context);

    result.IsSuccess.Should().BeTrue();

    var addresses = SavedAddresses();

    Find(addresses, _physicalId).IsPrimary.Should().BeTrue("another address type keeps its own primary");
    Find(addresses, _mailingOldId).IsPrimary.Should().BeFalse();
    addresses.Single(a => a.Id == result.Value.ToString()).IsPrimary.Should().BeTrue();
  }

  [Fact]
  public async Task CreateAddress_WritesIsPrimaryFalse_EvenWhenTheCachedEntryLacksTheProperty()
  {
    // The existing Mailing address has no isPrimary property at all
    GivenCachedAddresses(
      $"{{\"id\":\"{_mailingOldId}\",\"addressType\":\"Mailing\"}}",
      Address(_physicalId, "Physical", isPrimary: true));

    await _sut.CreateAddressAsync(
      new CreateAddressRequest("Mailing", "9 Mailing Way", "Victoria", "BC", "V8W 1A1", true, ApplicantId: Guid.NewGuid()),
      _context);

    var addresses = SavedAddresses();

    Find(addresses, _mailingOldId).IsPrimary.Should().BeFalse("the flag is written even when absent");
    Find(addresses, _physicalId).IsPrimary.Should().BeTrue();
  }

  [Fact]
  public async Task SetAsPrimaryAddress_TogglesTheTargetTypeGroupOnly()
  {
    GivenCachedAddresses(
      Address(_physicalId, "Physical", isPrimary: true),
      Address(_mailingOldId, "Mailing", isPrimary: true),
      Address(_mailingNewId, "Mailing", isPrimary: false));

    var result = await _sut.SetAsPrimaryAddressAsync(_mailingNewId, _context);

    result.IsSuccess.Should().BeTrue();

    var addresses = SavedAddresses();

    Find(addresses, _physicalId).IsPrimary.Should().BeTrue("the Physical group must not be touched");
    Find(addresses, _mailingOldId).IsPrimary.Should().BeFalse();
    Find(addresses, _mailingNewId).IsPrimary.Should().BeTrue();
  }

  [Fact]
  public async Task EditAddress_ThatChangesType_VacatesTheOldGroupAndContestsTheNewOne()
  {
    GivenCachedAddresses(
      Address(_physicalId, "Physical", isPrimary: true),
      Address(_mailingOldId, "Mailing", isPrimary: true));

    var result = await _sut.EditAddressAsync(
      new EditAddressRequest(_physicalId, "Mailing", "1 Physical Way", "Victoria", "BC", "V8W 1A1", true),
      _context);

    result.IsSuccess.Should().BeTrue();

    var addresses = SavedAddresses();

    var moved = Find(addresses, _physicalId);
    moved.AddressType.Should().Be("Mailing");
    moved.IsPrimary.Should().BeTrue();
    Find(addresses, _mailingOldId).IsPrimary.Should().BeFalse("the new type group is contested");
  }

  [Fact]
  public async Task EditAddress_LeavesOtherTypeGroupsUntouched()
  {
    GivenCachedAddresses(
      Address(_physicalId, "Physical", isPrimary: true),
      Address(_mailingOldId, "Mailing", isPrimary: true),
      Address(_mailingNewId, "Mailing", isPrimary: false));

    await _sut.EditAddressAsync(
      new EditAddressRequest(_mailingNewId, "Mailing", "3 Mailing Way", "Victoria", "BC", "V8W 1A1", true),
      _context);

    var addresses = SavedAddresses();

    Find(addresses, _physicalId).IsPrimary.Should().BeTrue();
    Find(addresses, _mailingOldId).IsPrimary.Should().BeFalse();
    Find(addresses, _mailingNewId).IsPrimary.Should().BeTrue();
  }

  [Fact]
  public async Task DeleteAddress_PromotesTheMostRecentRemainingAddressOfTheSameTypeOnly()
  {
    GivenCachedAddresses(
      Address(_physicalId, "Physical", isPrimary: true, creationTime: "2024-01-01T00:00:00+00:00"),
      Address(_mailingOldId, "Mailing", isPrimary: true, creationTime: "2024-02-01T00:00:00+00:00"),
      Address(_mailingNewId, "Mailing", isPrimary: false, creationTime: "2024-08-01T00:00:00+00:00"));

    var result = await _sut.DeleteAddressAsync(_mailingOldId, Guid.NewGuid(), _context);

    result.IsSuccess.Should().BeTrue();

    var addresses = SavedAddresses();

    addresses.Should().HaveCount(2);
    addresses.Should().NotContain(a => a.Id == _mailingOldId.ToString());
    Find(addresses, _mailingNewId).IsPrimary.Should().BeTrue();
    Find(addresses, _physicalId).IsPrimary.Should().BeTrue("the Physical group keeps its own primary");
  }

  [Fact]
  public async Task DeleteAddress_DoesNotPromoteAnotherType_WhenNoAddressOfTheDeletedTypeRemains()
  {
    GivenCachedAddresses(
      Address(_physicalId, "Physical", isPrimary: false),
      Address(_mailingOldId, "Mailing", isPrimary: true));

    await _sut.DeleteAddressAsync(_mailingOldId, Guid.NewGuid(), _context);

    var addresses = SavedAddresses();

    addresses.Should().ContainSingle();
    Find(addresses, _physicalId).IsPrimary.Should().BeFalse("promotion never crosses address types");
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private void GivenCachedAddresses(params string[] addresses)
  {
    var json = "{\"addresses\":[" + string.Join(",", addresses) + "]}";
    using var document = JsonDocument.Parse(json);
    var cached = new ProfileData(_profileId, "UNITY", Provider, "ADDRESSINFO", document.RootElement.Clone());

    _cacheService
      .TryGetAsync<ProfileData>(_profileId, "UNITY", $"{Provider}:ADDRESSINFO", Arg.Any<CancellationToken>())
      .Returns(cached);
  }

  private List<CachedAddress> SavedAddresses()
  {
    _saved.Should().NotBeNull("the plugin must patch the cached addresses");

    var json = JsonSerializer.Serialize(_saved!.Data);
    using var document = JsonDocument.Parse(json);

    return [.. document.RootElement.GetProperty("addresses")
      .EnumerateArray()
      .Select(a => new CachedAddress(
        a.GetProperty("id").GetString()!,
        a.TryGetProperty("addressType", out var t) ? t.GetString()! : string.Empty,
        a.TryGetProperty("isPrimary", out var p) ? p.GetBoolean() : null))];
  }

  private static CachedAddress Find(List<CachedAddress> addresses, Guid id)
    => addresses.Should().ContainSingle(a => string.Equals(a.Id, id.ToString(), StringComparison.OrdinalIgnoreCase)).Subject;

  private static string Address(Guid id, string addressType, bool isPrimary, string? creationTime = null)
  {
    var parts = new List<string>
    {
      $"\"id\":\"{id}\"",
      $"\"addressType\":\"{addressType}\"",
      $"\"isPrimary\":{(isPrimary ? "true" : "false")}"
    };

    if (creationTime != null)
      parts.Add($"\"creationTime\":\"{creationTime}\"");

    return "{" + string.Join(",", parts) + "}";
  }
}
