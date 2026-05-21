using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Core.EmployerProfiles.Commands.RemoveCompanyImage;

public record RemoveCompanyImageCommand(Guid UserId, Guid CompanyImageId) : ICommand;
