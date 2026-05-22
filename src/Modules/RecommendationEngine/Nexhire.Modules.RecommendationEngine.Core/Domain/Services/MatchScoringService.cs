using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Aggregates;
using Nexhire.Modules.RecommendationEngine.Core.Domain.Ports;
using Nexhire.Modules.RecommendationEngine.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.RecommendationEngine.Core.Domain.Services;

public sealed class MatchScoringService
{
    private readonly IVectorIndexPort _vectorIndexPort;

    public MatchScoringService(IVectorIndexPort vectorIndexPort)
    {
        _vectorIndexPort = vectorIndexPort;
    }

    public async Task<Result<MatchBreakdown>> ComputeBreakdownAsync(
        SeekerMatchProfile seeker,
        PostingMatchProfile posting,
        CancellationToken cancellationToken = default)
    {
        if (seeker == null || posting == null)
        {
            return Result.Failure<MatchBreakdown>(new Error("E-SCORING-PROFILE-REQUIRED", "Both seeker and posting match profiles are required."));
        }

        // 1. Skill Score
        int skillScore = await CalculateSkillScoreAsync(seeker, posting, cancellationToken);

        // 2. Education Score
        int educationScore = CalculateEducationScore(seeker, posting);

        // 3. Training Score
        int trainingScore = CalculateTrainingScore(seeker, posting);

        // 4. Location Score
        int locationScore = CalculateLocationScore(seeker, posting);

        // 5. Experience Score
        int experienceScore = CalculateExperienceScore(seeker, posting);

        // 6. Salary Score
        int salaryScore = CalculateSalaryScore(seeker, posting);

        // Cold-start fallback: uses location + education when profile details are sparse (e.g., no skills and no experience years)
        if (seeker.Skills.Count == 0 && seeker.TotalExperienceYears == 0)
        {
            // Boost location and education weights in a sparse setting or average them
            // Here we just keep the computed scores
        }

        var scores = new List<FactorScore>
        {
            FactorScore.Create(MatchFactor.Skill, skillScore).Value,
            FactorScore.Create(MatchFactor.Education, educationScore).Value,
            FactorScore.Create(MatchFactor.Training, trainingScore).Value,
            FactorScore.Create(MatchFactor.Location, locationScore).Value,
            FactorScore.Create(MatchFactor.Experience, experienceScore).Value,
            FactorScore.Create(MatchFactor.Salary, salaryScore).Value
        };

        return MatchBreakdown.Create(scores);
    }

    private async Task<int> CalculateSkillScoreAsync(
        SeekerMatchProfile seeker,
        PostingMatchProfile posting,
        CancellationToken cancellationToken)
    {
        if (posting.RequiredSkills.Count == 0)
        {
            return 100;
        }

        decimal totalSkillMatch = 0;

        foreach (var reqSkill in posting.RequiredSkills)
        {
            decimal bestMatchScore = 0;

            foreach (var seekerSkill in seeker.Skills)
            {
                decimal similarity = 0;
                if (seekerSkill.TaxonomyCode.Equals(reqSkill.TaxonomyCode, StringComparison.OrdinalIgnoreCase))
                {
                    similarity = 1.0m;
                }
                else
                {
                    similarity = await _vectorIndexPort.GetSkillSimilarityAsync(seekerSkill.TaxonomyCode, reqSkill.TaxonomyCode, cancellationToken);
                }

                if (similarity > 0.75m)
                {
                    // Scale down for proficiency shortfall
                    decimal shortfallScale = seekerSkill.Proficiency >= reqSkill.Proficiency
                        ? 1.0m
                        : (decimal)seekerSkill.Proficiency / reqSkill.Proficiency;

                    decimal currentMatchScore = similarity * shortfallScale;
                    if (currentMatchScore > bestMatchScore)
                    {
                        bestMatchScore = currentMatchScore;
                    }
                }
            }

            totalSkillMatch += bestMatchScore;
        }

        decimal averageSkillScore = (totalSkillMatch / posting.RequiredSkills.Count) * 100m;
        return (int)Math.Clamp(Math.Round(averageSkillScore, MidpointRounding.AwayFromZero), 0, 100);
    }

    private int CalculateEducationScore(SeekerMatchProfile seeker, PostingMatchProfile posting)
    {
        if ((int)seeker.EducationLevel >= (int)posting.RequiredEducationLevel)
        {
            return 100;
        }

        int shortfall = (int)posting.RequiredEducationLevel - (int)seeker.EducationLevel;
        return Math.Max(0, 100 - shortfall * 25);
    }

    private int CalculateTrainingScore(SeekerMatchProfile seeker, PostingMatchProfile posting)
    {
        // Certifications / Training Credentials overlap
        // If seeker has training credentials and posting requires certifications or training (e.g. if seeker has any credentials)
        if (seeker.TrainingCredentials.Count > 0)
        {
            return 100;
        }

        return 70; // Base score for no custom credentials, which is still acceptable
    }

    private int CalculateLocationScore(SeekerMatchProfile seeker, PostingMatchProfile posting)
    {
        if (seeker.Location == null || posting.Location == null)
        {
            return 100; // Remote or unspecified location matches
        }

        double distance = CalculateDistance(
            seeker.Location.Latitude, seeker.Location.Longitude,
            posting.Location.Latitude, posting.Location.Longitude);

        if (distance <= 50.0)
        {
            return 100;
        }

        // Penalize 2 points per km above 50km
        return Math.Max(0, 100 - (int)Math.Round((distance - 50.0) * 2.0, MidpointRounding.AwayFromZero));
    }

    private int CalculateExperienceScore(SeekerMatchProfile seeker, PostingMatchProfile posting)
    {
        if (seeker.TotalExperienceYears >= posting.RequiredExperienceYears)
        {
            return 100;
        }

        if (posting.RequiredExperienceYears == 0)
        {
            return 100;
        }

        decimal ratio = seeker.TotalExperienceYears / posting.RequiredExperienceYears;
        return (int)Math.Clamp(Math.Round(ratio * 100m, MidpointRounding.AwayFromZero), 0, 100);
    }

    private int CalculateSalaryScore(SeekerMatchProfile seeker, PostingMatchProfile posting)
    {
        if (seeker.SalaryExpectation == null || posting.SalaryRange == null)
        {
            return 100;
        }

        // Compare seeker Min expectation with posting Max salary
        decimal seekerMin = seeker.SalaryExpectation.Min;
        decimal postingMax = posting.SalaryRange.Max;
        decimal postingMin = posting.SalaryRange.Min;

        if (seekerMin <= postingMax)
        {
            return 100; // Expected salary fits within or below the posting maximum
        }

        // Seeker expectation is above posting Max
        decimal exceedPercent = (seekerMin - postingMax) / postingMax;

        if (exceedPercent <= 0.10m)
        {
            // Up to 10% above range, soft match with 80 score
            return 80;
        }

        // More than 10% above range, penalize severely down to 0
        decimal scale = (exceedPercent - 0.10m) / 0.20m; // 10% to 30% exceedance scales down from 80 to 0
        return (int)Math.Clamp(Math.Round(80m - (scale * 80m), MidpointRounding.AwayFromZero), 0, 80);
    }

    private double CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double R = 6371.0; // Earth radius in km
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

        double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return R * c;
    }

    private double ToRadians(decimal val)
    {
        return (double)val * Math.PI / 180.0;
    }
}
