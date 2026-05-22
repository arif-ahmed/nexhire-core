using Nexhire.Modules.JobSeekerProfile.Core.Domain.Aggregates;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Ports;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Repositories;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.Services;
using Nexhire.Modules.JobSeekerProfile.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;
using System.Text.Json;

namespace Nexhire.Modules.JobSeekerProfile.Core.JobSeekerProfile.Commands.UploadResume;

public class UploadResumeCommandHandler : ICommandHandler<UploadResumeCommand>
{
    private readonly IJobSeekerProfileRepository _profileRepository;
    private readonly IResumeRepository _resumeRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly IVirusScanner _virusScanner;
    private readonly IResumeParser _resumeParser;
    private readonly IUnitOfWork _unitOfWork;

    public UploadResumeCommandHandler(
        IJobSeekerProfileRepository profileRepository,
        IResumeRepository resumeRepository,
        IObjectStorage objectStorage,
        IVirusScanner virusScanner,
        IResumeParser resumeParser,
        IUnitOfWork unitOfWork)
    {
        _profileRepository = profileRepository;
        _resumeRepository = resumeRepository;
        _objectStorage = objectStorage;
        _virusScanner = virusScanner;
        _resumeParser = resumeParser;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UploadResumeCommand request, CancellationToken cancellationToken)
    {
        // 1. Load Profile
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            return Result.Failure(new Error("JobSeekerProfile.NotFound", "Job seeker profile not found."));
        }

        // 2. Validate format and size before storing
        var tempFileRefResult = FileReference.Create("temp_key", request.FileName, request.MimeType, request.Content.Length);
        if (tempFileRefResult.IsFailure)
        {
            return Result.Failure(tempFileRefResult.Error);
        }

        var tempResumeResult = Resume.Upload(Guid.NewGuid(), profile.Id, tempFileRefResult.Value);
        if (tempResumeResult.IsFailure)
        {
            return Result.Failure(tempResumeResult.Error);
        }

        // 3. Store file in object storage
        var storeResult = await _objectStorage.StoreAsync(request.Content, request.FileName, request.MimeType, cancellationToken);
        if (storeResult.IsFailure)
        {
            return Result.Failure(storeResult.Error);
        }

        var fileRef = storeResult.Value;

        // 4. Create Resume Aggregate
        var resumeResult = Resume.Upload(Guid.NewGuid(), profile.Id, fileRef);
        if (resumeResult.IsFailure)
        {
            await _objectStorage.DeleteAsync(fileRef.StorageKey, cancellationToken);
            return Result.Failure(resumeResult.Error);
        }

        var resume = resumeResult.Value;

        // 5. Replace existing active resume if any
        var existingActiveResume = await _resumeRepository.GetActiveByProfileIdAsync(profile.Id, cancellationToken);
        if (existingActiveResume != null)
        {
            ResumeReplacementService.Replace(existingActiveResume, resume);
        }

        // 6. Perform Virus Scanning
        var scanResult = await _virusScanner.ScanAsync(fileRef, cancellationToken);
        var recordScanResult = resume.RecordScanResult(scanResult);
        if (recordScanResult.IsFailure)
        {
            // Infected file: Save failed status and return infected error
            if (existingActiveResume != null)
            {
                await _resumeRepository.UpdateAsync(existingActiveResume, cancellationToken);
            }
            await _resumeRepository.AddAsync(resume, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(recordScanResult.Error);
        }

        // 7. Perform Resume Parsing (with a 30-second timeout)
        resume.BeginParsing();
        using var parserCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        parserCts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            var parseResult = await _resumeParser.ParseAsync(fileRef, parserCts.Token);
            if (parseResult.IsSuccess)
            {
                resume.RecordParseSuccess(parseResult.Value, "NexhireAIParser");
            }
            else
            {
                resume.RecordParseFailure(parseResult.Error.Message);
            }
        }
        catch (OperationCanceledException)
        {
            resume.RecordParseFailure("Parsing timed out after 30 seconds.");
        }
        catch (Exception ex)
        {
            resume.RecordParseFailure($"Parsing failed with exception: {ex.Message}");
        }

        // 8. Persist All Changes
        if (existingActiveResume != null)
        {
            await _resumeRepository.UpdateAsync(existingActiveResume, cancellationToken);
        }
        await _resumeRepository.AddAsync(resume, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
