using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexhire.Modules.IdentityAccess.Application.Ports;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Infrastructure.Persistence;

public static class IdentityAccessSeedData
{
    public static async Task SeedAsync(IdentityAccessDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.UserAccounts.AnyAsync()) return;

        // Admin
        var adminAccount = CreateAccount(
            "admin@nexhire.gov.bd", "+8801711111111", "Admin@Password2025!",
            UserRole.MoLAdministrator, passwordHasher);
        adminAccount.Activate(); // Promote past PendingActivation
        context.UserAccounts.Add(adminAccount);

        // Test Employer
        var employerAccount = CreateAccount(
            "employer@nexhire.com", "+8801711111112", "Employer@Password2025!",
            UserRole.Employer, passwordHasher);
        employerAccount.Activate();
        context.UserAccounts.Add(employerAccount);

        // Test JobSeeker
        var seekerAccount = CreateAccount(
            "seeker@nexhire.com", "+8801711111113", "Seeker@Password2025!",
            UserRole.JobSeeker, passwordHasher);
        seekerAccount.Activate();
        context.UserAccounts.Add(seekerAccount);

        // Pending Employer (PendingActivation — login will get E-LOGIN-ACCOUNT-NOT-ACTIVATED)
        var pendingAccount = CreateAccount(
            "pending@nexhire.com", "+8801711111114", "Pending@Password2025!",
            UserRole.Employer, passwordHasher);
        // NOT activated — stays PendingActivation
        context.UserAccounts.Add(pendingAccount);

        // Suspended User
        var suspendedAccount = CreateAccount(
            "suspended@nexhire.com", "+8801711111115", "Suspended@Password2025!",
            UserRole.JobSeeker, passwordHasher);
        suspendedAccount.Activate();
        suspendedAccount.Suspend("Policy violation — multiple failed login attempts");
        context.UserAccounts.Add(suspendedAccount);

        // Deactivated User
        var deactivatedAccount = CreateAccount(
            "deactivated@nexhire.com", "+8801711111116", "Deactivated@Password2025!",
            UserRole.JobSeeker, passwordHasher);
        deactivatedAccount.Activate();
        deactivatedAccount.Deactivate();
        context.UserAccounts.Add(deactivatedAccount);

        // Third-Party Portal
        var portalAccount = CreateAccount(
            "portal@external.com", "+8801711111117", "Portal@Password2025!",
            UserRole.ThirdPartyPortal, passwordHasher);
        portalAccount.Activate();
        context.UserAccounts.Add(portalAccount);

        await context.SaveChangesAsync();
    }

    private static UserAccount CreateAccount(
        string email, string mobile, string rawPassword,
        UserRole role, IPasswordHasher hasher)
    {
        var emailVO = EmailAddress.Create(email).Value;
        var mobileVO = MobileNumber.Create(mobile, "+880").Value;
        var rawVO = RawPassword.Create(rawPassword).Value;
        var passwordHash = hasher.Hash(rawVO);
        var permissions = PermissionResolver.Resolve(role, Array.Empty<string>());

        return UserAccount.Provision(emailVO, mobileVO, passwordHash, role, permissions);
    }
}

public static class IdentityAccessSeedExtensions
{
    public static async Task SeedIdentityAccessDataAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityAccessDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await IdentityAccessSeedData.SeedAsync(context, hasher);
    }
}
