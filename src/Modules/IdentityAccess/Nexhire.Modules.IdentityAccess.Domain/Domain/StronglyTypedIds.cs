namespace Nexhire.Modules.IdentityAccess.Domain.Domain;

public record UserAccountId(Guid Value);
public record OtpChallengeId(Guid Value);
public record SessionId(Guid Value);
public record BackupCodeId(Guid Value);
public record TrustedDeviceId(Guid Value);
public record PasswordResetTokenId(Guid Value);
