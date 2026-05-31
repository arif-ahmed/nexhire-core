using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RegisterEmployer;

public record RegisterEmployerCommand(
    string Email,
    string Mobile,
    string Password,
    string CompanyName,
    string CompanyIdentifier) : ICommand<Guid>;
