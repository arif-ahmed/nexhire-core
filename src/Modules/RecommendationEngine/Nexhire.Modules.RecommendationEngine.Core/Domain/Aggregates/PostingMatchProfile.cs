using System;
using System.Collections.Generic;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Events;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;

public sealed class PostingMatchProfile : AggregateRoot<PostingMatchProfileId>
{
    private readonly List<SkillRequirement> _requiredSkills = new();

    public Guid JobPostingId { get; private set; }
    public Guid EmployerId { get; private set; }
    public IReadOnlyCollection<SkillRequirement> RequiredSkills => _requiredSkills.AsReadOnly();
    public EducationLevel RequiredEducationLevel { get; private set; }
    public decimal RequiredExperienceYears { get; private set; }
    public GeoLocation? Location { get; private set; }
    public SalaryRange? SalaryRange { get; private set; }
    public PostingMatchStatus Status { get; private set; }
    public int? PerPostingThresholdOverride { get; private set; }
    public NlpExtractionStatus NlpStatus { get; private set; }

    private PostingMatchProfile() : base(PostingMatchProfileId.New()) { }

    private PostingMatchProfile(
        PostingMatchProfileId id,
        Guid jobPostingId,
        Guid employerId,
        List<SkillRequirement> requiredSkills,
        EducationLevel requiredEducationLevel,
        decimal requiredExperienceYears,
        GeoLocation? location,
        SalaryRange? salaryRange,
        PostingMatchStatus status,
        NlpExtractionStatus nlpStatus,
        int? perPostingThresholdOverride) : base(id)
    {
        JobPostingId = jobPostingId;
        EmployerId = employerId;
        _requiredSkills = requiredSkills ?? new List<SkillRequirement>();
        RequiredEducationLevel = requiredEducationLevel;
        RequiredExperienceYears = requiredExperienceYears;
        Location = location;
        SalaryRange = salaryRange;
        Status = status;
        NlpStatus = nlpStatus;
        PerPostingThresholdOverride = perPostingThresholdOverride;
    }

    public static PostingMatchProfile Create(
        Guid jobPostingId,
        Guid employerId,
        List<SkillRequirement> requiredSkills,
        EducationLevel requiredEducationLevel,
        decimal requiredExperienceYears,
        GeoLocation? location,
        SalaryRange? salaryRange,
        PostingMatchStatus status)
    {
        return new PostingMatchProfile(
            PostingMatchProfileId.New(),
            jobPostingId,
            employerId,
            requiredSkills,
            requiredEducationLevel,
            requiredExperienceYears,
            location,
            salaryRange,
            status,
            NlpExtractionStatus.Pending,
            perPostingThresholdOverride: null);
    }

    public void ApplyPostingUpdated(
        List<SkillRequirement> requiredSkills,
        EducationLevel requiredEducationLevel,
        decimal requiredExperienceYears,
        GeoLocation? location,
        SalaryRange? salaryRange)
    {
        _requiredSkills.Clear();
        if (requiredSkills != null) _requiredSkills.AddRange(requiredSkills);

        RequiredEducationLevel = requiredEducationLevel;
        RequiredExperienceYears = requiredExperienceYears;
        Location = location;
        SalaryRange = salaryRange;

        RaiseDomainEvent(new PostingMatchInputChanged(JobPostingId, DateTime.UtcNow));
    }

    public void ApplyNlpExtraction(List<SkillRequirement> extractedSkills)
    {
        NlpStatus = NlpExtractionStatus.Extracted;
        
        // Merge or replace extracted skills
        if (extractedSkills != null && extractedSkills.Count > 0)
        {
            foreach (var ext in extractedSkills)
            {
                var existing = _requiredSkills.Find(s => s.TaxonomyCode == ext.TaxonomyCode);
                if (existing != null)
                {
                    _requiredSkills.Remove(existing);
                }
                _requiredSkills.Add(ext);
            }
        }

        RaiseDomainEvent(new PostingMatchInputChanged(JobPostingId, DateTime.UtcNow));
    }

    public void RecordNlpExtractionFailure()
    {
        NlpStatus = NlpExtractionStatus.Failed;
    }

    public void Deactivate()
    {
        Status = PostingMatchStatus.Inactive;
        RaiseDomainEvent(new PostingMatchInputChanged(JobPostingId, DateTime.UtcNow));
    }

    public void SetPerPostingThresholdOverride(int? thresholdOverride)
    {
        PerPostingThresholdOverride = thresholdOverride;
    }
}
