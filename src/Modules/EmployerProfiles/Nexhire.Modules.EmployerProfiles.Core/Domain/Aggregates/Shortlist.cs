using Nexhire.Modules.EmployerProfiles.Core.Domain.Events;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.EmployerProfiles.Core.Domain.Aggregates;

public class Shortlist : AggregateRoot<Guid>
{
    private readonly List<ShortlistMember> _members = new();

    public Guid EmployerProfileId { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsDeleted { get; private set; }
    
    public IReadOnlyCollection<ShortlistMember> Members => _members.AsReadOnly();
    
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime UpdatedOnUtc { get; private set; }

    private Shortlist(Guid id, Guid employerProfileId, string name) : base(id)
    {
        EmployerProfileId = employerProfileId;
        Name = name;
        IsDeleted = false;
        CreatedOnUtc = DateTime.UtcNow;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    private Shortlist()
    {
        // Required by EF Core
    }

    public static Result<Shortlist> Create(Guid id, Guid employerProfileId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Shortlist>(new Error("Shortlist.EmptyName", "Shortlist name cannot be empty."));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            return Result.Failure<Shortlist>(new Error("Shortlist.NameTooLong", "Shortlist name must not exceed 100 characters."));
        }

        return Result.Success(new Shortlist(id, employerProfileId, trimmedName));
    }

    public Result Rename(string newName)
    {
        if (IsDeleted)
        {
            return Result.Failure(new Error("Shortlist.Deleted", "Cannot rename a deleted shortlist."));
        }

        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(new Error("Shortlist.EmptyName", "Shortlist name cannot be empty."));
        }

        var trimmedName = newName.Trim();
        if (trimmedName.Length > 100)
        {
            return Result.Failure(new Error("Shortlist.NameTooLong", "Shortlist name must not exceed 100 characters."));
        }

        Name = trimmedName;
        UpdatedOnUtc = DateTime.UtcNow;

        return Result.Success();
    }

    public Result AddCandidate(Guid candidateUserId, int? matchScore = null)
    {
        if (IsDeleted)
        {
            return Result.Failure(new Error("Shortlist.Deleted", "Cannot add candidates to a deleted shortlist."));
        }

        if (_members.Any(m => m.CandidateUserId == candidateUserId))
        {
            return Result.Failure(new Error("Shortlist.DuplicateCandidate", "Candidate is already present in this shortlist."));
        }

        var member = ShortlistMember.Create(Guid.NewGuid(), candidateUserId, matchScore);
        _members.Add(member);
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CandidateSavedToTalentPoolIntegrationEvent(
            Guid.NewGuid(),
            EmployerProfileId, // Will resolve to UserId in event mapping if required, or is passed directly.
            candidateUserId,
            Id,
            UpdatedOnUtc,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result RemoveCandidate(Guid shortlistMemberId)
    {
        if (IsDeleted)
        {
            return Result.Failure(new Error("Shortlist.Deleted", "Cannot remove candidates from a deleted shortlist."));
        }

        var member = _members.FirstOrDefault(m => m.Id == shortlistMemberId);
        if (member == null)
        {
            return Result.Failure(new Error("Shortlist.MemberNotFound", "Shortlist member not found."));
        }

        _members.Remove(member);
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new CandidateRemovedFromTalentPool(
            Guid.NewGuid(),
            Id,
            member.CandidateUserId,
            UpdatedOnUtc));

        return Result.Success();
    }

    public Result Delete()
    {
        if (IsDeleted)
        {
            return Result.Success(); // Idempotent
        }

        IsDeleted = true;
        UpdatedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ShortlistDeleted(
            Guid.NewGuid(),
            Id,
            UpdatedOnUtc));

        return Result.Success();
    }
}
