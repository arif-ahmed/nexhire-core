using System;
using System.Collections.Generic;
using System.Linq;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class TalentPool : AggregateRoot<TalentPoolId>
{
    private readonly List<TalentPoolCandidate> _members = new();

    public Guid EmployerId { get; private set; }
    public Guid RecruiterId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public List<string> Tags { get; private set; } = new();
    public bool IsShared { get; private set; }
    public IReadOnlyCollection<TalentPoolCandidate> Members => _members.AsReadOnly();

    private TalentPool() : base(TalentPoolId.New()) { }

    private TalentPool(
        TalentPoolId id,
        Guid employerId,
        Guid recruiterId,
        string name,
        string? description,
        List<string>? tags,
        bool isShared) : base(id)
    {
        EmployerId = employerId;
        RecruiterId = recruiterId;
        Name = name;
        Description = description;
        Tags = tags ?? new List<string>();
        IsShared = isShared;
    }

    public static TalentPool Create(
        Guid employerId,
        Guid recruiterId,
        string name,
        string? description,
        List<string>? tags,
        bool isShared)
    {
        return new TalentPool(
            TalentPoolId.New(),
            employerId,
            recruiterId,
            name,
            description,
            tags,
            isShared);
    }

    public Result AddCandidate(Guid jobSeekerId, Guid recruiterId, string? note)
    {
        var existing = _members.FirstOrDefault(m => m.JobSeekerId == jobSeekerId);

        if (existing != null)
        {
            if (existing.IsActive)
            {
                return Result.Failure(new Error("E-POOL-DUPLICATE-MEMBER", "Candidate is already active in this talent pool."));
            }

            // Reactivate candidate
            existing.Reactivate(recruiterId, note);
            return Result.Success();
        }

        var candidate = new TalentPoolCandidate(jobSeekerId, recruiterId, note);
        _members.Add(candidate);

        return Result.Success();
    }

    public Result RemoveCandidate(Guid jobSeekerId)
    {
        var candidate = _members.FirstOrDefault(m => m.JobSeekerId == jobSeekerId && m.IsActive);
        if (candidate == null)
        {
            return Result.Failure(new Error("E-POOL-MEMBER-NOT-FOUND", "Active candidate not found in this talent pool."));
        }

        candidate.Deactivate();
        return Result.Success();
    }

    public void Rename(string name) => Name = name;
    public void UpdateDescription(string? description) => Description = description;
    public void SetAssociatedSkills(List<string> skills) => Tags = skills;
    public void SetShared(bool isShared) => IsShared = isShared;

    public void UpdateCandidateNote(Guid jobSeekerId, string? note)
    {
        var candidate = _members.FirstOrDefault(m => m.JobSeekerId == jobSeekerId && m.IsActive);
        if (candidate != null)
        {
            candidate.UpdateNote(note);
        }
    }
}

public sealed class TalentPoolCandidate : Entity<Guid>
{
    public Guid JobSeekerId { get; private set; }
    public Guid AddedByRecruiterId { get; private set; }
    public string? Note { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime AddedAtUtc { get; private set; }
    public DateTime? RemovedAtUtc { get; private set; }

    internal TalentPoolCandidate(Guid jobSeekerId, Guid addedByRecruiterId, string? note)
        : base(Guid.NewGuid())
    {
        JobSeekerId = jobSeekerId;
        AddedByRecruiterId = addedByRecruiterId;
        Note = note;
        IsActive = true;
        AddedAtUtc = DateTime.UtcNow;
    }

    internal void Reactivate(Guid recruiterId, string? note)
    {
        IsActive = true;
        AddedByRecruiterId = recruiterId;
        Note = note;
        AddedAtUtc = DateTime.UtcNow;
        RemovedAtUtc = null;
    }

    internal void Deactivate()
    {
        IsActive = false;
        RemovedAtUtc = DateTime.UtcNow;
    }

    internal void UpdateNote(string? note) => Note = note;
}
