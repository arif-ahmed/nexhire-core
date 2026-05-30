namespace Nexhire.Modules.IdentityAccess.Application.DTOs;

public record UserDto(Guid Id, string Email, string FirstName, string LastName, DateTime CreatedAtUtc);
