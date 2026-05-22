using System;
using System.Collections.Generic;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Events;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class SeekerMatchProfile : AggregateRoot<SeekerMatchProfileId>
{
    private readonly List<SkillRequirement> _skills = new();
    private readonly List<string> _trainingCredentials = new();

    public Guid JobSeekerId { get; private set; }
    public IReadOnlyCollection<SkillRequirement> Skills => _skills.AsReadOnly();
    public EducationLevel EducationLevel { get; private set; }
    public IReadOnlyCollection<string> TrainingCredentials => _trainingCredentials.AsReadOnly();
    public decimal TotalExperienceYears { get; private set; }
    public GeoLocation? Location { get; private set; }
    public SalaryRange? SalaryExpectation { get; private set; }
    public PrivacyLevel PrivacyLevel { get; private set; }
    public bool IsActive { get; private set; }

    private SeekerMatchProfile() : base(SeekerMatchProfileId.New()) { }

    private SeekerMatchProfile(
        SeekerMatchProfileId id,
        Guid jobSeekerId,
        List<SkillRequirement> skills,
        EducationLevel educationLevel,
        List<string> trainingCredentials,
        decimal totalExperienceYears,
        GeoLocation? location,
        SalaryRange? salaryExpectation,
        PrivacyLevel privacyLevel,
        bool isActive) : base(id)
    {
        JobSeekerId = jobSeekerId;
        _skills = skills ?? new List<SkillRequirement>();
        EducationLevel = educationLevel;
        _trainingCredentials = trainingCredentials ?? new List<string>();
        TotalExperienceYears = totalExperienceYears;
        Location = location;
        SalaryExpectation = salaryExpectation;
        PrivacyLevel = privacyLevel;
        IsActive = isActive;
    }

    public static SeekerMatchProfile Create(
        Guid jobSeekerId,
        List<SkillRequirement> skills,
        EducationLevel educationLevel,
        List<string> trainingCredentials,
        decimal totalExperienceYears,
        GeoLocation? location,
        SalaryRange? salaryExpectation,
        PrivacyLevel privacyLevel)
    {
        return new SeekerMatchProfile(
            SeekerMatchProfileId.New(),
            jobSeekerId,
            skills,
            educationLevel,
            trainingCredentials,
            totalExperienceYears,
            location,
            salaryExpectation,
            privacyLevel,
            isActive: true);
    }

    public void ApplySkillsUpdated(List<SkillRequirement> skills)
    {
        _skills.Clear();
        if (skills != null)
        {
            _skills.AddRange(skills);
        }
        RaiseDomainEvent(new SeekerMatchInputChanged(JobSeekerId, DateTime.UtcNow));
    }

    public void ApplyResumeParsed(
        List<SkillRequirement> skills,
        EducationLevel educationLevel,
        decimal totalExperienceYears,
        List<string> trainingCredentials)
    {
        _skills.Clear();
        if (skills != null) _skills.AddRange(skills);
        
        EducationLevel = educationLevel;
        TotalExperienceYears = totalExperienceYears;

        _trainingCredentials.Clear();
        if (trainingCredentials != null) _trainingCredentials.AddRange(trainingCredentials);

        RaiseDomainEvent(new SeekerMatchInputChanged(JobSeekerId, DateTime.UtcNow));
    }

    public void ApplyLevel2Completed(
        GeoLocation location,
        SalaryRange salaryExpectation)
    {
        Location = location;
        SalaryExpectation = salaryExpectation;
        RaiseDomainEvent(new SeekerMatchInputChanged(JobSeekerId, DateTime.UtcNow));
    }

    public void ApplyVisibilityChanged(PrivacyLevel privacyLevel)
    {
        if (PrivacyLevel != privacyLevel)
        {
            PrivacyLevel = privacyLevel;
            RaiseDomainEvent(new SeekerPrivacyChanged(JobSeekerId, PrivacyLevel, DateTime.UtcNow));
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            RaiseDomainEvent(new SeekerPrivacyChanged(JobSeekerId, PrivacyLevel, DateTime.UtcNow));
        }
    }

    public void Reactivate()
    {
        if (!IsActive)
        {
            IsActive = true;
            RaiseDomainEvent(new SeekerPrivacyChanged(JobSeekerId, PrivacyLevel, DateTime.UtcNow));
        }
    }
}
