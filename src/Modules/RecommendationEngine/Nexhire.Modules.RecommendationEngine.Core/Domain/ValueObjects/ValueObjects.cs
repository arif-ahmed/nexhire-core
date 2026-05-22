using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;

public sealed record FactorWeights
{
    public decimal Skill { get; private init; }
    public decimal Education { get; private init; }
    public decimal Training { get; private init; }
    public decimal Location { get; private init; }
    public decimal Experience { get; private init; }
    public decimal Salary { get; private init; }

    private FactorWeights(decimal skill, decimal education, decimal training, decimal location, decimal experience, decimal salary)
    {
        Skill = skill;
        Education = education;
        Training = training;
        Location = location;
        Experience = experience;
        Salary = salary;
    }

    public static Result<FactorWeights> Create(decimal skill, decimal education, decimal training, decimal location, decimal experience, decimal salary)
    {
        if (skill is < 0 or > 1 || education is < 0 or > 1 || training is < 0 or > 1 ||
            location is < 0 or > 1 || experience is < 0 or > 1 || salary is < 0 or > 1)
        {
            return Result.Failure<FactorWeights>(new Error("E-WEIGHTS-OUT-OF-RANGE", "Each weight must be between 0.0 and 1.0."));
        }

        var sum = skill + education + training + location + experience + salary;
        if (Math.Abs(sum - 1.0m) > 0.01m)
        {
            return Result.Failure<FactorWeights>(new Error("E-WEIGHTS-INVALID-SUM", "Weights must sum to 1.0 ± 0.01."));
        }

        return Result.Success(new FactorWeights(skill, education, training, location, experience, salary));
    }
}

public sealed record FactorScore
{
    public MatchFactor Factor { get; private init; }
    public int Score { get; private init; }

    private FactorScore(MatchFactor factor, int score)
    {
        Factor = factor;
        Score = score;
    }

    public static Result<FactorScore> Create(MatchFactor factor, int score)
    {
        if (score is < 0 or > 100)
        {
            return Result.Failure<FactorScore>(new Error("E-FACTOR-SCORE-OUT-OF-RANGE", "Factor score must be between 0 and 100."));
        }

        return Result.Success(new FactorScore(factor, score));
    }
}

public sealed record MatchBreakdown
{
    public IReadOnlyCollection<FactorScore> Scores { get; private init; }

    private MatchBreakdown(List<FactorScore> scores)
    {
        Scores = scores.AsReadOnly();
    }

    public static Result<MatchBreakdown> Create(IEnumerable<FactorScore> scores)
    {
        var list = scores?.ToList() ?? new List<FactorScore>();
        if (list.Count != 6)
        {
            return Result.Failure<MatchBreakdown>(new Error("E-BREAKDOWN-INCOMPLETE", "Match breakdown must contain exactly six factor scores."));
        }

        var factors = list.Select(s => s.Factor).Distinct().ToList();
        if (factors.Count != 6)
        {
            return Result.Failure<MatchBreakdown>(new Error("E-BREAKDOWN-DUPLICATE-FACTOR", "Match breakdown cannot contain duplicate factors."));
        }

        return Result.Success(new MatchBreakdown(list));
    }
}

public sealed record EmbeddingVector
{
    public IReadOnlyList<decimal> Values { get; private init; }
    public int Dimension { get; private init; }

    private EmbeddingVector(List<decimal> values, int dimension)
    {
        Values = values.AsReadOnly();
        Dimension = dimension;
    }

    public static Result<EmbeddingVector> Create(List<decimal> values, int dimension)
    {
        if (values == null || values.Count != dimension)
        {
            return Result.Failure<EmbeddingVector>(new Error("E-EMBEDDING-DIMENSION-MISMATCH", $"Embedding vector length must match the dimension of {dimension}."));
        }

        if (dimension <= 0)
        {
            return Result.Failure<EmbeddingVector>(new Error("E-EMBEDDING-DIMENSION-INVALID", "Dimension must be positive."));
        }

        return Result.Success(new EmbeddingVector(values, dimension));
    }
}

public sealed record ConfidenceScore
{
    public int Value { get; private init; }
    public bool NeedsReview => Value < 70;

    private ConfidenceScore(int value)
    {
        Value = value;
    }

    public static Result<ConfidenceScore> Create(int value)
    {
        if (value is < 0 or > 100)
        {
            return Result.Failure<ConfidenceScore>(new Error("E-CONFIDENCE-OUT-OF-RANGE", "Confidence score must be between 0 and 100."));
        }

        return Result.Success(new ConfidenceScore(value));
    }
}

public sealed record SkillRequirement
{
    public string TaxonomyCode { get; private init; }
    public string DisplayLabel { get; private init; }
    public int Proficiency { get; private init; } // 1-5
    public ConfidenceScore Confidence { get; private init; }

    private SkillRequirement(string taxonomyCode, string displayLabel, int proficiency, ConfidenceScore confidence)
    {
        TaxonomyCode = taxonomyCode;
        DisplayLabel = displayLabel;
        Proficiency = proficiency;
        Confidence = confidence;
    }

    public static Result<SkillRequirement> Create(string taxonomyCode, string displayLabel, int proficiency, ConfidenceScore confidence)
    {
        if (string.IsNullOrWhiteSpace(taxonomyCode))
        {
            return Result.Failure<SkillRequirement>(new Error("E-SKILL-TAXONOMY-REQUIRED", "Taxonomy code is required."));
        }

        if (string.IsNullOrWhiteSpace(displayLabel))
        {
            return Result.Failure<SkillRequirement>(new Error("E-SKILL-LABEL-REQUIRED", "Display label is required."));
        }

        if (proficiency is < 1 or > 5)
        {
            return Result.Failure<SkillRequirement>(new Error("E-SKILL-PROFICIENCY-OUT-OF-RANGE", "Proficiency must be between 1 and 5."));
        }

        if (confidence == null)
        {
            return Result.Failure<SkillRequirement>(new Error("E-SKILL-CONFIDENCE-REQUIRED", "Confidence score is required."));
        }

        return Result.Success(new SkillRequirement(taxonomyCode, displayLabel, proficiency, confidence));
    }
}

public sealed record GeoLocation
{
    public decimal Latitude { get; private init; }
    public decimal Longitude { get; private init; }
    public string City { get; private init; }

    private GeoLocation(decimal latitude, decimal longitude, string city)
    {
        Latitude = latitude;
        Longitude = longitude;
        City = city;
    }

    public static Result<GeoLocation> Create(decimal latitude, decimal longitude, string city)
    {
        if (latitude is < -90 or > 90)
        {
            return Result.Failure<GeoLocation>(new Error("E-LOCATION-LATITUDE-OUT-OF-RANGE", "Latitude must be between -90 and 90."));
        }

        if (longitude is < -180 or > 180)
        {
            return Result.Failure<GeoLocation>(new Error("E-LOCATION-LONGITUDE-OUT-OF-RANGE", "Longitude must be between -180 and 180."));
        }

        return Result.Success(new GeoLocation(latitude, longitude, city ?? string.Empty));
    }
}

public sealed record SalaryRange
{
    public decimal Min { get; private init; }
    public decimal Max { get; private init; }
    public string Currency { get; private init; }

    private SalaryRange(decimal min, decimal max, string currency)
    {
        Min = min;
        Max = max;
        Currency = currency;
    }

    public static Result<SalaryRange> Create(decimal min, decimal max, string currency)
    {
        if (min < 0)
        {
            return Result.Failure<SalaryRange>(new Error("E-SALARY-MIN-NEGATIVE", "Minimum salary cannot be negative."));
        }

        if (min > max)
        {
            return Result.Failure<SalaryRange>(new Error("E-SALARY-MIN-GREATER-THAN-MAX", "Minimum salary cannot exceed maximum salary."));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
        {
            return Result.Failure<SalaryRange>(new Error("E-SALARY-CURRENCY-INVALID", "Currency must be a valid 3-letter ISO code."));
        }

        return Result.Success(new SalaryRange(min, max, currency.ToUpperInvariant()));
    }
}

public sealed record QualificationThreshold
{
    public int MinOverallMatch { get; private init; }
    public int MinSkillMatch { get; private init; }
    public EducationLevel MinEducationLevel { get; private init; }
    public decimal MinExperienceYears { get; private init; }
    public IReadOnlyCollection<string> RequiredCertifications { get; private init; }

    private QualificationThreshold(int minOverallMatch, int minSkillMatch, EducationLevel minEducationLevel, decimal minExperienceYears, List<string> requiredCertifications)
    {
        MinOverallMatch = minOverallMatch;
        MinSkillMatch = minSkillMatch;
        MinEducationLevel = minEducationLevel;
        MinExperienceYears = minExperienceYears;
        RequiredCertifications = requiredCertifications.AsReadOnly();
    }

    public static Result<QualificationThreshold> Create(int minOverallMatch, int minSkillMatch, EducationLevel minEducationLevel, decimal minExperienceYears, List<string> requiredCertifications)
    {
        if (minOverallMatch is < 0 or > 100)
        {
            return Result.Failure<QualificationThreshold>(new Error("E-QUAL-OVERALL-OUT-OF-RANGE", "Minimum overall match percentage must be between 0 and 100."));
        }

        if (minSkillMatch is < 0 or > 100)
        {
            return Result.Failure<QualificationThreshold>(new Error("E-QUAL-SKILL-OUT-OF-RANGE", "Minimum skill match percentage must be between 0 and 100."));
        }

        if (minExperienceYears < 0)
        {
            return Result.Failure<QualificationThreshold>(new Error("E-QUAL-EXP-NEGATIVE", "Minimum experience years cannot be negative."));
        }

        return Result.Success(new QualificationThreshold(minOverallMatch, minSkillMatch, minEducationLevel, minExperienceYears, requiredCertifications ?? new List<string>()));
    }
}

public sealed record RecommendationReason
{
    public string Summary { get; private init; }
    public IReadOnlyCollection<MatchFactor> TopFactors { get; private init; }

    private RecommendationReason(string summary, List<MatchFactor> topFactors)
    {
        Summary = summary;
        TopFactors = topFactors.AsReadOnly();
    }

    public static Result<RecommendationReason> Create(string summary, List<MatchFactor> topFactors)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return Result.Failure<RecommendationReason>(new Error("E-REASON-SUMMARY-REQUIRED", "Summary explanation is required."));
        }

        return Result.Success(new RecommendationReason(summary, topFactors ?? new List<MatchFactor>()));
    }
}

public sealed record CandidateSearchCriteria
{
    public IReadOnlyCollection<string> Skills { get; private init; }
    public ExperienceLevel? ExperienceLevel { get; private init; }
    public decimal? MinExperienceYears { get; private init; }
    public decimal? MaxExperienceYears { get; private init; }
    public GeoLocation? Location { get; private init; }
    public decimal? RadiusKm { get; private init; }
    public SalaryRange? SalaryRange { get; private init; }
    public EducationLevel? MinEducationLevel { get; private init; }
    public IReadOnlyCollection<string> Certifications { get; private init; }

    private CandidateSearchCriteria(
        List<string> skills,
        ExperienceLevel? experienceLevel,
        decimal? minExperienceYears,
        decimal? maxExperienceYears,
        GeoLocation? location,
        decimal? radiusKm,
        SalaryRange? salaryRange,
        EducationLevel? minEducationLevel,
        List<string> certifications)
    {
        Skills = skills.AsReadOnly();
        ExperienceLevel = experienceLevel;
        MinExperienceYears = minExperienceYears;
        MaxExperienceYears = maxExperienceYears;
        Location = location;
        RadiusKm = radiusKm;
        SalaryRange = salaryRange;
        MinEducationLevel = minEducationLevel;
        Certifications = certifications.AsReadOnly();
    }

    public static Result<CandidateSearchCriteria> Create(
        List<string> skills,
        ExperienceLevel? experienceLevel,
        decimal? minExperienceYears,
        decimal? maxExperienceYears,
        GeoLocation? location,
        decimal? radiusKm,
        SalaryRange? salaryRange,
        EducationLevel? minEducationLevel,
        List<string> certifications)
    {
        if (location != null && (radiusKm == null || radiusKm <= 0))
        {
            return Result.Failure<CandidateSearchCriteria>(new Error("E-SEARCH-RADIUS-REQUIRED", "Search radius must be a positive value when location is supplied."));
        }

        if (minExperienceYears.HasValue && minExperienceYears.Value < 0)
        {
            return Result.Failure<CandidateSearchCriteria>(new Error("E-SEARCH-EXP-MIN-NEGATIVE", "Minimum experience years cannot be negative."));
        }

        if (minExperienceYears.HasValue && maxExperienceYears.HasValue && minExperienceYears.Value > maxExperienceYears.Value)
        {
            return Result.Failure<CandidateSearchCriteria>(new Error("E-SEARCH-EXP-MIN-GREATER-THAN-MAX", "Minimum experience years cannot exceed maximum experience years."));
        }

        return Result.Success(new CandidateSearchCriteria(
            skills ?? new List<string>(),
            experienceLevel,
            minExperienceYears,
            maxExperienceYears,
            location,
            radiusKm,
            salaryRange,
            minEducationLevel,
            certifications ?? new List<string>()));
    }
}

public sealed record FitAnalysis
{
    public int OverallScore { get; private init; }
    public IReadOnlyCollection<MatchFactor> Strengths { get; private init; }
    public IReadOnlyCollection<MatchFactor> Gaps { get; private init; }
    public SalaryFitIndicator SalaryFit { get; private init; }
    public int SalaryMatchPercent { get; private init; }
    public int MotivationScore { get; private init; }
    public TimeToProductivityEstimate TimeToProductivity { get; private init; }
    public ContactLikelihood ContactLikelihood { get; private init; }
    public bool WorkArrangementCompatible { get; private init; }

    private FitAnalysis(
        int overallScore,
        List<MatchFactor> strengths,
        List<MatchFactor> gaps,
        SalaryFitIndicator salaryFit,
        int salaryMatchPercent,
        int motivationScore,
        TimeToProductivityEstimate timeToProductivity,
        ContactLikelihood contactLikelihood,
        bool workArrangementCompatible)
    {
        OverallScore = overallScore;
        Strengths = strengths.AsReadOnly();
        Gaps = gaps.AsReadOnly();
        SalaryFit = salaryFit;
        SalaryMatchPercent = salaryMatchPercent;
        MotivationScore = motivationScore;
        TimeToProductivity = timeToProductivity;
        ContactLikelihood = contactLikelihood;
        WorkArrangementCompatible = workArrangementCompatible;
    }

    public static Result<FitAnalysis> Create(
        int overallScore,
        List<MatchFactor> strengths,
        List<MatchFactor> gaps,
        SalaryFitIndicator salaryFit,
        int salaryMatchPercent,
        int motivationScore,
        TimeToProductivityEstimate timeToProductivity,
        ContactLikelihood contactLikelihood,
        bool workArrangementCompatible)
    {
        if (overallScore is < 0 or > 100)
        {
            return Result.Failure<FitAnalysis>(new Error("E-FIT-OVERALL-OUT-OF-RANGE", "Overall score must be between 0 and 100."));
        }

        if (salaryMatchPercent is < 0 or > 100)
        {
            return Result.Failure<FitAnalysis>(new Error("E-FIT-SALARY-OUT-OF-RANGE", "Salary match percent must be between 0 and 100."));
        }

        if (motivationScore is < 0 or > 100)
        {
            return Result.Failure<FitAnalysis>(new Error("E-FIT-MOTIVATION-OUT-OF-RANGE", "Motivation score must be between 0 and 100."));
        }

        return Result.Success(new FitAnalysis(
            overallScore,
            strengths ?? new List<MatchFactor>(),
            gaps ?? new List<MatchFactor>(),
            salaryFit,
            salaryMatchPercent,
            motivationScore,
            timeToProductivity,
            contactLikelihood,
            workArrangementCompatible));
    }
}
