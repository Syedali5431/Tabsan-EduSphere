using FluentAssertions;
using Tabsan.EduSphere.Domain.Licensing;
using Xunit;

namespace Tabsan.EduSphere.UnitTests;

public class LicenseStateTests
{
    private static readonly DateTime FixedExpiry = new(2027, 3, 3, 23, 59, 59);

    [Fact]
    public void RefreshStatus_UsesSuppliedPointInTime_SoClockRollbackCannotReviveExpiredLicense()
    {
        var state = new LicenseState("hash", LicenseType.Yearly, FixedExpiry);

        // The validator passes max(now, LastValidatedAt); a time past expiry keeps it expired
        // even if the machine clock is later moved back before the expiry date.
        state.RefreshStatus(FixedExpiry.AddDays(1));

        state.Status.Should().Be(LicenseStatus.Expired);
    }

    [Fact]
    public void RecordValidatedAt_NeverMovesBackwards()
    {
        var state = new LicenseState("hash", LicenseType.Yearly, FixedExpiry);
        var later = DateTime.UtcNow.AddDays(10);

        state.RecordValidatedAt(later);
        state.RecordValidatedAt(later.AddDays(-30));

        state.LastValidatedAt.Should().Be(later);
    }

    [Fact]
    public void RestoreFromSignedPayload_RevertsEditedExpiry()
    {
        var state = new LicenseState("hash", LicenseType.Yearly, new DateTime(2035, 12, 31));

        state.RestoreFromSignedPayload(LicenseType.Yearly, FixedExpiry, maxUsers: 0);

        state.ExpiresAt.Should().Be(FixedExpiry);
    }

    [Fact]
    public void Replace_KeepsExpiryFromLicense_NotFromActivationTime()
    {
        var state = new LicenseState("old", LicenseType.Yearly, FixedExpiry.AddYears(-1));

        state.Replace("new", LicenseType.Yearly, FixedExpiry, licenseBlob: [1, 2, 3]);

        state.ExpiresAt.Should().Be(FixedExpiry);
        state.LicenseBlob.Should().Equal(1, 2, 3);
        state.Status.Should().Be(LicenseStatus.Active);
    }
}
