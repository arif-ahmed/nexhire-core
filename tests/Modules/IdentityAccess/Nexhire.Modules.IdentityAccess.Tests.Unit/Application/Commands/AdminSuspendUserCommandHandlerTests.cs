using FluentAssertions;
using NSubstitute;
using Nexhire.Modules.IdentityAccess.Application.Accounts.Commands.AdminSuspendUser;
using Nexhire.Modules.IdentityAccess.Domain.Domain;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Repositories;
using Nexhire.Modules.IdentityAccess.Domain.Domain.Services;
using Nexhire.Modules.IdentityAccess.Domain.ValueObjects;

namespace Nexhire.Modules.IdentityAccess.Tests.Unit.Application.Commands;

public class AdminSuspendUserCommandHandlerTests
{
    private readonly IUserAccountRepository    _repo    = Substitute.For<IUserAccountRepository>();
    private readonly IAdminActionLogRepository _logRepo = Substitute.For<IAdminActionLogRepository>();

    private AdminSuspendUserCommandHandler CreateHandler() =>
        new(_repo, _logRepo);

    private static UserAccount MakeActiveAccount()
    {
        var perms = PermissionResolver.Resolve(UserRole.JobSeeker, []);
        var account = UserAccount.Provision(
            EmailAddress.Create("target@example.com").Value,
            MobileNumber.Create("+8801700000001").Value,
            PasswordHash.Create("$argon2id$h").Value,
            UserRole.JobSeeker, perms);
        account.Activate();
        account.ClearDomainEvents();
        return account;
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Suspend_Account_And_Write_AdminActionLog()
    {
        var account = MakeActiveAccount();
        var adminId = Guid.NewGuid();
        var cmd = new AdminSuspendUserCommand(adminId, account.Id.Value, "Policy violation");

        _repo.GetByIdAsync(new UserAccountId(account.Id.Value), Arg.Any<CancellationToken>())
            .Returns(account);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        account.Status.Should().Be(AccountStatus.Suspended);
        await _logRepo.Received(1).AddAsync(
            Arg.Is<AdminActionLog>(log =>
                log.ActionType == AdminActionType.Suspended &&
                log.TargetUserId == account.Id.Value &&
                log.AdminUserId == adminId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Persist_Both_Account_Change_And_Log_In_Same_SaveChanges_Call()
    {
        var account = MakeActiveAccount();
        var cmd = new AdminSuspendUserCommand(Guid.NewGuid(), account.Id.Value, "reason");

        _repo.GetByIdAsync(new UserAccountId(account.Id.Value), Arg.Any<CancellationToken>())
            .Returns(account);
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        // SaveChanges is called AFTER both the aggregate change and the log are staged
        // Verify log was added before SaveChanges
        Received.InOrder(() =>
        {
            _logRepo.AddAsync(Arg.Any<AdminActionLog>(), Arg.Any<CancellationToken>());
            _repo.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    // ── Account not found ──────────────────────────────────────────────────────

    [Fact]
    public async Task Should_Fail_With_NOT_FOUND_When_Target_Account_Missing()
    {
        _repo.GetByIdAsync(Arg.Any<UserAccountId>(), Arg.Any<CancellationToken>())
            .Returns((UserAccount?)null);
        var cmd = new AdminSuspendUserCommand(Guid.NewGuid(), Guid.NewGuid(), "reason");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-NOT-FOUND");
        await _logRepo.DidNotReceive().AddAsync(Arg.Any<AdminActionLog>(), Arg.Any<CancellationToken>());
    }

    // ── Suspend fails (e.g. already suspended) ─────────────────────────────────

    [Fact]
    public async Task Should_Not_Write_AdminActionLog_When_Suspend_Fails()
    {
        var account = MakeActiveAccount();
        account.Suspend("already suspended"); // pre-suspend so next Suspend fails
        var cmd = new AdminSuspendUserCommand(Guid.NewGuid(), account.Id.Value, "another reason");

        _repo.GetByIdAsync(new UserAccountId(account.Id.Value), Arg.Any<CancellationToken>())
            .Returns(account);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue("can't suspend an already-suspended account");
        await _logRepo.DidNotReceive().AddAsync(Arg.Any<AdminActionLog>(), Arg.Any<CancellationToken>());
    }
}
