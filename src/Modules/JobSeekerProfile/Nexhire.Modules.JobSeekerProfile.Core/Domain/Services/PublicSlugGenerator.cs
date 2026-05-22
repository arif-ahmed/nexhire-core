using System.Text.RegularExpressions;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;

public static class PublicSlugGenerator
{
    private static readonly HashSet<string> BannedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "abuse", "asshole", "bitch", "bastard", "crap", "cunt", "damn", "fuck", "nigger", "shit", "slut", "whore"
    };

    private static readonly Random RandomSource = new();

    public static Result<string> Generate(PersonName name, Func<string, bool> isSlugTaken, int maxRetries = 10)
    {
        if (name == null)
        {
            return Result.Failure<string>(new Error("PublicSlug.NullName", "Name cannot be null."));
        }

        // Clean names: lowercase, strip non-alphanumeric characters
        var cleanedFirst = CleanNamePart(name.First);
        var cleanedLast = CleanNamePart(name.Last);

        if (string.IsNullOrEmpty(cleanedFirst) && string.IsNullOrEmpty(cleanedLast))
        {
            return Result.Failure<string>(new Error("E-SLUG-GENERATION", "Failed to generate slug from empty name parts."));
        }

        var baseSlug = $"{cleanedFirst}-{cleanedLast}";

        for (int i = 0; i < maxRetries; i++)
        {
            var hash = GenerateRandomHash(4);
            var candidateSlug = $"{baseSlug}-{hash}".ToLowerInvariant();

            if (ContainsProfanity(candidateSlug))
            {
                continue;
            }

            if (!isSlugTaken(candidateSlug))
            {
                return Result.Success(candidateSlug);
            }
        }

        return Result.Failure<string>(new Error("E-SLUG-GENERATION", "Failed to generate a unique public slug after maximum retries."));
    }

    private static string CleanNamePart(string part)
    {
        if (string.IsNullOrWhiteSpace(part))
        {
            return string.Empty;
        }

        // Keep only alphanumeric characters and convert to lowercase
        var lower = part.ToLowerInvariant();
        return Regex.Replace(lower, @"[^a-z0-9]", string.Empty);
    }

    private static string GenerateRandomHash(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[RandomSource.Next(chars.Length)];
        }
        return new string(result);
    }

    private static bool ContainsProfanity(string slug)
    {
        foreach (var word in BannedWords)
        {
            if (slug.Contains(word))
            {
                return true;
            }
        }
        return false;
    }
}
