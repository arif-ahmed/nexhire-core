using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Aggregates = Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence;

public static class JobSeekerProfileSeedData
{
    public static readonly Guid ActiveJobSeekerUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PendingJobSeekerUserId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid SuspendedJobSeekerUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid DeactivatedJobSeekerUserId = Guid.Parse("00000000-0000-0000-0000-000000000004");

    public static async Task SeedAsync(JobSeekerProfileDbContext context)
    {
        if (await context.JobSeekerProfiles.AnyAsync()) return;

        var now = DateTime.UtcNow;

        // Active job seeker — matches IdentityAccess seed "seeker@nexhire.com"
        var activeResult = Aggregates.JobSeekerProfile.Register(
            Guid.NewGuid(),
            ActiveJobSeekerUserId,
            PersonName.Create("Rahim", "Islam").Value,
            EmailAddress.Create("seeker@nexhire.com").Value,
            MobileNumber.Create("+8801711111113").Value,
            Gender.Male);
        var active = activeResult.Value;
        active.Activate();
        context.JobSeekerProfiles.Add(active);

        // Pending job seeker (PendingActivation)
        var pendingResult = Aggregates.JobSeekerProfile.Register(
            Guid.NewGuid(),
            PendingJobSeekerUserId,
            PersonName.Create("Fatima", "Begum").Value,
            EmailAddress.Create("pending@nexhire.com").Value,
            MobileNumber.Create("+8801711111114").Value,
            Gender.Female);
        context.JobSeekerProfiles.Add(pendingResult.Value);

        // Suspended job seeker
        var suspendedResult = Aggregates.JobSeekerProfile.Register(
            Guid.NewGuid(),
            SuspendedJobSeekerUserId,
            PersonName.Create("Kamal", "Hossain").Value,
            EmailAddress.Create("suspended@nexhire.com").Value,
            MobileNumber.Create("+8801711111115").Value,
            Gender.Male);
        var suspended = suspendedResult.Value;
        suspended.Activate();
        suspended.Deactivate();
        context.JobSeekerProfiles.Add(suspended);

        // Full profile job seeker (with education, experience, skills)
        var fullResult = Aggregates.JobSeekerProfile.Register(
            Guid.NewGuid(),
            DeactivatedJobSeekerUserId,
            PersonName.Create("Ayesha", "Khatun").Value,
            EmailAddress.Create("deactivated@nexhire.com").Value,
            MobileNumber.Create("+8801711111116").Value,
            Gender.Female);
        var full = fullResult.Value;
        full.Activate();
        context.JobSeekerProfiles.Add(full);

        await context.SaveChangesAsync();
    }
}

public static class JobSeekerProfileSeedExtensions
{
    public static async Task SeedJobSeekerProfileDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<JobSeekerProfileDbContext>();
        await JobSeekerProfileSeedData.SeedAsync(context);
    }
}
