using FluentAssertions;
using Nexhire.Modules.ContentManagement.Core.Application.Commands;
using Nexhire.Modules.ContentManagement.Core.Application.Ports;
using Nexhire.Modules.ContentManagement.Core.Domain.Aggregates;
using Nexhire.Modules.ContentManagement.Core.Domain.Enums;
using Nexhire.Modules.ContentManagement.Core.Domain.Repositories;
using Nexhire.Modules.ContentManagement.Core.Domain.ValueObjects;
using NSubstitute;

namespace Nexhire.Modules.ContentManagement.Tests.Unit;

public class ArticleCommandHandlerTests
{
    private readonly IArticleRepository _repo = Substitute.For<IArticleRepository>();
    private readonly ICategoryRepository _catRepo = Substitute.For<ICategoryRepository>();
    private readonly IContentManagementUnitOfWork _uow = Substitute.For<IContentManagementUnitOfWork>();

    private static LocalizedContent EnContent => LocalizedContent.Create("Title", "Summary", "<p>Body</p>").Value;

    private Article CreatePublishedArticle()
    {
        var article = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        article.SetPrimaryCategory(Guid.NewGuid());
        article.Publish();
        article.ClearDomainEvents();
        return article;
    }

    [Fact]
    public async Task CreateArticleDraft_Succeeds()
    {
        var handler = new CreateArticleDraftCommandHandler(_repo, _uow);
        var result = await handler.Handle(new CreateArticleDraftCommand(Guid.NewGuid(), "En", "Title", "Summary", "<p>Body</p>"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        result.Value.ArticleId.Should().NotBeEmpty();
        await _repo.Received(1).AddAsync(Arg.Any<Article>(), CancellationToken.None);
    }

    [Fact]
    public async Task CreateArticleDraft_InvalidLanguage_Fails()
    {
        var handler = new CreateArticleDraftCommandHandler(_repo, _uow);
        var result = await handler.Handle(new CreateArticleDraftCommand(Guid.NewGuid(), "Fr", "Title", "Summary", "<p>Body</p>"), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-INVALID-LANGUAGE");
    }

    [Fact]
    public async Task PublishArticle_Succeeds()
    {
        var draft = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        draft.SetPrimaryCategory(Guid.NewGuid());
        _repo.GetByIdAsync(draft.Id, CancellationToken.None).Returns(draft);

        var handler = new PublishArticleCommandHandler(_repo, _uow);
        var result = await handler.Handle(new PublishArticleCommand(draft.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task PublishArticle_NotFound_Fails()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns((Article?)null);
        var handler = new PublishArticleCommandHandler(_repo, _uow);
        var result = await handler.Handle(new PublishArticleCommand(Guid.NewGuid()), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ARTICLE-NOT-FOUND");
    }

    [Fact]
    public async Task PublishArticle_NoCategory_Fails()
    {
        var draft = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        _repo.GetByIdAsync(draft.Id, CancellationToken.None).Returns(draft);

        var handler = new PublishArticleCommandHandler(_repo, _uow);
        var result = await handler.Handle(new PublishArticleCommand(draft.Id), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-ARTICLE-NO-CATEGORY");
    }

    [Fact]
    public async Task ScheduleArticle_PastTime_Fails()
    {
        var draft = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        draft.SetPrimaryCategory(Guid.NewGuid());
        _repo.GetByIdAsync(draft.Id, CancellationToken.None).Returns(draft);

        var handler = new ScheduleArticleCommandHandler(_repo, _uow);
        var result = await handler.Handle(new ScheduleArticleCommand(draft.Id, DateTime.UtcNow.AddHours(-1)), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-SCHEDULE-PAST");
    }

    [Fact]
    public async Task ArchiveArticle_Succeeds()
    {
        var article = CreatePublishedArticle();
        _repo.GetByIdAsync(article.Id, CancellationToken.None).Returns(article);

        var handler = new ArchiveArticleCommandHandler(_repo, _uow);
        var result = await handler.Handle(new ArchiveArticleCommand(article.Id), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BulkArchive_ExceedsLimit_Fails()
    {
        var handler = new BulkArchiveArticlesCommandHandler(_repo, _uow);
        var ids = Enumerable.Range(0, 51).Select(_ => Guid.NewGuid()).ToList();
        var result = await handler.Handle(new BulkArchiveArticlesCommand(ids), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-BULK-LIMIT-EXCEEDED");
    }

    [Fact]
    public async Task PublishDueArticles_NoDueArticles_Succeeds()
    {
        _repo.GetDueForPublicationAsync(Arg.Any<DateTime>(), CancellationToken.None).Returns([]);
        var handler = new PublishDueArticlesCommandHandler(_repo, _uow);
        var result = await handler.Handle(new PublishDueArticlesCommand(), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SetArticleCategory_InvalidCategory_Fails()
    {
        var draft = Article.CreateDraft(Guid.NewGuid(), Language.En, EnContent);
        _repo.GetByIdAsync(draft.Id, CancellationToken.None).Returns(draft);
        _catRepo.GetByIdAsync(Arg.Any<Guid>(), CancellationToken.None).Returns((Category?)null);

        var handler = new SetArticleCategoryCommandHandler(_repo, _catRepo, _uow);
        var result = await handler.Handle(new SetArticleCategoryCommand(draft.Id, Guid.NewGuid()), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("E-CATEGORY-NOT-FOUND");
    }
}

public class HelpFeedbackHandlerTests
{
    private readonly IHelpFeedbackRepository _repo = Substitute.For<IHelpFeedbackRepository>();
    private readonly IContentManagementUnitOfWork _uow = Substitute.For<IContentManagementUnitOfWork>();

    [Fact]
    public async Task SubmitHelpFeedback_Succeeds()
    {
        var handler = new SubmitHelpFeedbackCommandHandler(_repo, _uow);
        var result = await handler.Handle(new SubmitHelpFeedbackCommand(Guid.NewGuid(), true, null, "Nice!", "JobSeeker", "En"), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Any<HelpFeedback>(), CancellationToken.None);
    }

    [Fact]
    public async Task SubmitHelpFeedback_NotHelpful_NoReason_Fails()
    {
        var handler = new SubmitHelpFeedbackCommandHandler(_repo, _uow);
        var result = await handler.Handle(new SubmitHelpFeedbackCommand(Guid.NewGuid(), false, null, null, null, "En"), CancellationToken.None);
        result.IsFailure.Should().BeTrue();
    }
}

public class UserRegisteredHandlerTests
{
    private readonly IContentPreferenceRepository _repo = Substitute.For<IContentPreferenceRepository>();
    private readonly IContentManagementUnitOfWork _uow = Substitute.For<IContentManagementUnitOfWork>();

    [Fact]
    public async Task UserRegistered_CreatesPreference()
    {
        _repo.ExistsForUserAsync(Arg.Any<Guid>(), CancellationToken.None).Returns(false);
        var handler = new UserRegisteredIntegrationEventHandler(_repo, _uow);
        var result = await handler.Handle(new UserRegisteredIntegrationEvent(Guid.NewGuid(), "jobseeker", "test@test.com", DateTime.UtcNow), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).AddAsync(Arg.Any<ContentPreference>(), CancellationToken.None);
    }

    [Fact]
    public async Task UserRegistered_Idempotent_NoOpOnDuplicate()
    {
        _repo.ExistsForUserAsync(Arg.Any<Guid>(), CancellationToken.None).Returns(true);
        var handler = new UserRegisteredIntegrationEventHandler(_repo, _uow);
        var result = await handler.Handle(new UserRegisteredIntegrationEvent(Guid.NewGuid(), "jobseeker", "t@t.com", DateTime.UtcNow), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
        await _repo.DidNotReceive().AddAsync(Arg.Any<ContentPreference>(), CancellationToken.None);
    }
}
