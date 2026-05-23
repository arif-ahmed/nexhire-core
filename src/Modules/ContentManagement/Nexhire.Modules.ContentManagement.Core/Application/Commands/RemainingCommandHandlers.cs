using Nexhire.Modules.ContentManagement.Core.Application.DTOs;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using Nexhire.Shared.Core.CQRS;
using Nexhire.Shared.Core.Results;

namespace Nexhire.Modules.ContentManagement.Core.Application.Commands;

// --- Category Handlers ---

public sealed class CreateCategoryCommandHandler(
    ICategoryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<CreateCategoryCommand, CreateArticleDraftResponse>
{
    public async Task<Result<CreateArticleDraftResponse>> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (await repository.IsSlugTakenAsync(request.Slug, null, ct))
            return Result.Failure<CreateArticleDraftResponse>(new Error("E-CATEGORY-SLUG-TAKEN", "Slug is already taken."));

        var names = request.Names.ToDictionary(kv => Enum.Parse<Language>(kv.Key), kv => kv.Value);
        var category = Category.Create(names, request.Slug);
        await repository.AddAsync(category, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateArticleDraftResponse(category.Id));
    }
}

public sealed class UpdateCategoryCommandHandler(
    ICategoryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UpdateCategoryCommand>
{
    public async Task<Result> Handle(UpdateCategoryCommand request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return Result.Failure(new Error("E-CATEGORY-NOT-FOUND", "Category not found."));

        if (request.Names is not null)
            foreach (var kv in request.Names)
                if (Enum.TryParse<Language>(kv.Key, true, out var lang))
                    category.Rename(lang, kv.Value);

        if (request.Slug is not null)
        {
            if (await repository.IsSlugTakenAsync(request.Slug, request.CategoryId, ct))
                return Result.Failure(new Error("E-CATEGORY-SLUG-TAKEN", "Slug is already taken."));
            category.SetSlug(request.Slug);
        }

        repository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class DeactivateCategoryCommandHandler(
    ICategoryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<DeactivateCategoryCommand>
{
    public async Task<Result> Handle(DeactivateCategoryCommand request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return Result.Failure(new Error("E-CATEGORY-NOT-FOUND", "Category not found."));
        category.Deactivate();
        repository.Update(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class DeleteCategoryCommandHandler(
    IArticleRepository articleRepository,
    ICategoryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(DeleteCategoryCommand request, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return Result.Failure(new Error("E-CATEGORY-NOT-FOUND", "Category not found."));

        var refCount = await articleRepository.CountByCategoryAsync(request.CategoryId, ct);
        var ensureResult = category.EnsureDeletable(refCount);
        if (ensureResult.IsFailure) return ensureResult;

        repository.Delete(category);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- Topic Handlers ---

public sealed class CreateTopicCommandHandler(
    ITopicRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<CreateTopicCommand, CreateArticleDraftResponse>
{
    public async Task<Result<CreateArticleDraftResponse>> Handle(CreateTopicCommand request, CancellationToken ct)
    {
        if (await repository.IsSlugTakenAsync(request.Slug, null, ct))
            return Result.Failure<CreateArticleDraftResponse>(new Error("E-TOPIC-SLUG-TAKEN", "Slug is already taken."));

        var names = request.Names.ToDictionary(kv => Enum.Parse<Language>(kv.Key), kv => kv.Value);
        var topic = Topic.Create(names, request.Slug);
        await repository.AddAsync(topic, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateArticleDraftResponse(topic.Id));
    }
}

public sealed class UpdateTopicCommandHandler(
    ITopicRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UpdateTopicCommand>
{
    public async Task<Result> Handle(UpdateTopicCommand request, CancellationToken ct)
    {
        var topic = await repository.GetByIdAsync(request.TopicId, ct);
        if (topic is null)
            return Result.Failure(new Error("E-TOPIC-NOT-FOUND", "Topic not found."));

        if (request.Names is not null)
            foreach (var kv in request.Names)
                if (Enum.TryParse<Language>(kv.Key, true, out var lang))
                    topic.Rename(lang, kv.Value);

        if (request.Slug is not null)
        {
            if (await repository.IsSlugTakenAsync(request.Slug, request.TopicId, ct))
                return Result.Failure(new Error("E-TOPIC-SLUG-TAKEN", "Slug is already taken."));
            topic.SetSlug(request.Slug);
        }

        repository.Update(topic);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class DeleteTopicCommandHandler(
    IFaqEntryRepository faqRepository,
    ITopicRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<DeleteTopicCommand>
{
    public async Task<Result> Handle(DeleteTopicCommand request, CancellationToken ct)
    {
        var topic = await repository.GetByIdAsync(request.TopicId, ct);
        if (topic is null)
            return Result.Failure(new Error("E-TOPIC-NOT-FOUND", "Topic not found."));

        var refCount = await repository.CountReferencingEntriesAsync(request.TopicId, ct);
        var ensureResult = topic.EnsureDeletable(refCount);
        if (ensureResult.IsFailure) return ensureResult;

        repository.Delete(topic);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- FaqEntry Handlers ---

public sealed class CreateFaqEntryCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<CreateFaqEntryCommand, CreateArticleDraftResponse>
{
    public async Task<Result<CreateArticleDraftResponse>> Handle(CreateFaqEntryCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<FaqEntryKind>(request.Kind, true, out var kind))
            return Result.Failure<CreateArticleDraftResponse>(new Error("E-FAQ-INVALID-KIND", "Invalid FAQ entry kind."));
        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure<CreateArticleDraftResponse>(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        var contentResult = FaqContent.Create(request.Question, request.AnswerRichText);
        if (contentResult.IsFailure)
            return Result.Failure<CreateArticleDraftResponse>(contentResult.Error);

        var entry = FaqEntry.CreateDraft(kind, language, contentResult.Value);
        await repository.AddAsync(entry, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateArticleDraftResponse(entry.Id));
    }
}

public sealed class UpdateFaqEntryContentCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UpdateFaqEntryContentCommand>
{
    public async Task<Result> Handle(UpdateFaqEntryContentCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));
        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        var contentResult = FaqContent.Create(request.Question, request.AnswerRichText);
        if (contentResult.IsFailure) return contentResult;

        entry.SetLocalization(language, contentResult.Value);
        repository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class SetFaqTopicsCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SetFaqTopicsCommand>
{
    public async Task<Result> Handle(SetFaqTopicsCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));
        entry.SetTopics(request.TopicIds);
        repository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class SetFaqVisibleRolesCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SetFaqVisibleRolesCommand>
{
    public async Task<Result> Handle(SetFaqVisibleRolesCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));

        var roles = request.Roles.Select(r => Enum.Parse<VisibleRole>(r, true));
        var setResult = VisibleRoleSet.Create(roles);
        if (setResult.IsFailure) return setResult;

        entry.SetVisibleRoles(setResult.Value);
        repository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class SetFaqContextKeysCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SetFaqContextKeysCommand>
{
    public async Task<Result> Handle(SetFaqContextKeysCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));
        entry.SetContextKeys(request.ContextKeys);
        repository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class PublishFaqEntryCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<PublishFaqEntryCommand>
{
    public async Task<Result> Handle(PublishFaqEntryCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));
        var result = entry.Publish();
        if (result.IsFailure) return result;
        repository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class UnpublishFaqEntryCommandHandler(
    IFaqEntryRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UnpublishFaqEntryCommand>
{
    public async Task<Result> Handle(UnpublishFaqEntryCommand request, CancellationToken ct)
    {
        var entry = await repository.GetByIdAsync(request.FaqEntryId, ct);
        if (entry is null)
            return Result.Failure(new Error("E-FAQ-NOT-FOUND", "FAQ entry not found."));
        var result = entry.Unpublish();
        if (result.IsFailure) return result;
        repository.Update(entry);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- GuidedTour Handlers ---

public sealed class CreateGuidedTourCommandHandler(
    IGuidedTourRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<CreateGuidedTourCommand, CreateArticleDraftResponse>
{
    public async Task<Result<CreateArticleDraftResponse>> Handle(CreateGuidedTourCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure<CreateArticleDraftResponse>(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        var audiences = request.TargetAudience.Select(a => Enum.Parse<Audience>(a, true));
        var audienceResult = AudienceSet.Create(audiences);
        if (audienceResult.IsFailure)
            return Result.Failure<CreateArticleDraftResponse>(audienceResult.Error);

        var tour = GuidedTour.Create(language, request.Name, request.Description, audienceResult.Value);
        await repository.AddAsync(tour, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success(new CreateArticleDraftResponse(tour.Id));
    }
}

public sealed class AddTourStepCommandHandler(
    IGuidedTourRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<AddTourStepCommand>
{
    public async Task<Result> Handle(AddTourStepCommand request, CancellationToken ct)
    {
        var tour = await repository.GetByIdAsync(request.TourId, ct);
        if (tour is null)
            return Result.Failure(new Error("E-TOUR-NOT-FOUND", "Guided tour not found."));

        TourAction? action = null;
        if (request.ActionKind is not null && Enum.TryParse<TourActionKind>(request.ActionKind, true, out var actionKind))
        {
            var actionResult = TourAction.Create(actionKind, request.ActionPayload);
            if (actionResult.IsFailure) return actionResult;
            action = actionResult.Value;
        }

        var result = tour.AddStep(request.TargetSelector, request.TooltipText, action);
        if (result.IsFailure) return result;

        repository.Update(tour);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class UpdateTourStepCommandHandler(
    IGuidedTourRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UpdateTourStepCommand>
{
    public async Task<Result> Handle(UpdateTourStepCommand request, CancellationToken ct)
    {
        var tour = await repository.GetByIdAsync(request.TourId, ct);
        if (tour is null)
            return Result.Failure(new Error("E-TOUR-NOT-FOUND", "Guided tour not found."));

        TourAction? action = null;
        if (request.ActionKind is not null && Enum.TryParse<TourActionKind>(request.ActionKind, true, out var actionKind))
        {
            var actionResult = TourAction.Create(actionKind, request.ActionPayload);
            if (actionResult.IsFailure) return actionResult;
            action = actionResult.Value;
        }

        var result = tour.UpdateStep(request.StepId, request.TargetSelector, request.TooltipText, action);
        if (result.IsFailure) return result;

        repository.Update(tour);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class RemoveTourStepCommandHandler(
    IGuidedTourRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<RemoveTourStepCommand>
{
    public async Task<Result> Handle(RemoveTourStepCommand request, CancellationToken ct)
    {
        var tour = await repository.GetByIdAsync(request.TourId, ct);
        if (tour is null)
            return Result.Failure(new Error("E-TOUR-NOT-FOUND", "Guided tour not found."));

        var result = tour.RemoveStep(request.StepId);
        if (result.IsFailure) return result;

        repository.Update(tour);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class ReorderTourStepsCommandHandler(
    IGuidedTourRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<ReorderTourStepsCommand>
{
    public async Task<Result> Handle(ReorderTourStepsCommand request, CancellationToken ct)
    {
        var tour = await repository.GetByIdAsync(request.TourId, ct);
        if (tour is null)
            return Result.Failure(new Error("E-TOUR-NOT-FOUND", "Guided tour not found."));

        var result = tour.ReorderSteps(request.OrderedStepIds);
        if (result.IsFailure) return result;

        repository.Update(tour);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

public sealed class SetTourActiveCommandHandler(
    IGuidedTourRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SetTourActiveCommand>
{
    public async Task<Result> Handle(SetTourActiveCommand request, CancellationToken ct)
    {
        var tour = await repository.GetByIdAsync(request.TourId, ct);
        if (tour is null)
            return Result.Failure(new Error("E-TOUR-NOT-FOUND", "Guided tour not found."));

        if (request.IsActive) tour.Activate(); else tour.Deactivate();

        repository.Update(tour);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- HelpFeedback Handler ---

public sealed class SubmitHelpFeedbackCommandHandler(
    IHelpFeedbackRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<SubmitHelpFeedbackCommand>
{
    public async Task<Result> Handle(SubmitHelpFeedbackCommand request, CancellationToken ct)
    {
        FeedbackReason? reason = request.Reason is not null
            ? Enum.Parse<FeedbackReason>(request.Reason, true)
            : null;

        if (!Enum.TryParse<Language>(request.Language, true, out var language))
            return Result.Failure(new Error("E-INVALID-LANGUAGE", "Invalid language."));

        var result = HelpFeedback.Submit(
            request.FaqEntryId, request.WasHelpful, reason,
            request.Comment, request.SubmittedByRole, language);

        if (result.IsFailure) return Result.Failure(result.Error);

        await repository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- ContentPreference Handler ---

public sealed class UpdateContentPreferenceCommandHandler(
    IContentPreferenceRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UpdateContentPreferenceCommand>
{
    public async Task<Result> Handle(UpdateContentPreferenceCommand request, CancellationToken ct)
    {
        var preference = await repository.GetByUserIdAsync(request.UserId, ct);
        if (preference is null)
            return Result.Failure(new Error("E-PREFERENCE-NOT-FOUND", "Content preference not found."));

        if (request.PreferredLanguage is not null &&
            Enum.TryParse<Language>(request.PreferredLanguage, true, out var lang))
            preference.SetPreferredLanguage(lang);

        if (request.IncludedCategoryIds is not null)
        {
            var result = preference.SetIncludedCategories(request.IncludedCategoryIds);
            if (result.IsFailure) return result;
        }

        if (request.HiddenCategoryIds is not null)
        {
            var result = preference.SetHiddenCategories(request.HiddenCategoryIds);
            if (result.IsFailure) return result;
        }

        repository.Update(preference);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// --- UserRegistered Consumer ---

public sealed class UserRegisteredIntegrationEventHandler(
    IContentPreferenceRepository repository,
    IContentManagementUnitOfWork unitOfWork) : ICommandHandler<UserRegisteredIntegrationEvent>
{
    public async Task<Result> Handle(UserRegisteredIntegrationEvent request, CancellationToken ct)
    {
        if (await repository.ExistsForUserAsync(request.UserId, ct))
            return Result.Success(); // Idempotent

        var preference = ContentPreference.CreateDefault(request.UserId);
        await repository.AddAsync(preference, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
