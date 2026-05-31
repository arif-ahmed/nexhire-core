namespace Nexhire.Modules.IdentityAccess.Domain.Domain.Services;

public static class PermissionResolver
{
    public static IReadOnlyList<string> Resolve(UserRole role, IReadOnlyList<string> explicitGrants)
    {
        var permissions = new HashSet<string>(explicitGrants);

        switch (role)
        {
            case UserRole.JobSeeker:
                permissions.Add("profile:self");
                permissions.Add("applications:self");
                permissions.Add("search:read");
                break;
            case UserRole.Employer:
                permissions.Add("employer:self");
                permissions.Add("jobs:write");
                permissions.Add("applications:read");
                permissions.Add("candidates:read");
                break;
            case UserRole.ThirdPartyPortal:
                permissions.Add("integrations:read");
                permissions.Add("jobs:write");
                break;
            case UserRole.MoLAdministrator:
                permissions.Add("users:manage");
                permissions.Add("jobs:moderate");
                permissions.Add("taxonomy:manage");
                permissions.Add("reports:read");
                
                // Plus all of the above
                permissions.Add("profile:self");
                permissions.Add("applications:self");
                permissions.Add("search:read");
                
                permissions.Add("employer:self");
                permissions.Add("jobs:write");
                permissions.Add("applications:read");
                permissions.Add("candidates:read");
                
                permissions.Add("integrations:read");
                break;
        }

        return permissions.ToList().AsReadOnly();
    }
}
