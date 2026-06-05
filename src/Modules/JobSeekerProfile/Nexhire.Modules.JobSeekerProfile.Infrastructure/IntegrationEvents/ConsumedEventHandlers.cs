using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexhire.Modules.AdministratorsConfiguration.Core.Contracts.IntegrationEvents;
using Nexhire.Modules.ExternalJobSync.Core.Domain.Events;
using Nexhire.Modules.IdentityAccess.Contracts.Events;
using Nexhire.Modules.JobSeekerProfile.Infrastructure.Persistence;
using Nexhire.Shared.Infrastructure.Messaging;

namespace Nexhire.Modules.JobSeekerProfile.Infrastructure.IntegrationEvents;

public sealed class UserAccountActivatedConsumer : INotificationHandler<UserAccountActivatedIntegrationEvent>
{
    private readonly JobSeekerProfileDbContext _dbContext;
    private readonly ILogger<UserAccountActivatedConsumer> _logger;

    public UserAccountActivatedConsumer(JobSeekerProfileDbContext dbContext, ILogger<UserAccountActivatedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(UserAccountActivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _dbContext.JobSeekerProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("[UserAccountActivated] No JobSeekerProfile found for UserId={UserId}", notification.UserId);
            return;
        }

        var result = profile.Activate();
        if (result.IsFailure)
        {
            _logger.LogWarning("[UserAccountActivated] Failed to activate profile {ProfileId}: {Error}", profile.Id, result.Error);
        }

        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountActivatedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[UserAccountActivated] Profile {ProfileId} activated for UserId={UserId}", profile.Id, notification.UserId);
    }
}

public sealed class AccountDeactivatedConsumer : INotificationHandler<AccountDeactivatedIntegrationEvent>
{
    private readonly JobSeekerProfileDbContext _dbContext;
    private readonly ILogger<AccountDeactivatedConsumer> _logger;

    public AccountDeactivatedConsumer(JobSeekerProfileDbContext dbContext, ILogger<AccountDeactivatedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(AccountDeactivatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _dbContext.JobSeekerProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("[AccountDeactivated] No JobSeekerProfile found for UserId={UserId}", notification.UserId);
            return;
        }

        var result = profile.Deactivate();
        if (result.IsFailure)
        {
            _logger.LogWarning("[AccountDeactivated] Failed to deactivate profile {ProfileId}: {Error}", profile.Id, result.Error);
        }

        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(AccountDeactivatedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[AccountDeactivated] Profile {ProfileId} deactivated for UserId={UserId}", profile.Id, notification.UserId);
    }
}

public sealed class UserAccountSuspendedConsumer : INotificationHandler<UserAccountSuspendedIntegrationEvent>
{
    private readonly JobSeekerProfileDbContext _dbContext;
    private readonly ILogger<UserAccountSuspendedConsumer> _logger;

    public UserAccountSuspendedConsumer(JobSeekerProfileDbContext dbContext, ILogger<UserAccountSuspendedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(UserAccountSuspendedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _dbContext.JobSeekerProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("[UserAccountSuspended] No JobSeekerProfile found for UserId={UserId}", notification.UserId);
            return;
        }

        var result = profile.Deactivate();
        if (result.IsFailure)
        {
            _logger.LogWarning("[UserAccountSuspended] Failed to deactivate profile {ProfileId}: {Error}", profile.Id, result.Error);
        }

        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountSuspendedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[UserAccountSuspended] Profile {ProfileId} deactivated for UserId={UserId} (Reason: {Reason})", profile.Id, notification.UserId, notification.Reason);
    }
}

public sealed class UserAccountReinstatedConsumer : INotificationHandler<UserAccountReinstatedIntegrationEvent>
{
    private readonly JobSeekerProfileDbContext _dbContext;
    private readonly ILogger<UserAccountReinstatedConsumer> _logger;

    public UserAccountReinstatedConsumer(JobSeekerProfileDbContext dbContext, ILogger<UserAccountReinstatedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(UserAccountReinstatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _dbContext.JobSeekerProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("[UserAccountReinstated] No JobSeekerProfile found for UserId={UserId}", notification.UserId);
            return;
        }

        var result = profile.Reactivate();
        if (result.IsFailure)
        {
            _logger.LogWarning("[UserAccountReinstated] Failed to reactivate profile {ProfileId}: {Error}", profile.Id, result.Error);
        }

        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(UserAccountReinstatedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[UserAccountReinstated] Profile {ProfileId} reactivated for UserId={UserId}", profile.Id, notification.UserId);
    }
}

// Uses ExternalJobSync (BC-8) definition: Nexhire.Modules.ExternalJobSync.Core.Domain.Events.IdentityVerifiedByGovernmentIntegrationEvent
public sealed class IdentityVerifiedByGovernmentConsumer : INotificationHandler<Nexhire.Modules.ExternalJobSync.Core.Domain.Events.IdentityVerifiedByGovernmentIntegrationEvent>
{
    private readonly JobSeekerProfileDbContext _dbContext;
    private readonly ILogger<IdentityVerifiedByGovernmentConsumer> _logger;

    public IdentityVerifiedByGovernmentConsumer(JobSeekerProfileDbContext dbContext, ILogger<IdentityVerifiedByGovernmentConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(Nexhire.Modules.ExternalJobSync.Core.Domain.Events.IdentityVerifiedByGovernmentIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _dbContext.JobSeekerProfiles
            .FirstOrDefaultAsync(p => p.UserId == notification.UserId, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("[IdentityVerifiedByGovernment] No JobSeekerProfile found for UserId={UserId}", notification.UserId);
            return;
        }

        profile.ApplyIdentityVerified();

        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, "IdentityVerifiedByGovernmentIntegrationEvent", DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[IdentityVerifiedByGovernment] Profile {ProfileId} identity verified via {Registry}", profile.Id, notification.Registry);
    }
}

public sealed class EducationVerifiedConsumer : INotificationHandler<EducationVerifiedIntegrationEvent>
{
    private readonly JobSeekerProfileDbContext _dbContext;
    private readonly ILogger<EducationVerifiedConsumer> _logger;

    public EducationVerifiedConsumer(JobSeekerProfileDbContext dbContext, ILogger<EducationVerifiedConsumer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(EducationVerifiedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        if (await _dbContext.InboxMessages.AnyAsync(m => m.Id == notification.EventId, cancellationToken))
            return;

        var profile = await _dbContext.JobSeekerProfiles
            .FirstOrDefaultAsync(p => p.Id == notification.JobSeekerProfileId, cancellationToken);

        if (profile is null)
        {
            _logger.LogWarning("[EducationVerified] No JobSeekerProfile found for ProfileId={ProfileId}", notification.JobSeekerProfileId);
            return;
        }

        profile.ApplyEducationVerified();

        _dbContext.InboxMessages.Add(new InboxMessage(notification.EventId, nameof(EducationVerifiedIntegrationEvent), DateTime.UtcNow));
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[EducationVerified] Profile {ProfileId} education verified", profile.Id);
    }
}

public sealed class TaxonomyUpdatedConsumer : INotificationHandler<TaxonomyUpdatedIntegrationEvent>
{
    private readonly ILogger<TaxonomyUpdatedConsumer> _logger;

    public TaxonomyUpdatedConsumer(ILogger<TaxonomyUpdatedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Handle(TaxonomyUpdatedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Invalidate any locally cached taxonomy data (stub cache).
        // When a real taxonomy API is in place (BC-11 integration), this handler
        // will clear the in-memory or distributed cache for taxonomy lookups.
        _logger.LogInformation(
            "[TaxonomyUpdated] Taxonomy {TaxonomyId} ({Kind}) updated to v{Version}: {ChangeSummary} — cache invalidated",
            notification.TaxonomyId, notification.Kind, notification.Version, notification.ChangeSummary);

        return Task.CompletedTask;
    }
}
