using Nexhire.Modules.JobSeekerProfile.Core.Domain.Events;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.Domain;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;

public class Resume : AggregateRoot<Guid>
{
    private readonly List<string> _mergedFieldKeys = new();

    public Guid ProfileId { get; private set; }
    public FileReference File { get; private set; } = null!;
    public VirusScanResult ScanResult { get; private set; } = null!;
    public ResumeParseStatus ParseStatus { get; private set; }
    public ParsedResumeData? ParsedData { get; private set; }
    public string? ParserName { get; private set; }
    public DateTime? ParsedOnUtc { get; private set; }
    public string? FailureReason { get; private set; }
    public IReadOnlyCollection<string> MergedFieldKeys => _mergedFieldKeys.AsReadOnly();
    public bool IsSuperseded { get; private set; }
    public DateTime UploadedOnUtc { get; private set; }

    private Resume(
        Guid id,
        Guid profileId,
        FileReference file,
        DateTime uploadedOnUtc) : base(id)
    {
        ProfileId = profileId;
        File = file;
        ScanResult = VirusScanResult.Create(VirusScanStatus.Pending).Value;
        ParseStatus = ResumeParseStatus.Uploaded;
        IsSuperseded = false;
        UploadedOnUtc = uploadedOnUtc;
    }

    private Resume()
    {
        // Required by EF Core
    }

    public static Result<Resume> Upload(Guid id, Guid profileId, FileReference file)
    {
        if (profileId == Guid.Empty)
        {
            return Result.Failure<Resume>(new Error("Resume.InvalidProfileId", "ProfileId cannot be empty."));
        }

        if (file == null)
        {
            return Result.Failure<Resume>(new Error("Resume.NullFile", "File cannot be null."));
        }

        var lowerMime = file.MimeType.ToLowerInvariant();
        if (lowerMime != "application/pdf" &&
            lowerMime != "application/vnd.openxmlformats-officedocument.wordprocessingml.document" &&
            lowerMime != "text/plain")
        {
            return Result.Failure<Resume>(new Error("E-UPLOAD-INVALID-FORMAT", "Only PDF, DOCX, and TXT formats are allowed."));
        }

        if (file.SizeBytes > 5 * 1024 * 1024)
        {
            return Result.Failure<Resume>(new Error("E-UPLOAD-SIZE-EXCEEDED", "Resume file size must not exceed 5 MB."));
        }

        var uploadedOnUtc = DateTime.UtcNow;
        var resume = new Resume(id, profileId, file, uploadedOnUtc);

        resume.RaiseDomainEvent(new ResumeUploadedEvent(
            Guid.NewGuid(),
            resume.ProfileId,
            resume.Id,
            file.MimeType,
            uploadedOnUtc));

        return Result.Success(resume);
    }

    public Result RecordScanResult(VirusScanResult scanResult)
    {
        if (ParseStatus != ResumeParseStatus.Uploaded)
        {
            return Result.Failure(new Error("Resume.InvalidStatusForScan", "Scan result can only be recorded when status is Uploaded."));
        }

        if (scanResult == null)
        {
            return Result.Failure(new Error("Resume.NullScanResult", "Scan result cannot be null."));
        }

        ScanResult = scanResult;

        if (scanResult.Status == VirusScanStatus.Infected)
        {
            ParseStatus = ResumeParseStatus.Failed;
            FailureReason = "Virus detected in file";
            
            RaiseDomainEvent(new ResumeScanFailedEvent(
                Guid.NewGuid(),
                ProfileId,
                Id,
                "E-UPLOAD-VIRUS",
                DateTime.UtcNow));

            return Result.Failure(new Error("E-UPLOAD-VIRUS", "The uploaded file is infected."));
        }

        if (scanResult.Status == VirusScanStatus.Clean)
        {
            ParseStatus = ResumeParseStatus.Scanned;
        }

        return Result.Success();
    }

    public Result BeginParsing()
    {
        if (ParseStatus != ResumeParseStatus.Scanned)
        {
            return Result.Failure(new Error("Resume.InvalidStatusForParsing", "Parsing can only begin when status is Scanned."));
        }

        ParseStatus = ResumeParseStatus.Parsing;
        return Result.Success();
    }

    public Result RecordParseSuccess(ParsedResumeData parsedData, string parserName)
    {
        if (ParseStatus != ResumeParseStatus.Parsing)
        {
            return Result.Failure(new Error("Resume.InvalidStatusForParseSuccess", "Parse success can only be recorded when status is Parsing."));
        }

        if (parsedData == null)
        {
            return Result.Failure(new Error("Resume.NullParsedData", "Parsed data cannot be null."));
        }

        ParseStatus = ResumeParseStatus.Parsed;
        ParsedData = parsedData;
        ParserName = parserName;
        ParsedOnUtc = DateTime.UtcNow;

        RaiseDomainEvent(new ResumeParsedEvent(
            Guid.NewGuid(),
            ProfileId,
            Id,
            ParsedData.Skills.Select(s => s.RawLabel).ToList().AsReadOnly(),
            ParsedData.Education,
            ParsedData.Experience,
            ParsedOnUtc.Value));

        return Result.Success();
    }

    public Result RecordParseFailure(string reason)
    {
        if (ParseStatus != ResumeParseStatus.Parsing)
        {
            return Result.Failure(new Error("Resume.InvalidStatusForParseFailure", "Parse failure can only be recorded when status is Parsing."));
        }

        ParseStatus = ResumeParseStatus.Failed;
        FailureReason = reason;

        RaiseDomainEvent(new ResumeParseFailedEvent(
            Guid.NewGuid(),
            ProfileId,
            Id,
            reason,
            DateTime.UtcNow));

        return Result.Success();
    }

    public Result ConfirmMergedFields(IEnumerable<string> fieldKeys)
    {
        if (ParseStatus != ResumeParseStatus.Parsed)
        {
            return Result.Failure(new Error("Resume.InvalidStatusForConfirmation", "Fields can only be confirmed when status is Parsed."));
        }

        var keysList = fieldKeys?.ToList() ?? new List<string>();
        _mergedFieldKeys.Clear();
        _mergedFieldKeys.AddRange(keysList);

        RaiseDomainEvent(new ResumeFieldsConfirmedEvent(
            Guid.NewGuid(),
            ProfileId,
            Id,
            _mergedFieldKeys.AsReadOnly(),
            DateTime.UtcNow));

        return Result.Success();
    }

    public void Supersede()
    {
        IsSuperseded = true;
    }
}
