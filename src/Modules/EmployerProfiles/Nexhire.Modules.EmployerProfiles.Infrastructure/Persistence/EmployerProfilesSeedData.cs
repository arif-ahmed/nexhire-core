using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.EmployerProfiles.Domain.Aggregates;
using Nexhire.Modules.EmployerProfiles.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Infrastructure.Persistence;

public static class EmployerProfilesSeedData
{
    /// <summary>
    /// Well-known seed UserIds (BC-1 identities) for test employer profiles.
    /// These match the test employer accounts created by IdentityAccessSeedData.
    /// </summary>
    public static readonly Guid VerifiedEmployerUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PendingEmployerUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid SuspendedEmployerUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid DeactivatedEmployerUserId = Guid.Parse("00000000-0000-0000-0000-000000000004");

    public static async Task SeedAsync(EmployerProfilesDbContext context)
    {
        if (await context.EmployerProfiles.AnyAsync()) return;

        var now = DateTime.UtcNow;

        // Verified employer — matches IdentityAccess seed "employer@nexhire.com"
        var verified = EmployerProfile.Register(
            Guid.NewGuid(),
            VerifiedEmployerUserId,
            CompanyName.Create("Nexhire Technologies Ltd.").Value,
            EmailAddress.Create("employer@nexhire.com").Value,
            MobileNumber.Create("+8801711111112").Value,
            CompanyIdentifier.Create("REG-VERIFIED-001").Value);
        verified.Activate();
        verified.CompleteLevel2(
            WebsiteUrl.Create("https://nexhire.com").Value,
            "Technology",
            CompanySize.Create("Large").Value,
            Address.Create("42 Software Avenue", null, "Dhaka", "Dhaka", "1212", "Bangladesh").Value,
            CompanyDescription.Create("Leading HR technology platform in Bangladesh.").Value);
        verified.BeginAutomaticVerification("gov-registry-ref-001");
        verified.RecordAutomaticVerificationPassed("auto-evidence-ref-001");
        context.EmployerProfiles.Add(verified);

        // Pending employer (PendingActivation — not yet activated) — matches IdentityAccess seed "pending@nexhire.com"
        var pending = EmployerProfile.Register(
            Guid.NewGuid(),
            PendingEmployerUserId,
            CompanyName.Create("Pending Startup Ltd.").Value,
            EmailAddress.Create("pending@nexhire.com").Value,
            MobileNumber.Create("+8801711111114").Value,
            CompanyIdentifier.Create("REG-PENDING-002").Value);
        // NOT activated — stays PendingActivation
        context.EmployerProfiles.Add(pending);

        // Suspended employer profile
        var suspended = EmployerProfile.Register(
            Guid.NewGuid(),
            SuspendedEmployerUserId,
            CompanyName.Create("Suspended Corp.").Value,
            EmailAddress.Create("suspended@nexhire.com").Value,
            MobileNumber.Create("+8801711111115").Value,
            CompanyIdentifier.Create("REG-SUSPENDED-003").Value);
        suspended.Activate();
        suspended.Suspend("Policy violation — incomplete documentation");
        context.EmployerProfiles.Add(suspended);

        // Deactivated employer profile
        var deactivated = EmployerProfile.Register(
            Guid.NewGuid(),
            DeactivatedEmployerUserId,
            CompanyName.Create("Deactivated Inc.").Value,
            EmailAddress.Create("deactivated@nexhire.com").Value,
            MobileNumber.Create("+8801711111116").Value,
            CompanyIdentifier.Create("REG-DEACTIVATED-004").Value);
        deactivated.Activate();
        deactivated.Deactivate();
        context.EmployerProfiles.Add(deactivated);

        await context.SaveChangesAsync();
    }
}

public static class EmployerProfilesSeedExtensions
{
    public static async Task SeedEmployerProfilesDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmployerProfilesDbContext>();
        await EmployerProfilesSeedData.SeedAsync(context);
    }
}
