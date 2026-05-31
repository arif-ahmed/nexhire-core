using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.EmployerProfiles.Application.EmployerProfiles.Commands.RemoveCompanyImage;

public record RemoveCompanyImageCommand(Guid UserId, Guid CompanyImageId) : ICommand;
