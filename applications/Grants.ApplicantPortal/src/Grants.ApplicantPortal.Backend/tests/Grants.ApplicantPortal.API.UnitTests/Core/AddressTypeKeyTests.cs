using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;
using Xunit;

namespace Grants.ApplicantPortal.API.UnitTests.Core;

/// <summary>
/// The plugins that enforce "one primary per address type" and the resolver that reports it
/// all group through this helper, so these tests pin the one definition they share.
/// </summary>
public class AddressTypeKeyTests
{
  [Theory]
  [InlineData("Physical", "Physical")]
  [InlineData("  Mailing  ", "Mailing")]
  public void Normalize_TrimsButPreservesCasing(string addressType, string expected)
  {
    AddressTypeKey.Normalize(addressType).Should().Be(expected);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Normalize_BlankValueBecomesUnknown(string? addressType)
  {
    AddressTypeKey.Normalize(addressType).Should().Be(AddressTypeKey.Unknown);
  }

  [Theory]
  [InlineData("Physical", "physical")]
  [InlineData("MAILING", "  mailing ")]
  public void AreSame_IgnoresCasingAndSurroundingWhitespace(string left, string right)
  {
    AddressTypeKey.AreSame(left, right).Should().BeTrue();
  }

  /// <summary>
  /// Every blank form has to land in one group, otherwise two untyped addresses could each
  /// hold a primary flag while the resolver reports only one of them.
  /// </summary>
  [Theory]
  [InlineData(null, "")]
  [InlineData("", "   ")]
  [InlineData(null, null)]
  public void AreSame_TreatsEveryBlankFormAsOneGroup(string? left, string? right)
  {
    AddressTypeKey.AreSame(left, right).Should().BeTrue();
  }

  /// <summary>
  /// The option Key travels to the client and comes back on create and edit, where it is used
  /// to group addresses, so it has to already be a normalized key. The property initializer
  /// that guarantees this is easy to drop in a refactor, which is what these pin.
  /// </summary>
  [Theory]
  [InlineData("  Mailing  ", "Mailing")]
  [InlineData("", AddressTypeKey.Unknown)]
  [InlineData("   ", AddressTypeKey.Unknown)]
  public void AddressTypeOption_NormalizesItsKeyOnConstruction(string key, string expected)
  {
    new AddressTypeOption(key, "Mailing address").Key.Should().Be(expected);
  }

  [Fact]
  public void AddressTypeOption_LeavesTheLabelUntouched()
  {
    var option = new AddressTypeOption("  mailing  ", "  Mailing address  ");

    option.Key.Should().Be("mailing");
    option.Label.Should().Be("  Mailing address  ");
  }

  [Fact]
  public void AreSame_DoesNotGroupDifferentTypesTogether()
  {
    AddressTypeKey.AreSame("Physical", "Mailing").Should().BeFalse();
    AddressTypeKey.AreSame(null, "Physical").Should().BeFalse();
  }
}
