using System.Text.Json;
using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;

namespace Grants.ApplicantPortal.API.UnitTests.Plugins;

/// <summary>
/// Tests that the Demo address store keeps ONE primary address PER ADDRESS TYPE
/// instead of a single global primary.
/// </summary>
public class DemoAddressesDataTests
{
  private const string Provider = "PROGRAM1";

  /// <summary>Matches the camelCase policy the demo plugin serializes with before caching.</summary>
  private static readonly JsonSerializerOptions _camelCase = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  /// <summary>Minimal shape expected by AddressesData.GenerateProgram1Addresses.</summary>
  private sealed record BaseData(Guid ProfileId);

  private sealed record CachedAddress(string Id, string AddressType, bool IsPrimary);

  [Fact]
  public void AddAddress_DemotesTheExistingPrimaryOfTheSameTypeOnly()
  {
    var profileId = Guid.NewGuid();

    var physicalId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "1 Physical Way", isPrimary: true));
    var firstMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "1 Mailing Way", isPrimary: true));
    var secondMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "2 Mailing Way", isPrimary: true));

    var addresses = Generate(profileId);

    // The second Mailing address takes over its own type group only: the first Mailing
    // address is demoted while the Physical primary is left alone.
    Single(addresses, firstMailingId).IsPrimary.Should().BeFalse();
    Single(addresses, secondMailingId).IsPrimary.Should().BeTrue();
    Single(addresses, physicalId).IsPrimary.Should().BeTrue();
    AssertOnePrimaryPerType(addresses);
  }

  [Fact]
  public void SetAddressAsPrimary_DoesNotDisturbThePrimaryOfAnotherType()
  {
    var profileId = Guid.NewGuid();

    var physicalId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "1 Physical Way", isPrimary: true));
    var firstMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "1 Mailing Way", isPrimary: true));
    var secondMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "2 Mailing Way", isPrimary: false));

    var updated = AddressesData.SetAddressAsPrimary(Provider, profileId, Guid.Parse(secondMailingId));

    updated.Should().BeTrue();

    var addresses = Generate(profileId);

    Single(addresses, physicalId).IsPrimary.Should().BeTrue();
    Single(addresses, secondMailingId).IsPrimary.Should().BeTrue();
    Single(addresses, firstMailingId).IsPrimary.Should().BeFalse();
    AssertOnePrimaryPerType(addresses);
  }

  [Fact]
  public void UpdateAddress_ClearsPrimaryOfTheNewTypeOnly()
  {
    var profileId = Guid.NewGuid();

    var physicalId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "1 Physical Way", isPrimary: true));
    var firstMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "1 Mailing Way", isPrimary: true));
    var secondMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "2 Mailing Way", isPrimary: false));

    var updated = AddressesData.UpdateAddress(Provider, profileId, Guid.Parse(secondMailingId),
      Edit(Guid.Parse(secondMailingId), "Mailing", "2 Mailing Way", isPrimary: true));

    updated.Should().BeTrue();

    var addresses = Generate(profileId);

    Single(addresses, physicalId).IsPrimary.Should().BeTrue();
    Single(addresses, secondMailingId).IsPrimary.Should().BeTrue();
    Single(addresses, firstMailingId).IsPrimary.Should().BeFalse();
    AssertOnePrimaryPerType(addresses);
  }

  [Fact]
  public void UpdateAddress_ThatChangesType_VacatesTheOldTypeGroup()
  {
    var profileId = Guid.NewGuid();

    var movingId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "1 Physical Way", isPrimary: true));
    var otherPhysicalId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "2 Physical Way", isPrimary: false));
    var mailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "1 Mailing Way", isPrimary: true));

    var updated = AddressesData.UpdateAddress(Provider, profileId, Guid.Parse(movingId),
      Edit(Guid.Parse(movingId), "Mailing", "1 Physical Way", isPrimary: true));

    updated.Should().BeTrue();

    var addresses = Generate(profileId);

    // It now contests (and wins) the Mailing group
    Single(addresses, movingId).AddressType.Should().Be("Mailing");
    Single(addresses, movingId).IsPrimary.Should().BeTrue();
    Single(addresses, mailingId).IsPrimary.Should().BeFalse();

    // and it no longer holds the Physical group, which re-infers its own primary
    addresses.Should().NotContain(a => a.AddressType == "Physical" && a.Id == movingId);
    Single(addresses, otherPhysicalId).AddressType.Should().Be("Physical");
    AssertOnePrimaryPerType(addresses);
  }

  [Fact]
  public void DeleteAddress_PromotesWithinTheDeletedAddressTypeOnly()
  {
    var profileId = Guid.NewGuid();

    var physicalId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "1 Physical Way", isPrimary: true));
    var primaryMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "1 Mailing Way", isPrimary: true));
    var otherMailingId = AddressesData.AddAddress(Provider, profileId, Address("Mailing", "2 Mailing Way", isPrimary: false));

    var deleted = AddressesData.DeleteAddress(Provider, profileId, Guid.Parse(primaryMailingId));

    deleted.Should().BeTrue();

    var addresses = Generate(profileId);

    addresses.Should().NotContain(a => a.Id == primaryMailingId);
    Single(addresses, otherMailingId).IsPrimary.Should().BeTrue();
    Single(addresses, physicalId).IsPrimary.Should().BeTrue();
    AssertOnePrimaryPerType(addresses);
  }

  [Fact]
  public void GenerateAddresses_ResolvesExactlyOnePrimaryPerType_WhenNoStoredPrimaryExists()
  {
    var profileId = Guid.NewGuid();

    // No stored addresses at all: the demo defaults alone must still resolve one primary per type
    var addresses = Generate(profileId);

    addresses.Should().NotBeEmpty();
    AssertOnePrimaryPerType(addresses);
  }

  [Fact]
  public void GenerateAddresses_ResolvesDuplicatePrimariesPerType()
  {
    var profileId = Guid.NewGuid();

    // The demo defaults already contain a primary Physical address; adding another stored
    // primary Physical address makes the type group hold two flagged primaries at read time.
    var storedPhysicalId = AddressesData.AddAddress(Provider, profileId, Address("Physical", "9 Physical Way", isPrimary: true));

    var addresses = Generate(profileId);

    Single(addresses, storedPhysicalId).IsPrimary.Should().BeTrue();
    AssertOnePrimaryPerType(addresses);
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  private static CreateAddressRequest Address(string addressType, string street, bool isPrimary)
    => new(addressType, street, "Victoria", "BC", "V8W 1A1", isPrimary, ApplicantId: Guid.NewGuid());

  private static EditAddressRequest Edit(Guid addressId, string addressType, string street, bool isPrimary)
    => new(addressId, addressType, street, "Victoria", "BC", "V8W 1A1", isPrimary, ApplicantId: Guid.NewGuid());

  private static List<CachedAddress> Generate(Guid profileId)
  {
    var generated = AddressesData.GenerateProgram1Addresses(new BaseData(profileId));

    var json = JsonSerializer.Serialize(generated, _camelCase);

    using var document = JsonDocument.Parse(json);

    return [.. document.RootElement.GetProperty("addresses")
      .EnumerateArray()
      .Select(a => new CachedAddress(
        a.GetProperty("id").GetString()!,
        a.GetProperty("addressType").GetString()!,
        a.GetProperty("isPrimary").GetBoolean()))];
  }

  private static CachedAddress Single(List<CachedAddress> addresses, string id)
    => addresses.Should().ContainSingle(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)).Subject;

  private static void AssertOnePrimaryPerType(List<CachedAddress> addresses)
  {
    foreach (var group in addresses.GroupBy(a => a.AddressType, StringComparer.OrdinalIgnoreCase))
    {
      group.Count(a => a.IsPrimary).Should().Be(1,
        "address type '{0}' must have exactly one primary address", group.Key);
    }
  }
}
