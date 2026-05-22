using System;
using System.Collections.Generic;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.JobApplication.Core.Domain.Events;

// 1. Integration Events (Published through outbox)

public record JobBookmarkedIntegrationEvent(
    Guid EventId,
    Guid JobSeekerId,
    Guid JobPostingId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record JobUnbookmarkedIntegrationEvent(
    Guid EventId,
    Guid JobSeekerId,
    Guid JobPostingId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record CompactSnapshotFingerprint(
    string FullName,
    string Email,
    bool IsLevel2Complete,
    Guid ResumeDocumentId,
    List<string> Skills);

public record ApplicationSubmittedIntegrationEvent(
    Guid EventId,
    Guid ApplicationId,
    Guid JobSeekerId,
    Guid JobPostingId,
    Guid EmployerId,
    CompactSnapshotFingerprint Snapshot,
    int? MatchScoreAtApply,
    DateTime AppliedOnUtc,
    DateTime OccurredOnUtc) : IDomainEvent;

public record ApplicationViewedIntegrationEvent(
    Guid EventId,
    Guid ApplicationId,
    Guid EmployerId,
    DateTime OccurredOnUtc) : IDomainEvent;

public record ApplicationStatusChangedIntegrationEvent(
    Guid EventId,
    Guid ApplicationId,
    string FromStatus,
    string ToStatus,
    string ByRole,
    DateTime OccurredOnUtc) : IDomainEvent;

public record ApplicationWithdrawnIntegrationEvent(
    Guid EventId,
    Guid ApplicationId,
    Guid JobSeekerId,
    string WithdrawalReasonCode,
    DateTime OccurredOnUtc) : IDomainEvent;

// 2. Internal Domain Events (Not published outside module)

public record ApplicationRejectedDomainEvent(
    Guid EventId,
    Guid ApplicationId,
    DateTime OccurredOnUtc) : IDomainEvent;
