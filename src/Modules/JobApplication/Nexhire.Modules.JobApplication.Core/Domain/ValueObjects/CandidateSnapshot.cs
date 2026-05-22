using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobApplication.Core.Domain.ValueObjects;

public class CandidateSnapshot : ValueObject
{
    public string FullName { get; }
    public string Email { get; }
    public string Mobile { get; }
    public string CurrentLocation { get; }
    public string EducationSummary { get; }
    public string ExperienceSummary { get; }
    public IReadOnlyList<string> Skills { get; }
    public bool IsLevel2Complete { get; }
    public DateTime CapturedOnUtc { get; }

    [JsonConstructor]
    private CandidateSnapshot(
        string fullName,
        string email,
        string mobile,
        string currentLocation,
        string educationSummary,
        string experienceSummary,
        IReadOnlyList<string> skills,
        bool isLevel2Complete,
        DateTime capturedOnUtc)
    {
        FullName = fullName;
        Email = email;
        Mobile = mobile;
        CurrentLocation = currentLocation;
        EducationSummary = educationSummary;
        ExperienceSummary = experienceSummary;
        Skills = skills;
        IsLevel2Complete = isLevel2Complete;
        CapturedOnUtc = capturedOnUtc;
    }

    public static Result<CandidateSnapshot> Create(
        string fullName,
        string email,
        string mobile,
        string currentLocation,
        string educationSummary,
        string experienceSummary,
        IReadOnlyList<string>? skills,
        bool isLevel2Complete,
        DateTime capturedOnUtc)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result.Failure<CandidateSnapshot>(new Error("CandidateSnapshot.EmptyFullName", "Full name cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<CandidateSnapshot>(new Error("CandidateSnapshot.EmptyEmail", "Email cannot be empty."));
        }

        if (string.IsNullOrWhiteSpace(mobile))
        {
            return Result.Failure<CandidateSnapshot>(new Error("CandidateSnapshot.EmptyMobile", "Mobile number cannot be empty."));
        }

        if (!isLevel2Complete)
        {
            return Result.Failure<CandidateSnapshot>(new Error("CandidateSnapshot.ProfileIncomplete", "Candidate profile Level 2 must be complete."));
        }

        var skillsList = skills ?? new List<string>();

        return new CandidateSnapshot(
            fullName.Trim(),
            email.Trim().ToLowerInvariant(),
            mobile.Trim(),
            currentLocation?.Trim() ?? string.Empty,
            educationSummary?.Trim() ?? string.Empty,
            experienceSummary?.Trim() ?? string.Empty,
            skillsList,
            isLevel2Complete,
            capturedOnUtc);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullName;
        yield return Email;
        yield return Mobile;
        yield return CurrentLocation;
        yield return EducationSummary;
        yield return ExperienceSummary;
        yield return IsLevel2Complete;
        yield return CapturedOnUtc;
        
        foreach (var skill in Skills)
        {
            yield return skill;
        }
    }
}
