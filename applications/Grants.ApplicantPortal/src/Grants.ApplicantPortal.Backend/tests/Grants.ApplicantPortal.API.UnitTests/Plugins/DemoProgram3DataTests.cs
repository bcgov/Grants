using System.Collections;
using FluentAssertions;
using Grants.ApplicantPortal.API.Plugins.Demo.Data;

namespace Grants.ApplicantPortal.API.UnitTests.Plugins;

/// <summary>
/// Tests for the DEMO plugin's PROGRAM3 (large mock data set) generators — verifies
/// volume, uniqueness, determinism and internal consistency of the generated data.
/// The generators return anonymous types, so properties are read via reflection,
/// matching the approach already used by <see cref="SubmissionFormData"/>.
/// </summary>
public class DemoProgram3DataTests
{
    private static object BaseData(Guid profileId) => new { ProfileId = profileId };

    private static object[] GetArray(object wrapper, string propertyName)
    {
        var property = wrapper.GetType().GetProperty(propertyName);
        property.Should().NotBeNull($"the generator wrapper should expose a '{propertyName}' property");

        var value = property!.GetValue(wrapper);
        value.Should().BeAssignableTo<IEnumerable>();

        return ((IEnumerable)value!).Cast<object>().ToArray();
    }

    private static T? GetProp<T>(object item, string name)
    {
        var property = item.GetType().GetProperty(name);
        property.Should().NotBeNull($"the item should expose a '{name}' property");
        return (T?)property!.GetValue(item);
    }

    [Fact]
    public void GenerateProgram3Submissions_Yields120Submissions()
    {
        var result = SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid()));

        var submissions = GetArray(result, "submissions");

        submissions.Should().HaveCountGreaterThanOrEqualTo(100);
        submissions.Should().HaveCount(120);
    }

    [Fact]
    public void GenerateProgram3Submissions_AllIdsAndReferenceNosAreUnique()
    {
        var result = SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid()));
        var submissions = GetArray(result, "submissions");

        var ids = submissions.Select(s => GetProp<string>(s, "id")).ToArray();
        var referenceNos = submissions.Select(s => GetProp<string>(s, "referenceNo")).ToArray();

        ids.Should().OnlyHaveUniqueItems();
        referenceNos.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateProgram3Submissions_IsDeterministic_AcrossMultipleCalls()
    {
        var first = GetArray(SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid())), "submissions");
        var second = GetArray(SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid())), "submissions");

        var firstIds = first.Select(s => GetProp<string>(s, "id")).ToArray();
        var secondIds = second.Select(s => GetProp<string>(s, "id")).ToArray();
        firstIds.Should().Equal(secondIds);

        var firstReceivedTimes = first.Select(s => GetProp<string>(s, "receivedTime")).ToArray();
        var secondReceivedTimes = second.Select(s => GetProp<string>(s, "receivedTime")).ToArray();
        firstReceivedTimes.Should().Equal(secondReceivedTimes);

        var firstSubmissionTimes = first.Select(s => GetProp<string>(s, "submissionTime")).ToArray();
        var secondSubmissionTimes = second.Select(s => GetProp<string>(s, "submissionTime")).ToArray();
        firstSubmissionTimes.Should().Equal(secondSubmissionTimes);
    }

    [Fact]
    public void GenerateProgram3Submissions_ReceivedTimeIsDescending()
    {
        var result = SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid()));
        var submissions = GetArray(result, "submissions");

        var receivedTimes = submissions
            .Select(s => DateTimeOffset.Parse(GetProp<string>(s, "receivedTime")!).UtcDateTime)
            .ToArray();

        receivedTimes.Should().BeInDescendingOrder();
    }

    [Fact]
    public void GenerateProgram3Submissions_SubmissionTimeIsBeforeReceivedTime()
    {
        var result = SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid()));
        var submissions = GetArray(result, "submissions");

        foreach (var submission in submissions)
        {
            var receivedTime = DateTimeOffset.Parse(GetProp<string>(submission, "receivedTime")!).UtcDateTime;
            var submissionTime = DateTimeOffset.Parse(GetProp<string>(submission, "submissionTime")!).UtcDateTime;

            submissionTime.Should().BeBefore(receivedTime);
        }
    }

    [Fact]
    public void GenerateProgram3Submissions_RenewalLinkImpliesEligibleForRenewal()
    {
        var result = SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid()));
        var submissions = GetArray(result, "submissions");

        var withRenewalLink = submissions.Where(s => GetProp<object>(s, "renewalLink") is not null).ToArray();
        withRenewalLink.Should().NotBeEmpty();

        foreach (var submission in withRenewalLink)
        {
            GetProp<bool>(submission, "eligibleForRenewal").Should().BeTrue();
        }

        var withoutRenewalLink = submissions.Where(s => GetProp<object>(s, "renewalLink") is null).ToArray();
        withoutRenewalLink.Should().NotBeEmpty();

        foreach (var submission in withoutRenewalLink)
        {
            GetProp<bool>(submission, "eligibleForRenewal").Should().BeFalse();
        }
    }

    [Fact]
    public void GenerateProgram3Submissions_IncludesAnUnorderedRelatedLinkForAtLeastAFewRecords()
    {
        var result = SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid()));
        var submissions = GetArray(result, "submissions");

        var unorderedLinkCount = submissions.Count(s =>
        {
            var relatedLinks = GetProp<IEnumerable>(s, "relatedLinks")!.Cast<object>().ToArray();
            return relatedLinks.Any(l => GetProp<int>(l, "Order") == -1);
        });

        unorderedLinkCount.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void GenerateProgram3Contacts_Yields14Contacts_WithExactlyOnePrimary()
    {
        var result = ContactsData.GenerateProgram3Contacts(BaseData(Guid.NewGuid()));
        var contacts = GetArray(result, "Contacts");

        contacts.Should().HaveCount(14);
        contacts.Count(c => GetProp<bool>(c, "IsPrimary")).Should().Be(1);
    }

    [Fact]
    public void GenerateProgram3Addresses_Yields12Addresses_WithExactlyOnePrimaryPerAddressType()
    {
        var result = AddressesData.GenerateProgram3Addresses(BaseData(Guid.NewGuid()));
        var addresses = GetArray(result, "Addresses");

        addresses.Should().HaveCount(12);

        // Addresses carry a per-type primary invariant (see DemoAddressesDataTests) rather
        // than a single global primary: mixing Physical and Mailing types means each type
        // group resolves its own primary.
        foreach (var group in addresses.GroupBy(a => GetProp<string>(a, "AddressType")))
        {
            group.Count(a => GetProp<bool>(a, "IsPrimary")).Should().Be(1,
                "address type '{0}' must have exactly one primary address", group.Key);
        }
    }

    [Fact]
    public void GenerateProgram3Payments_Yields40Payments_WithUniqueIds()
    {
        var result = PaymentsData.GenerateProgram3Payments(BaseData(Guid.NewGuid()));
        var payments = GetArray(result, "payments");

        payments.Should().HaveCount(40);
        payments.Select(p => GetProp<string>(p, "id")).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GenerateProgram3Payments_IsDeterministic_AcrossMultipleCalls()
    {
        var first = GetArray(PaymentsData.GenerateProgram3Payments(BaseData(Guid.NewGuid())), "payments");
        var second = GetArray(PaymentsData.GenerateProgram3Payments(BaseData(Guid.NewGuid())), "payments");

        var firstIds = first.Select(p => GetProp<string>(p, "id")).ToArray();
        var secondIds = second.Select(p => GetProp<string>(p, "id")).ToArray();
        firstIds.Should().Equal(secondIds);

        var firstReferenceNos = first.Select(p => GetProp<string>(p, "referenceNo")).ToArray();
        var secondReferenceNos = second.Select(p => GetProp<string>(p, "referenceNo")).ToArray();
        firstReferenceNos.Should().Equal(secondReferenceNos);

        var firstAmounts = first.Select(p => GetProp<decimal>(p, "amount")).ToArray();
        var secondAmounts = second.Select(p => GetProp<decimal>(p, "amount")).ToArray();
        firstAmounts.Should().Equal(secondAmounts);

        var firstPaymentDates = first.Select(p => GetProp<string>(p, "paymentDate")).ToArray();
        var secondPaymentDates = second.Select(p => GetProp<string>(p, "paymentDate")).ToArray();
        firstPaymentDates.Should().Equal(secondPaymentDates);
    }

    [Fact]
    public void GetForm_ForProgram3Submission_ReflectsThatSubmissionsProgramType()
    {
        var submissions = GetArray(
            SubmissionsData.GenerateProgram3Submissions(BaseData(Guid.NewGuid())), "submissions");

        // Pick a known Program 3 submission (first record — "Rural Arts Access Grant").
        var submission = submissions.First();
        var submissionId = GetProp<string>(submission, "id")!;
        var expectedType = GetProp<string>(submission, "type")!;
        var expectedReferenceNo = GetProp<string>(submission, "referenceNo")!;

        var (_, data) = SubmissionFormData.GetForm(submissionId);

        var innerData = data.GetType().GetProperty("data")!.GetValue(data)!;
        var innerType = innerData.GetType();

        var organizationName = (string)innerType.GetProperty("organizationName")!.GetValue(innerData)!;
        var programType = (string)innerType.GetProperty("programType")!.GetValue(innerData)!;
        var projectSummary = (string)innerType.GetProperty("projectSummary")!.GetValue(innerData)!;

        // Program type must reflect the actual submission (not the generic fallback).
        organizationName.Should().NotBe("Demo Community Society");
        programType.Should().Be("ruralArtsAccess");
        projectSummary.Should().Contain(expectedType);
        projectSummary.Should().Contain(expectedReferenceNo);
    }
}
