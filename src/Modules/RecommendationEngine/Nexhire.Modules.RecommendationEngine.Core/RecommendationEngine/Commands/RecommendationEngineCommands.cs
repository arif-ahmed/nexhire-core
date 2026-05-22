using System;
using System.Collections.Generic;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;

namespace Nexhire.Modules.RecommendationEngine.Core.RecommendationEngine.Commands;

public sealed record UpdateMatchThresholdCommand(int NewThreshold, Guid AdminId) : ICommand;

public sealed record UpdateMatchingWeightsCommand(
    decimal Skill,
    decimal Education,
    decimal Training,
    decimal Location,
    decimal Experience,
    decimal Salary,
    Guid AdminId) : ICommand;

public sealed record CreateTalentPoolCommand(
    Guid EmployerId,
    Guid RecruiterId,
    string Name,
    string? Description,
    List<string>? Tags,
    bool IsShared) : ICommand<Guid>;

public sealed record AddCandidateToTalentPoolCommand(
    Guid TalentPoolId,
    Guid JobSeekerId,
    Guid RecruiterId,
    string? Note) : ICommand;

public sealed record RemoveCandidateFromTalentPoolCommand(
    Guid TalentPoolId,
    Guid JobSeekerId) : ICommand;

public sealed record SetQualificationThresholdCommand(
    Guid JobPostingId,
    Guid RecruiterId,
    int MinOverallMatch,
    int MinSkillMatch,
    EducationLevel MinEducationLevel,
    decimal MinExperienceYears,
    List<string> RequiredCertifications) : ICommand;

public sealed record RecordRecommendationFeedbackCommand(
    Guid JobSeekerId,
    Guid JobPostingId,
    FeedbackSignal Signal) : ICommand;

public sealed record RefreshCandidateShortlistCommand(
    Guid JobPostingId,
    Guid RecruiterId) : ICommand;

public sealed record SetPerPostingThresholdCommand(
    Guid PostingId,
    int? Percent,
    Guid ChangedBy) : ICommand;

public sealed record CreateWeightVariantCommand(
    string Version,
    decimal Skill,
    decimal Education,
    decimal Training,
    decimal Location,
    decimal Experience,
    decimal Salary,
    string VariantId,
    int AllocationPercent,
    Guid CreatedBy) : ICommand;

public sealed record ActivateWeightProfileCommand(
    string Version) : ICommand;

public sealed record RollbackWeightProfileCommand(
    string TargetVersion) : ICommand;

public sealed record SetShortlistSizeCommand(
    Guid PostingId,
    int Size) : ICommand;

public sealed record UpdateTalentPoolCommand(
    Guid PoolId,
    string? Name,
    string? Description,
    List<string>? AssociatedSkills,
    bool? IsShared) : ICommand;

public sealed record UpdateTalentPoolCandidateNoteCommand(
    Guid PoolId,
    Guid JobSeekerId,
    string? Note) : ICommand;
