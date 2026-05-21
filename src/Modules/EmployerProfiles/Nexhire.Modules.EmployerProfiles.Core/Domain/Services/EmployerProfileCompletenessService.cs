using Nexhire.Modules.EmployerProfiles.Core.Domain.ValueObjects;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Services;

public class EmployerProfileCompletenessService
{
    public static ProfileCompleteness Evaluate(
        CompanyName companyName,
        EmailAddress email,
        MobileNumber mobile,
        CompanyIdentifier companyIdentifier,
        WebsiteUrl? website,
        string? industry,
        CompanySize? companySize,
        Address? address,
        CompanyDescription? description)
    {
        // Level 1 is always complete if the core registration fields are present
        bool l1Complete = companyName != null && email != null && mobile != null && companyIdentifier != null;

        // Level 2 is complete only when all L2 fields are fully provided
        bool l2Complete = website != null && 
                          !string.IsNullOrWhiteSpace(industry) && 
                          companySize != null && 
                          address != null && 
                          description != null;

        return ProfileCompleteness.Create(l1Complete, l2Complete).Value;
    }
}
