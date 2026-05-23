using Nexhire.Modules.ContentManagement.Core.Application.DTOs;
using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Application.Commands;

// --- Article Handlers ---

public sealed class CreateArticleDraftCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<CreateArticleDraftCommand, CreateArticleDraftResponse>
{
    public async Task<Result<CreateArticleDraftResponse>> Handle(CreateArticleDraftCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure<CreateArticleDraftResponse>(new Error("E-INVALID-LANGUAGE", "Invalid language. Use 'En' or 'Bn'."));

        var contentResult = LocalizedContent.Create(request.Title, request.Summary, request.BodyRichText);
        if (contentResult.IsFailure)
            return Result.Failure<CreateArticleDraftResponse>(contentResult.Error);

        var article = Article.CreateDraft(request.AuthorUserId, language, contentResult.Value);
        await repository.AddAsync(article, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateArticleDraftResponse(article.Id));
    }
}

public sealed class UpdateArticleContentCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UpdateArticleContentCommand>
{
    public async Task<Result> Handle(UpdateArticleContentCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        var contentResult = LocalizedContent.Create(request.Title, request.Summary, request.BodyRichText);
        if (contentResult.IsFailure)
            return contentResult;

        article.SetLocalization(language, contentResult.Value);
        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class RemoveArticleLocalizationCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<RemoveArticleLocalizationCommand>
{
    public async Task<Result> Handle(RemoveArticleLocalizationCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        var result = article.RemoveLocalization(language);
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class SetArticleCategoryCommandHandler(
    IArticleRepository repository,
    ICategoryRepository categoryRepository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SetArticleCategoryCommand>
{
    public async Task<Result> Handle(SetArticleCategoryCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, ct);
        if (category is null || !category.IsActive)
            return Result.Failure(new Error("E-CATEGORY-NOT-FOUND", "Category not found or inactive."));

        article.SetPrimaryCategory(request.CategoryId);
        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class SetArticleTagsCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SetArticleTagsCommand>
{
    public async Task<Result> Handle(SetArticleTagsCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        article.SetTags(language, request.Tags);
        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class AddArticleMediaCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<AddArticleMediaCommand>
{
    public async Task<Result> Handle(AddArticleMediaCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        if (!Enum.TryParse<MediaKind>(request.Kind, true, out var kind))
            return Result.Failure(new Error("E-MEDIA-INVALID-KIND", "Invalid media kind. Use 'Image' or 'Video'."));

        var mediaResult = MediaReference.Create(request.StorageKey, request.Url, request.MimeType, request.SizeBytes, kind);
        if (mediaResult.IsFailure)
            return mediaResult;

        article.AddMedia(mediaResult.Value);
        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class RemoveArticleMediaCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<RemoveArticleMediaCommand>
{
    public async Task<Result> Handle(RemoveArticleMediaCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var result = article.RemoveMedia(request.StorageKey);
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class PublishArticleCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<PublishArticleCommand>
{
    public async Task<Result> Handle(PublishArticleCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var result = article.Publish();
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class ScheduleArticleCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<ScheduleArticleCommand>
{
    public async Task<Result> Handle(ScheduleArticleCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var scheduleResult = PublicationSchedule.Create(request.PublishAtUtc);
        if (scheduleResult.IsFailure)
            return Result.Failure(new Error(scheduleResult.Error.Code, scheduleResult.Error.Message));

        var result = article.Schedule(scheduleResult.Value);
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class CancelArticleScheduleCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<CancelArticleScheduleCommand>
{
    public async Task<Result> Handle(CancelArticleScheduleCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var result = article.CancelSchedule();
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class RescheduleArticleCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<RescheduleArticleCommand>
{
    public async Task<Result> Handle(RescheduleArticleCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var cancelResult = article.CancelSchedule();
        if (cancelResult.IsFailure) return cancelResult;

        var scheduleResult = PublicationSchedule.Create(request.PublishAtUtc);
        if (scheduleResult.IsFailure)
            return Result.Failure(new Error(scheduleResult.Error.Code, scheduleResult.Error.Message));

        var result = article.Schedule(scheduleResult.Value);
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class UnpublishArticleCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UnpublishArticleCommand>
{
    public async Task<Result> Handle(UnpublishArticleCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var result = article.Unpublish();
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class ArchiveArticleCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<ArchiveArticleCommand>
{
    public async Task<Result> Handle(ArchiveArticleCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var result = article.Archive();
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class BulkArchiveArticlesCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<BulkArchiveArticlesCommand>
{
    public async Task<Result> Handle(BulkArchiveArticlesCommand request, CancellationToken ct)
    {
        if (request.ArticleIds.Count == 0 || request.ArticleIds.Count > 50)
            return Result.Failure(new Error("E-BULK-LIMIT-EXCEEDED", "Must provide between 1 and 50 article IDs."));

        foreach (var id in request.ArticleIds)
        {
            var article = await repository.GetByIdAsync(id, ct);
            if (article is null) continue;

            var result = article.Archive();
            if (result.IsFailure) continue;

            repository.Update(article);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class RestoreArticleFromArchiveCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<RestoreArticleFromArchiveCommand>
{
    public async Task<Result> Handle(RestoreArticleFromArchiveCommand request, CancellationToken ct)
    {
        var article = await repository.GetByIdAsync(request.ArticleId, ct);
        if (article is null)
            return Result.Failure(new Error("E-ARTICLE-NOT-FOUND", "Article not found."));

        var result = article.RestoreFromArchive();
        if (result.IsFailure) return result;

        repository.Update(article);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class PublishDueArticlesCommandHandler(
    IArticleRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<PublishDueArticlesCommand>
{
    public async Task<Result> Handle(PublishDueArticlesCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var due = await repository.GetDueForPublicationAsync(now, ct);

        foreach (var article in due)
        {
            if (article.IsDueForPublication(now))
            {
                article.MarkPublishedBySchedule();
                repository.Update(article);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
