---
title: "Handover Package — BC-12 Content Management"
type: handover-package
bc_id: BC-12
bc_name: Content Management
bc_class: supporting
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/content
---

# Handover Package — BC-12 Content Management

> **Audience:** an AI coding agent. This package owns the **domain design** for the `ContentManagement` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `ContentManagement`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `content_management`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

This BC is **largely independent** (see [[Context_Map]]): it has no Partnership or ACL relationship with any other context. It is a Customer/Supplier *supplier* to BC-3's job-seeker dashboard (it surfaces personalized news) and an event publisher that BC-3 and BC-9 consume. The only thing it consumes is `UserRegistered` from BC-1 — used purely to seed default content-personalization preferences. Treat the module as a standalone CMS that emits facts.

---

## 1. Purpose & scope boundaries

### What this BC is for

Content Management owns the **platform's editorial and self-service-support content**: news articles (rich-text, bilingual, scheduled, categorized, archivable), the FAQ / Help Center (topic- and role-organized entries, multimedia help, guided tours), and the per-user content-personalization preferences that decide which news a job seeker sees on their dashboard. It is a **supporting** subdomain — standard CMS-shaped work — but it is the platform's voice to its users and its first line of self-service support.

### In scope

The `ContentManagement` module is responsible for:

- **News articles** — create/edit with rich-text + embedded media, immediate publish, **scheduled** publish at a future time, automatic draft save, bilingual (EN / Bangla) independent content versions (`US-3.7.1-01`, `US-3.7.1-02`).
- **Categorization & tagging** — one primary admin-defined category plus free-form bilingual tags per article; category required before publish; tag auto-suggest from existing tags (`US-3.7.1-03`).
- **Article lifecycle** — `Draft → Scheduled → Published → Unpublished → Archived`, with restore-from-archive and bulk archive (`US-3.7.1-04`).
- **Browse & filter news** — paginated published feed, newest-first, filter by category and tag with AND logic across filter types, clear filters, language-scoped display (`US-3.7.1-05`).
- **Search news archive** — case-insensitive full-text over title + body, date-range filter, title matches ranked first, paginated, language-scoped; archive search includes published, unpublished, and archived articles but never drafts (`US-3.7.1-06`).
- **Dashboard personalization** — a `ContentPreference` per user: profile-attribute-driven category matching, manual category include/hide overrides, default fallback to recent global news, language preference (`US-3.7.1-07`).
- **FAQ / Help entries** — create/edit with rich text, draft/publish states, bilingual independent versions, no built-in version history (overwrite on edit) (`US-3.7.2-01`).
- **Help organization** — assign one-or-more admin-defined topics and one-or-more visible roles per entry; flat topic list management; role-based filtering of help content (`US-3.7.2-02`).
- **Help search** — keyword search over title + content, role-filtered, title-first relevance ranking, case-insensitive, language-scoped, excludes drafts (`US-3.7.2-03`).
- **Context-sensitive help** — map help entries to page/form context keys; serve the entries for a given context key + role + language (`US-3.7.2-04`).
- **Help feedback** — anonymous "Was this helpful?" Yes/No, optional reason + comment, stored with timestamp/role/language; admin aggregation dashboard (`US-3.7.2-05`).
- **Multimedia help content** — embedded video (YouTube link or uploaded file reference), interactive step guides, optional transcripts/captions (`US-3.7.2-06`).
- **Guided tours** — admin-authored ordered steps (CSS selector + tooltip + optional action), role/audience targeting, bilingual versions (`US-3.7.2-07`).
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs (or external systems) and are reached via the contracts in §9, or are simply not this module's concern:

- **Credentials, login, sessions, access tokens, role claims** → BC-1 IAM/UAM. The content API trusts the access token BC-1 issued; the editor/admin role check reads a claim from the token.
- **The user's profile attributes (sector, location, job interests)** → BC-3 JobSeeker Profile. Dashboard personalization needs these, but this module does **not** store the profile. It calls the `JobSeekerProfileQueryApi` port (§9.2) to read the seeker's personalization attributes at query time, or accepts them as request parameters. It keeps only the user's *content* preferences (which categories to include/hide, language).
- **Object storage / CDN for the actual image, video, and media bytes** → external, reached via the `MediaStorage` port. This module stores only media *references* (storage keys, URLs, MIME, size).
- **Video transcoding** → external, triggered by the storage adapter. This module records that a transcript/caption exists; it does not transcode.
- **The rich-text WYSIWYG editor** → a frontend concern. This module stores and serves the produced rich-text payload (sanitized HTML or a structured document representation) — it does not render an editor.
- **Sending the news digest email / in-app news notification** → BC-9 Notification. This module emits `ArticlePublished` / `ArticleArchived`; BC-9 decides who gets a digest and delivers it.
- **The background scheduler infrastructure itself** → host infrastructure. This module owns the *scheduling decision* (which article publishes when) and a domain-level `due` check; the host runs a recurring worker that calls the module's `PublishDueArticlesCommand`. The package specifies the worker contract but assumes the host provides the timer.
- **Reporting / analytics on help feedback or article reach** → BC-10 Reporting. This module emits `HelpFeedbackReceived` and `FAQPublished`; BC-10 builds dashboards. The in-module admin feedback aggregation (`US-3.7.2-05 AC-06`) is a simple operational read-model over this module's own feedback table — it is **not** LMIS analytics.
- **Rendering the guided tour on the page / running the tour engine** → a frontend concern. This module owns the tour *definition* (steps, selectors, targeting); the SPA owns playback.
- **A workflow / approval pipeline for content** — the stories explicitly assume no approval workflow in MVP. Any editor/admin may publish directly.
- **Taxonomy of article categories as a shared platform vocabulary** — article categories and help topics are **owned and admin-managed inside this module** (they are CMS concerns), distinct from BC-11's skill/occupation/industry taxonomy. Do not call BC-11. See the boundary note below.

### Boundary note — content categories vs. the platform taxonomy (teaching point)

A reasonable instinct is to source article categories and help topics from BC-11 Administrators Configuration, since BC-11 owns "taxonomies." This package deliberately does **not**. BC-11's taxonomy is the *skill / occupation / industry* vocabulary that fuels matching and search — a Published Language consumed by BC-3, BC-4, BC-7, BC-8. Article categories ("Labor Law Updates", "Platform News") and help topics ("Laws", "Regulations", "Contract Types") are *editorial* classifications with no role outside the CMS. Folding them into BC-11 would couple an editorial concern to the matching vocabulary's release cycle for no benefit. They live here, as locally-owned `Category` and `Topic` aggregates. Discuss with the class: *what makes two "taxonomies" actually the same bounded concept?* — the answer is shared consumers and shared invariants, neither of which holds here.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Article** | The `Article` aggregate — a news/update item. Root of the news side of the BC. |
| **Localized Content** | The `LocalizedContent` value object — title + rich body + summary for **one** language. An article holds one per language it has been authored in. |
| **Language** | `En` or `Bn` (Bangla). Every authored content unit is single-language; EN and Bangla versions are independent. |
| **Article Status** | `Draft`, `Scheduled`, `Published`, `Unpublished`, `Archived`. |
| **Schedule** | A `PublicationSchedule` VO — a future UTC instant at which a `Scheduled` article auto-publishes. |
| **Category** | The `Category` aggregate — an admin-defined editorial classification. An article has **exactly one** primary category. |
| **Tag** | A free-form label on an article, per language. Stored normalized (lower-cased, trimmed) for case-insensitive matching. Not an aggregate — a child value of `Article`. |
| **Media Reference** | A `MediaReference` VO — a pointer to a stored image/video (`StorageKey`, `Url`, `MimeType`, `SizeBytes`, `Kind`). The bytes live in external storage. |
| **FAQ Entry** | The `FaqEntry` aggregate — a question/answer help item. Root of the help side of the BC. |
| **Topic** | The `Topic` aggregate — an admin-defined help classification (e.g. "Laws"). A FAQ entry may have several. |
| **Visible Roles** | The set of platform roles that may see a FAQ entry: `JobSeeker`, `Employer`, `Administrator`, or `All`. |
| **Help Article** | A `FaqEntry` whose `Kind` is `HelpArticle` rather than `Faq` — same aggregate, may carry multimedia. |
| **Multimedia Block** | A child value of `FaqEntry` — an embedded video or interactive step guide, optionally with a transcript reference. |
| **Context Key** | A stable string identifying a page/form (e.g. `job-posting.create`). Help entries are mapped to context keys to power context-sensitive help. |
| **Guided Tour** | The `GuidedTour` aggregate — an ordered sequence of `TourStep`s with role/audience targeting and a language. |
| **Tour Step** | A child entity of `GuidedTour`: `Order`, `TargetSelector` (CSS selector), `TooltipText`, optional `Action`. |
| **Content Preference** | The `ContentPreference` aggregate — one per `UserId`. Holds preferred language + manual category include/hide overrides for dashboard news personalization. |
| **Help Feedback** | The `HelpFeedback` aggregate — one anonymous feedback submission against a `FaqEntry`. |
| **Feedback Reason** | `Unclear`, `Incomplete`, `Incorrect`, `Other` — only set when the feedback verdict is "not helpful". |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `ContentManagement`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** the module runs a **scheduled-publication worker** (a hosted background service) at a fixed interval (e.g. every 60 s) that issues `PublishDueArticlesCommand` to auto-publish `Scheduled` articles whose `PublishAtUtc` has passed. It also runs the standard outbox relay. The "is due" decision lives in the domain (`Article.IsDueForPublication(now)`); the worker owns only the timer.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **seven aggregates**: `Article`, `Category`, `FaqEntry`, `Topic`, `GuidedTour`, `ContentPreference`, plus the small `HelpFeedback` aggregate (kept separate because it is written by anonymous users on a different lifecycle from the content it targets). (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: Article

**Aggregate root.** Identity: `ArticleId` (strongly-typed id wrapping `uuid`).

| Member | Type | Notes |
|---|---|---|
| `Id` | `ArticleId` | |
| `AuthorUserId` | `uuid` | BC-1 identity of the content editor. Set at creation. Immutable. |
| `Status` | `ArticleStatus` | enum: `Draft`, `Scheduled`, `Published`, `Unpublished`, `Archived` |
| `PrimaryCategoryId` | `CategoryId?` | nullable while `Draft`; **required** before publish (§6 invariant 2) |
| `Localizations` | `map<Language, LocalizedContent>` | one entry per authored language; at least one required to publish |
| `Tags` | `list<ArticleTag>` | child value list — `(Language, NormalizedLabel, DisplayLabel)` |
| `Media` | `list<MediaReference>` | embedded images/media |
| `Schedule` | `PublicationSchedule?` | non-null iff `Status == Scheduled` |
| `PublishedOnUtc` | `datetime?` | set when first transitions to `Published` |
| `PreviousStatus` | `ArticleStatus?` | remembered on archive, so restore returns to the right state |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.2 Aggregate: Category

**Aggregate root.** Identity: `CategoryId`. Admin-managed editorial classification for articles.

| Member | Type | Notes |
|---|---|---|
| `Id` | `CategoryId` | |
| `Names` | `map<Language, string>` | display name per language |
| `Slug` | `string` | URL-safe, lower-cased, unique |
| `IsActive` | `bool` | inactive categories are hidden from editors but not deleted |

### 5.3 Aggregate: FaqEntry

**Aggregate root.** Identity: `FaqEntryId`. Covers both plain FAQ entries and richer help articles (`Kind` discriminates).

| Member | Type | Notes |
|---|---|---|
| `Id` | `FaqEntryId` | |
| `Kind` | `FaqEntryKind` | enum: `Faq`, `HelpArticle` |
| `Status` | `ContentStatus` | enum: `Draft`, `Published` (no archive lifecycle — overwrite-on-edit per `US-3.7.2-01`) |
| `Localizations` | `map<Language, FaqContent>` | `FaqContent` = `Question` + rich `Answer`; independent per language |
| `TopicIds` | `list<TopicId>` | zero-or-more topics |
| `VisibleRoles` | `VisibleRoleSet` | VO — which roles may see this entry |
| `ContextKeys` | `list<string>` | page/form context keys for context-sensitive help |
| `MultimediaBlocks` | `list<MultimediaBlock>` | child values — videos / step guides (only meaningful for `HelpArticle`) |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.4 Aggregate: Topic

**Aggregate root.** Identity: `TopicId`. Admin-managed help classification.

| Member | Type | Notes |
|---|---|---|
| `Id` | `TopicId` | |
| `Names` | `map<Language, string>` | display name per language |
| `Slug` | `string` | URL-safe, unique |
| `IsActive` | `bool` | |

### 5.5 Aggregate: GuidedTour

**Aggregate root.** Identity: `GuidedTourId`. An ordered onboarding walkthrough.

| Member | Type | Notes |
|---|---|---|
| `Id` | `GuidedTourId` | |
| `Language` | `Language` | a tour is single-language; EN/Bangla are separate tours |
| `Name` | `string` | |
| `Description` | `string` | |
| `TargetAudience` | `AudienceSet` | VO — `NewUsers`, `JobSeekers`, `Recruiters`, `Administrators` |
| `Steps` | `list<TourStep>` | child entities, ordered; identity local to the aggregate |
| `IsActive` | `bool` | inactive tours are not served to the SPA |

- `TourStep` — `TourStepId`, `Order` (`int`), `TargetSelector` (`string`, CSS selector), `TooltipText` (`string`), `Action` (`TourAction?` — VO: `Kind` ∈ `None`/`Click`/`Navigate`, optional payload).

### 5.6 Aggregate: ContentPreference

**Aggregate root.** Identity: `ContentPreferenceId` (one per `UserId`). Drives dashboard news personalization.

| Member | Type | Notes |
|---|---|---|
| `Id` | `ContentPreferenceId` | |
| `UserId` | `uuid` | BC-1 identity. Unique. Immutable. |
| `PreferredLanguage` | `Language` | default `En` |
| `IncludedCategoryIds` | `list<CategoryId>` | manual "show only these" overrides; empty = use profile-attribute matching |
| `HiddenCategoryIds` | `list<CategoryId>` | manual "never show these" overrides |
| `UpdatedOnUtc` | `datetime` | |

### 5.7 Aggregate: HelpFeedback

**Aggregate root.** Identity: `HelpFeedbackId`. One anonymous feedback submission. Append-only — never edited or user-deleted.

| Member | Type | Notes |
|---|---|---|
| `Id` | `HelpFeedbackId` | |
| `FaqEntryId` | `FaqEntryId` | the entry the feedback is about (plain `uuid` reference inside this module's schema, FK allowed since same module) |
| `WasHelpful` | `bool` | the Yes/No verdict |
| `Reason` | `FeedbackReason?` | set only when `WasHelpful == false` |
| `Comment` | `string?` | optional, max 2000 chars |
| `SubmittedByRole` | `string?` | role of the submitter if logged in; null if fully anonymous |
| `Language` | `Language` | language of the content viewed |
| `SubmittedOnUtc` | `datetime` | |

### 5.8 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `LocalizedContent` | `Title`, `Summary`, `BodyRichText` | `Title` non-empty ≤ 200; `Summary` ≤ 500; `BodyRichText` non-empty; HTML is sanitized on store (strip script/style/event handlers) |
| `FaqContent` | `Question`, `AnswerRichText` | `Question` non-empty ≤ 300; `AnswerRichText` non-empty, sanitized |
| `ArticleTag` | `Language`, `NormalizedLabel`, `DisplayLabel` | `DisplayLabel` non-empty ≤ 50; `NormalizedLabel` = `DisplayLabel` trimmed + lower-cased |
| `MediaReference` | `StorageKey`, `Url`, `MimeType`, `SizeBytes` (`int64`), `Kind` (`Image`/`Video`), `TranscriptUrl?` | `StorageKey` non-empty; `SizeBytes > 0`; images ≤ 5 MB, videos ≤ 500 MB; image MIME ∈ {jpeg, png, gif}, video MIME ∈ {mp4, webm} |
| `PublicationSchedule` | `PublishAtUtc` | must be **strictly in the future** at the moment it is set (`E-SCHEDULE-PAST`) |
| `VisibleRoleSet` | `Roles` (set of `JobSeeker`/`Employer`/`Administrator`/`All`) | non-empty; if `All` present it is the only member |
| `AudienceSet` | `Audiences` (set of `NewUsers`/`JobSeekers`/`Recruiters`/`Administrators`) | non-empty |
| `MultimediaBlock` | `BlockKind` (`Video`/`StepGuide`), `Media` (`MediaReference?`), `Steps` (`list<GuideStep>`) | `Video` ⇒ `Media` non-null & `Kind == Video`; `StepGuide` ⇒ `Steps` non-empty |
| `GuideStep` | `Order`, `Caption`, `Image` (`MediaReference?`) | `Caption` non-empty; `Image.Kind == Image` when present |
| `TourAction` | `Kind` (`None`/`Click`/`Navigate`), `Payload?` | `Navigate` ⇒ `Payload` (target route) non-empty |
| `Language` | `Value` | enum: `En`, `Bn` |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 Article — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static CreateDraft(authorUserId, language, content)` | starts in `Draft` with one `LocalizedContent`. | — |
| `SetLocalization(language, content)` | adds/replaces the content for one language. EN and Bangla are independent — editing one never touches the other. | — |
| `RemoveLocalization(language)` | cannot remove the last remaining localization. | — |
| `SetPrimaryCategory(categoryId)` | exactly one primary category; replaces any existing. | — |
| `SetTags(language, tags)` | replaces the tag set for one language; labels normalized; deduped. | — |
| `AddMedia(mediaReference)` / `RemoveMedia(storageKey)` | media reference validated by the VO. | — |
| `Publish()` | **requires** a primary category (`E-ARTICLE-NO-CATEGORY`) and ≥ 1 localization. Legal from `Draft`, `Scheduled`, `Unpublished`. → `Published`; sets `PublishedOnUtc` if not already set; clears `Schedule`. Idempotent if already `Published`. | `ArticlePublished` |
| `Schedule(publicationSchedule)` | legal from `Draft` or `Scheduled`. Requires a primary category + ≥ 1 localization. `PublishAtUtc` must be future. → `Scheduled`. | `ArticleScheduled` |
| `CancelSchedule()` | legal only from `Scheduled`. → `Draft`; clears `Schedule`. | — |
| `MarkPublishedBySchedule()` | called only by the scheduler flow when `now >= Schedule.PublishAtUtc`. `Scheduled → Published`. | `ArticlePublished` |
| `IsDueForPublication(nowUtc)` | query: `Status == Scheduled && Schedule.PublishAtUtc <= nowUtc`. | — |
| `Unpublish()` | legal only from `Published`. → `Unpublished` (removed from feeds, kept in DB). | `ArticleArchived` *(see note below)* |
| `Archive()` | legal from `Unpublished` or `Published` (`US-3.7.1-04 AC-01`+`AC-02` allow either path; from `Published` it implicitly unpublishes first). Records `PreviousStatus`. → `Archived`. | `ArticleArchived` |
| `RestoreFromArchive()` | legal only from `Archived`. → `PreviousStatus` (`Published` or `Unpublished`/`Draft`). If restored to `Published`, raise `ArticlePublished` again. | `ArticlePublished` *(only if restored to Published)* |

> **Event-mapping note.** The [[Event_Catalog]] gives BC-12 only `ArticleArchived` for "no longer on the active feed". This package raises `ArticleArchived` on **both** `Unpublish()` and `Archive()` because, from a downstream consumer's point of view (BC-3 dashboard, BC-9 digest), both mean "stop showing this article". The integration-event payload carries the precise resulting `Status` so consumers can tell them apart. This is a deliberate published-language decision — discuss in §Appendix.

### 6.2 Core invariants (must always hold)

1. **Status machine** is fixed: `Draft → {Scheduled, Published}`; `Scheduled → {Published, Draft}`; `Published → {Unpublished, Archived}`; `Unpublished → {Published, Archived}`; `Archived → PreviousStatus`. No other transition is legal.
2. **Publish gate**: an article can be `Published` or `Scheduled` only if it has a `PrimaryCategoryId` **and** at least one `LocalizedContent`. (`US-3.7.1-03 AC-04`.)
3. **Schedule consistency**: `Schedule` is non-null **iff** `Status == Scheduled`. `Schedule.PublishAtUtc` was strictly future at the time it was set.
4. **Localization independence**: EN and Bangla content are stored separately; a mutation to one language's `LocalizedContent` or tag set never mutates another's. At least one localization must always exist after `CreateDraft`.
5. **One primary category** per article; it must reference an existing, active `Category` (validated by the handler via the repository — cross-aggregate, so not enforced inside `Article`).
6. **`PublishedOnUtc`** is set exactly once — on the first transition into `Published` — and never cleared.
7. **FAQ entry**: `Status` is only `Draft` or `Published`; there is no archive lifecycle and no version history (overwrite on edit, per `US-3.7.2-01`).
8. **`VisibleRoleSet`** is never empty; `All` is mutually exclusive with specific roles.
9. **Topic / Category deletion**: a `Topic` may be deleted only if **no** `FaqEntry` references it; a `Category` may be deleted only if no `Article` references it (`US-3.7.2-02 AC-06`). Enforced by the handler — it is a cross-aggregate rule.
10. **Guided tour** steps are contiguously ordered starting at 1; `GuidedTour` is single-language.
11. **`ContentPreference`**: `UserId` is unique and immutable; a category id may not appear in both `IncludedCategoryIds` and `HiddenCategoryIds`.
12. **`HelpFeedback`** is append-only: never updated, never user-deleted; `Reason` is non-null **iff** `WasHelpful == false`.

### 6.3 FaqEntry — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static CreateDraft(kind, language, content)` | starts `Draft` with one `FaqContent`. | — |
| `SetLocalization(language, content)` / `RemoveLocalization(language)` | independent per language; cannot remove the last. | — |
| `SetTopics(topicIds)` | replaces topic set; deduped. | — |
| `SetVisibleRoles(visibleRoleSet)` | VO enforces non-empty + `All` exclusivity. | — |
| `SetContextKeys(keys)` | replaces the context-key list; trimmed, deduped. | — |
| `AddMultimediaBlock(block)` / `RemoveMultimediaBlock(index)` | only allowed when `Kind == HelpArticle`. | — |
| `Publish()` | requires ≥ 1 localization. `Draft → Published`. Idempotent. | `FAQPublished` |
| `Unpublish()` | `Published → Draft`. | — |

### 6.4 Category / Topic — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Create(names, slug)` | `slug` URL-safe, unique (uniqueness checked by handler). At least one language name. | — |
| `Rename(language, name)` / `SetSlug(slug)` | slug re-uniqueness checked by handler. | — |
| `Deactivate()` / `Activate()` | toggles `IsActive`. | — |
| `EnsureDeletable(referenceCount)` | returns failure `E-CATEGORY-IN-USE` / `E-TOPIC-IN-USE` if `referenceCount > 0`. | — |

### 6.5 GuidedTour — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Create(language, name, description, audience)` | non-empty name; audience non-empty. | — |
| `AddStep(targetSelector, tooltipText, action)` | appends at `Order = Steps.Count + 1`. | — |
| `ReorderSteps(orderedStepIds)` | must be a permutation of existing step ids; re-numbers contiguously from 1. | — |
| `UpdateStep(stepId, ...)` / `RemoveStep(stepId)` | step must exist; remove re-numbers remaining steps. | — |
| `Activate()` / `Deactivate()` | toggles `IsActive`. | — |

### 6.6 ContentPreference — behaviors

| Method | Rules | Event |
|---|---|---|
| `static CreateDefault(userId)` | `PreferredLanguage = En`, empty include/hide lists. Created on `UserRegistered`. | — |
| `SetPreferredLanguage(language)` | — | — |
| `SetIncludedCategories(categoryIds)` | a category may not also be in the hidden list (invariant 11). | — |
| `SetHiddenCategories(categoryIds)` | symmetric check. | — |
| `DismissForSession(...)` | **not modeled here** — per `US-3.7.1-07 AC-04` dismissal is session-only and client-side; the module does not persist it. |

### 6.7 HelpFeedback — behaviors

| Method | Rules | Domain event raised |
|---|---|---|
| `static Submit(faqEntryId, wasHelpful, reason, comment, role, language)` | `reason` required when `!wasHelpful`, forbidden when `wasHelpful`; `comment` ≤ 2000. | `HelpFeedbackReceived` |

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `DashboardNewsSelector`

```
Select(preference: ContentPreference,
       profileAttributes: SeekerPersonalizationAttributes?,    // sector, location, job interests; null if profile incomplete
       candidatePublishedArticles: list<ArticleSummary>,
       maxItems: int) -> list<ArticleId>
```

Implements `US-3.7.1-07`:

- If `preference.IncludedCategoryIds` is non-empty → return newest articles whose `PrimaryCategoryId` is in that set.
- Else if `profileAttributes` is present → match candidate articles whose category/tags align with the seeker's sector / location / job interests (attribute-based, no collaborative filtering).
- Else (profile incomplete and no manual includes) → **default fallback**: newest published articles globally (`AC-02`).
- In all branches, exclude any article whose `PrimaryCategoryId` is in `preference.HiddenCategoryIds`, and prefer `preference.PreferredLanguage` content.
- Sort newest-first, take `maxItems`.

`ArticleSummary` and `SeekerPersonalizationAttributes` are plain domain DTOs defined in `Domain` (the latter mirrors the shape returned by the `JobSeekerProfileQueryApi` port in §9.2).

### 7.2 `ContentSearchRanker`

```
Rank(query: string, matches: list<SearchableContent>) -> list<ScoredContent>
```

Shared by news-archive search (`US-3.7.1-06`) and help search (`US-3.7.2-03`). Pure ranking only — the *matching* (case-insensitive substring/`ILIKE`-style match over title+body, language scoping, role filtering, date-range filtering, draft exclusion) is done by the repository query; this service takes the matched rows and orders them: **title matches rank above body-only matches** (`US-3.7.1-06 AC-03`, `US-3.7.2-03 AC-04`). Ties broken by recency (news) or by topic order (help).

### 7.3 `ContextHelpResolver`

```
Resolve(contextKey: string, viewerRole: string, language: Language, candidates: list<FaqEntry>) -> list<FaqEntryId>
```

Implements `US-3.7.2-04`: from FAQ entries already filtered to `Status == Published`, returns those whose `ContextKeys` contains `contextKey`, that are visible to `viewerRole` (or `All`), and that have a localization in `language`. Ordering: `HelpArticle` kind before plain `Faq`, then most-recently-updated first.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. Other modules depend on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `ArticlePublishedIntegrationEvent` | `Article.Publish()` or `MarkPublishedBySchedule()` or restore-to-published succeeds | `ArticleId`, `Title` (preferred-language or EN fallback), `PrimaryCategoryId`, `CategorySlug`, `Tags` (`list<string>`), `Languages` (`list<string>` of authored langs), `PublishedOnUtc`, `OccurredOnUtc` |
| `ArticleScheduledIntegrationEvent` | `Article.Schedule()` succeeds | `ArticleId`, `PublishAtUtc`, `OccurredOnUtc` |
| `ArticleArchivedIntegrationEvent` | `Article.Unpublish()` or `Article.Archive()` succeeds | `ArticleId`, `ResultingStatus` (`"Unpublished"` \| `"Archived"`), `OccurredOnUtc` |
| `FAQPublishedIntegrationEvent` | `FaqEntry.Publish()` succeeds | `FaqEntryId`, `Kind` (`"Faq"` \| `"HelpArticle"`), `TopicIds` (`list<uuid>`), `OccurredOnUtc` |
| `HelpFeedbackReceivedIntegrationEvent` | `HelpFeedback.Submit()` succeeds | `HelpFeedbackId`, `FaqEntryId`, `WasHelpful` (`bool`), `Reason` (`string?`), `OccurredOnUtc` |

Consumers (for context only — you do not code them): **BC-3 JobSeeker Profile** consumes `ArticlePublished` and `ArticleArchived` to refresh the dashboard news widget; **BC-9 Notification** consumes `ArticlePublished` (news digest opt-ins), `ArticleScheduled` (to know when a future publish fires), and `ArticleArchived`; **BC-10 Reporting** consumes `FAQPublished` and `HelpFeedbackReceived` for content analytics. Per the [[Event_Catalog]] BC-12 owns exactly five events — this package keeps to that set.

### 8.2 Internal domain events (NOT published outside the module)

This module has very little in-module reactive logic, so the internal-event list is short. Use internal `DomainEvent`s only where one aggregate change must trigger another in the same module — e.g. there are none required by the current stories. If you add in-module reactions later (e.g. an `ArticleUnpublished` internal event that recomputes a "trending" cache), keep them off the outbox. **Do not** put `ArticleScheduled`-vs-`ArticlePublished` bookkeeping on the outbox twice.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

| Integration event | From | Payload you receive | Your reaction |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` | BC-1 IAM/UAM | `UserId`, `Role` (`"jobseeker"` \| `"employer"` \| `"administrator"`), `Email`, `CreatedAtUtc` | Create a `ContentPreference` via `ContentPreference.CreateDefault(userId)` — default language `En`, empty include/hide lists. This is the *only* upstream event this module consumes (per [[Event_Catalog]]). Skip if a preference for that `UserId` already exists. **Idempotent.** |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox) — and the handler must also treat "preference already exists for this user" as a no-op. Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: JobSeekerProfileQueryApi  (provided by BC-3 JobSeeker Profile — read personalization attributes for dashboard news matching)
                                // Reproduced here verbatim so this package is self-contained.
                                // BC-3 exposes this on its public contract surface.
  GetPersonalizationAttributes(userId: uuid) -> SeekerPersonalizationAttributes?
      // returns null if the profile does not exist or is not complete enough to personalize

  SeekerPersonalizationAttributes {
    UserId: uuid,
    Sector: string?,                   // e.g. "IT", "Manufacturing"
    Location: string?,                 // district / city string
    JobInterests: list<string>         // free-form interest labels
  }

Port: MediaStorage              (external object storage / CDN — stores & serves the actual image/video bytes)
                                // This module keeps only MediaReference; it never holds bytes.
  Store(content: bytes, fileName: string, mimeType: string) -> Result<MediaReference>
  Delete(storageKey: string)    -> void
  GetPublicUrl(storageKey: string) -> Result<string>     // CDN-backed public URL for an already-stored object
```

For the exercise, `Infrastructure` may provide **stub adapters** for `JobSeekerProfileQueryApi` (returns a canned attribute set, or `null`) and `MediaStorage` (writes to local disk / in-memory, returns fake CDN URLs) so the module runs standalone. Keep the port shapes exactly as above so real adapters drop in later.

> **Design note.** Dashboard personalization (`US-3.7.1-07`) needs the seeker's profile attributes, which BC-3 owns. Two options: (a) call `JobSeekerProfileQueryApi` synchronously at dashboard-query time, or (b) have BC-12 subscribe to BC-3 profile events and keep a local copy. This package chooses **(a)** — a synchronous query — because the data is read rarely (only on dashboard load), is non-critical (graceful fallback to global news on `null`), and copying profile state into a CMS would bloat this BC's model for little gain. The API endpoint may also simply accept the attributes as request parameters if the SPA already has them; the port exists for the server-driven case.

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: ContentManagementPublicApi
  GetDashboardNews(userId: uuid, maxItems: int) -> list<DashboardArticleDto>
      // used by BC-3's dashboard to render the personalized news widget server-side, if it prefers
      // a single call over consuming events. Honors the user's stored ContentPreference.
  GetPublishedArticle(articleId: uuid, language: string) -> PublishedArticleDto?
      // generic published-article lookup (e.g. for deep links from a notification)

DashboardArticleDto {
  ArticleId: uuid, Title: string, Summary: string, CategorySlug: string,
  Tags: list<string>, PublishedOnUtc: datetime
}
PublishedArticleDto {
  ArticleId: uuid, Title: string, Summary: string, BodyHtml: string,
  CategorySlug: string, Tags: list<string>, Language: string, PublishedOnUtc: datetime
}
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `CreateArticleDraftCommand` | US-3.7.1-01 | `Article.CreateDraft(authorUserId, language, content)` → persist. Returns `ArticleId`. Backs both explicit "save draft" and auto-save (`AC-05`). |
| `UpdateArticleContentCommand` | US-3.7.1-01 | Load → `SetLocalization` for the supplied language → persist. EN/Bangla independent (`AC-04`). |
| `RemoveArticleLocalizationCommand` | US-3.7.1-01 | Load → `RemoveLocalization` (fails if last) → persist. |
| `SetArticleCategoryCommand` | US-3.7.1-03 | Validate category exists & `IsActive` via `CategoryRepository` → `SetPrimaryCategory` → persist. |
| `SetArticleTagsCommand` | US-3.7.1-03 | Load → `SetTags(language, tags)` (normalizes, dedupes) → persist. |
| `AddArticleMediaCommand` | US-3.7.1-01 | `MediaStorage.Store` → `AddMedia(mediaReference)` → persist. |
| `RemoveArticleMediaCommand` | US-3.7.1-01 | Load → `RemoveMedia` → `MediaStorage.Delete` → persist. |
| `PublishArticleCommand` | US-3.7.1-01 | Load → `Publish()` (enforces category + localization gate) → persist; `ArticlePublished` to outbox. |
| `ScheduleArticleCommand` | US-3.7.1-02 | Build `PublicationSchedule.Create(publishAtUtc)` (must be future) → `Schedule(...)` → persist; `ArticleScheduled` to outbox. |
| `CancelArticleScheduleCommand` | US-3.7.1-02 | Load → `CancelSchedule()` → persist. |
| `RescheduleArticleCommand` | US-3.7.1-02 | Load → `CancelSchedule()` then `Schedule(newSchedule)` → persist (`AC-05`). |
| `PublishDueArticlesCommand` | US-3.7.1-02 | **Called by the scheduled-publication worker.** Query articles where `Status == Scheduled && Schedule.PublishAtUtc <= now` → for each `MarkPublishedBySchedule()` → persist; one `ArticlePublished` per article to outbox. Idempotent — already-published articles are skipped by the query. |
| `UnpublishArticleCommand` | US-3.7.1-04 | Load → `Unpublish()` → persist; `ArticleArchived{ResultingStatus="Unpublished"}` to outbox. |
| `ArchiveArticleCommand` | US-3.7.1-04 | Load → `Archive()` → persist; `ArticleArchived{ResultingStatus="Archived"}` to outbox. |
| `BulkArchiveArticlesCommand` | US-3.7.1-04 | Validate ≤ 50 ids (`E-BULK-LIMIT-EXCEEDED`) → load each → `Archive()` → persist all in one transaction; one outbox event per article. |
| `RestoreArticleFromArchiveCommand` | US-3.7.1-04 | Load → `RestoreFromArchive()` → persist; `ArticlePublished` to outbox only if restored to `Published`. |
| `CreateCategoryCommand` / `UpdateCategoryCommand` / `DeactivateCategoryCommand` | US-3.7.1-03 | admin-only; slug uniqueness checked via repo. |
| `DeleteCategoryCommand` | US-3.7.1-03 | Count referencing articles via repo → `EnsureDeletable(count)` → delete or `E-CATEGORY-IN-USE`. |
| `CreateFaqEntryCommand` | US-3.7.2-01 | `FaqEntry.CreateDraft(kind, language, content)` → persist. |
| `UpdateFaqEntryContentCommand` | US-3.7.2-01 | Load → `SetLocalization` (overwrite-on-edit, no version history) → persist. |
| `SetFaqTopicsCommand` | US-3.7.2-02 | Validate topic ids exist → `SetTopics` → persist. |
| `SetFaqVisibleRolesCommand` | US-3.7.2-02 | Load → `SetVisibleRoles(VisibleRoleSet.Create(roles))` → persist. |
| `SetFaqContextKeysCommand` | US-3.7.2-04 | Load → `SetContextKeys` → persist. |
| `AddFaqMultimediaBlockCommand` | US-3.7.2-06 | `MediaStorage.Store` for video/images → `AddMultimediaBlock` (only on `HelpArticle`) → persist. |
| `PublishFaqEntryCommand` | US-3.7.2-01 | Load → `Publish()` → persist; `FAQPublished` to outbox. |
| `UnpublishFaqEntryCommand` | US-3.7.2-01 | Load → `Unpublish()` → persist. |
| `CreateTopicCommand` / `UpdateTopicCommand` / `DeleteTopicCommand` | US-3.7.2-02 | admin-only; delete enforces `EnsureDeletable` → `E-TOPIC-IN-USE`. |
| `CreateGuidedTourCommand` | US-3.7.2-07 | `GuidedTour.Create(language, name, description, audience)` → persist. |
| `AddTourStepCommand` / `UpdateTourStepCommand` / `RemoveTourStepCommand` / `ReorderTourStepsCommand` | US-3.7.2-07 | Load → mutate steps via root → persist. |
| `SetTourActiveCommand` | US-3.7.2-07 | Load → `Activate()`/`Deactivate()` → persist. |
| `SubmitHelpFeedbackCommand` | US-3.7.2-05 | **Anonymous-allowed** — no access token required. Validate `reason` presence rule → `HelpFeedback.Submit(...)` → persist; `HelpFeedbackReceived` to outbox. |
| `UpdateContentPreferenceCommand` | US-3.7.1-07 | Load preference by `userId` → `SetPreferredLanguage` / `SetIncludedCategories` / `SetHiddenCategories` → persist. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetArticleQuery` | US-3.7.1-01 | `ArticleDto` (all localizations, category, tags, media, status, schedule) — editor-facing, any status. |
| `BrowseNewsQuery` | US-3.7.1-05 | `PagedResult<NewsFeedItemDto>` — **published only**, newest-first; filter params: `categorySlug?`, `tags[]` (AND across category+tags), `language`, `page`, `pageSize`. |
| `SearchNewsArchiveQuery` | US-3.7.1-06 | `PagedResult<NewsSearchResultDto>` — case-insensitive title/body match; `dateFrom?`/`dateTo?`; `language`; **includes Published, Unpublished, Archived; excludes Draft**; title matches ranked first (via `ContentSearchRanker`); helpful "no results" handled by empty page + flag. |
| `GetDashboardNewsQuery` | US-3.7.1-07 | `list<DashboardArticleDto>` — loads `ContentPreference`, optionally calls `JobSeekerProfileQueryApi`, runs `DashboardNewsSelector`. |
| `GetCategoriesQuery` | US-3.7.1-03 | `list<CategoryDto>` — for editor category picker and feed filter sidebar. |
| `GetFaqEntryQuery` | US-3.7.2-01 | `FaqEntryDto` — admin-facing, any status. |
| `BrowseHelpCenterQuery` | US-3.7.2-02 | `list<HelpEntrySummaryDto>` grouped by topic — **published only**, filtered to viewer role (`viewerRole` param) + `language`. |
| `SearchHelpContentQuery` | US-3.7.2-03 | `list<HelpSearchResultDto>` — keyword over title/content, role-filtered, language-scoped, draft-excluded, title-first ranking; result carries match offsets so the SPA can highlight. |
| `GetContextHelpQuery` | US-3.7.2-04 | `list<HelpEntrySummaryDto>` — by `contextKey` + `viewerRole` + `language`, via `ContextHelpResolver`. |
| `GetGuidedToursQuery` | US-3.7.2-07 | `list<GuidedTourDto>` — active tours for a given `audience` + `language`. |
| `GetFeedbackSummaryQuery` | US-3.7.2-05 | `list<FeedbackSummaryDto>` — **admin-only**; per `FaqEntry`: helpful count, not-helpful count, reason breakdown, recent comments. |

### 10.3 Validators — representative rules

- `CreateArticleDraftCommand`: `language` in enum; `content.Title` non-empty ≤ 200; `content.BodyRichText` non-empty.
- `PublishArticleCommand`: `articleId` non-empty (the category + localization gate is a domain invariant, surfaced as `E-ARTICLE-NO-CATEGORY` from the aggregate, not duplicated here).
- `ScheduleArticleCommand`: `publishAtUtc` must be `> now()` (`E-SCHEDULE-PAST`) — also re-checked in the VO factory.
- `SetArticleTagsCommand`: each tag `DisplayLabel` non-empty ≤ 50; at most 25 tags per language.
- `AddArticleMediaCommand`: image MIME ∈ {jpeg, png, gif} ≤ 5 MB; video MIME ∈ {mp4, webm} ≤ 500 MB.
- `BulkArchiveArticlesCommand`: `articleIds` non-empty, count ≤ 50 (`E-BULK-LIMIT-EXCEEDED`).
- `CreateFaqEntryCommand`: `content.Question` non-empty ≤ 300; `content.AnswerRichText` non-empty; `kind` in enum.
- `SetFaqVisibleRolesCommand`: `roles` non-empty; `All` not mixed with specific roles.
- `SubmitHelpFeedbackCommand`: `reason` **required** when `wasHelpful == false`, **forbidden** when `true`; `comment` ≤ 2000; `faqEntryId` references an existing entry.
- `UpdateContentPreferenceCommand`: no category id appears in both included and hidden lists.
- `AddTourStepCommand`: `targetSelector` non-empty; `tooltipText` non-empty ≤ 500.

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper. `PagedResult<T>` carries `Items`, `Page`, `PageSize`, `TotalCount`, and a `NoResults` convenience flag.

---

## 11. Persistence & data model

Schema/namespace: `content_management`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. References to BC-1 identity (`author_user_id`, `ContentPreference.user_id`) are plain `uuid` columns with no FK constraint. Foreign keys *within* this schema (article → category, faq → topic via join table, feedback → faq entry) are permitted and encouraged.

### 11.1 Reference relational model — schema `content_management`

```
TABLE articles
  id                    uuid          PK
  author_user_id        uuid          NOT NULL                 -- BC-1 identity, no FK
  status                enum          NOT NULL                 -- Draft|Scheduled|Published|Unpublished|Archived
  primary_category_id   uuid          NULL                     -- FK → categories.id (same schema), nullable while Draft
  schedule_publish_at   datetime      NULL                     -- non-null iff status = Scheduled
  published_on_utc      datetime      NULL
  previous_status       enum          NULL                     -- remembered on archive for restore
  media                 json          NOT NULL DEFAULT '[]'    -- MediaReference[] VO
  created_on_utc        datetime      NOT NULL
  updated_on_utc        datetime      NOT NULL
  version_token         (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (status)
  INDEX (primary_category_id)
  INDEX (status, published_on_utc DESC) WHERE status = 'Published'
  INDEX (schedule_publish_at) WHERE status = 'Scheduled'

TABLE article_localizations
  id                    uuid          PK
  article_id            uuid          NOT NULL                 -- FK → articles.id ON DELETE CASCADE
  language              enum          NOT NULL                 -- En|Bn
  title                 string        NOT NULL
  summary               string        NOT NULL
  body_rich_text        string        NOT NULL                 -- sanitized HTML
  UNIQUE (article_id, language)
  INDEX (article_id, language)
  -- title/body full-text: use a case-insensitive substring search; a native FTS index is a future optimization

TABLE article_tags
  id                    uuid          PK
  article_id            uuid          NOT NULL                 -- FK → articles.id ON DELETE CASCADE
  language              enum          NOT NULL
  normalized_label      string        NOT NULL                 -- lower-cased, trimmed
  display_label         string        NOT NULL
  UNIQUE (article_id, language, normalized_label)
  INDEX (language, normalized_label)                           -- powers tag filter + auto-suggest

TABLE categories
  id                    uuid          PK
  slug                  string        NOT NULL UNIQUE
  names                 json          NOT NULL                 -- { "En": "...", "Bn": "..." }
  is_active             bool          NOT NULL DEFAULT true

TABLE faq_entries
  id                    uuid          PK
  kind                  enum          NOT NULL                 -- Faq|HelpArticle
  status                enum          NOT NULL                 -- Draft|Published
  visible_roles         json          NOT NULL                 -- VisibleRoleSet VO, e.g. ["All"] or ["JobSeeker","Employer"]
  context_keys          json          NOT NULL DEFAULT '[]'    -- list<string>
  multimedia_blocks     json          NOT NULL DEFAULT '[]'    -- MultimediaBlock[] VO
  created_on_utc        datetime      NOT NULL
  updated_on_utc        datetime      NOT NULL
  version_token         (optimistic-concurrency token)
  INDEX (status)
  -- context_keys filtering uses json-containment if the database supports it, else in-memory after a status filter

TABLE faq_localizations
  id                    uuid          PK
  faq_entry_id          uuid          NOT NULL                 -- FK → faq_entries.id ON DELETE CASCADE
  language              enum          NOT NULL
  question              string        NOT NULL
  answer_rich_text      string        NOT NULL                 -- sanitized HTML
  UNIQUE (faq_entry_id, language)

TABLE topics
  id                    uuid          PK
  slug                  string        NOT NULL UNIQUE
  names                 json          NOT NULL
  is_active             bool          NOT NULL DEFAULT true

TABLE faq_entry_topics                                        -- join table, FAQ ↔ Topic many-to-many
  faq_entry_id          uuid          NOT NULL                 -- FK → faq_entries.id ON DELETE CASCADE
  topic_id              uuid          NOT NULL                 -- FK → topics.id ON DELETE RESTRICT
  PRIMARY KEY (faq_entry_id, topic_id)
  INDEX (topic_id)                                             -- powers "is this topic referenced?" delete check

TABLE guided_tours
  id                    uuid          PK
  language              enum          NOT NULL
  name                  string        NOT NULL
  description           string        NOT NULL
  target_audience       json          NOT NULL                 -- AudienceSet VO
  is_active             bool          NOT NULL DEFAULT true
  version_token         (optimistic-concurrency token)

TABLE tour_steps
  id                    uuid          PK
  guided_tour_id        uuid          NOT NULL                 -- FK → guided_tours.id ON DELETE CASCADE
  step_order            int           NOT NULL
  target_selector       string        NOT NULL
  tooltip_text          string        NOT NULL
  action                json          NULL                     -- TourAction VO
  UNIQUE (guided_tour_id, step_order)

TABLE content_preferences
  id                    uuid          PK
  user_id               uuid          NOT NULL UNIQUE          -- BC-1 identity, no FK
  preferred_language    enum          NOT NULL DEFAULT 'En'
  included_category_ids json          NOT NULL DEFAULT '[]'
  hidden_category_ids   json          NOT NULL DEFAULT '[]'
  updated_on_utc        datetime      NOT NULL
  INDEX (user_id)

TABLE help_feedback
  id                    uuid          PK
  faq_entry_id          uuid          NOT NULL                 -- FK → faq_entries.id ON DELETE CASCADE
  was_helpful           bool          NOT NULL
  reason                string        NULL                     -- non-null iff was_helpful = false
  comment               string        NULL
  submitted_by_role     string        NULL                     -- null when fully anonymous
  language              enum          NOT NULL
  submitted_on_utc      datetime      NOT NULL
  INDEX (faq_entry_id)                                         -- powers the admin aggregation dashboard

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Aggregate roots and their child collections are mapped by the chosen ORM. `article_localizations`, `article_tags`, `faq_localizations`, `tour_steps` are **owned** by their roots and loaded with them. The FAQ↔Topic relation goes through the `faq_entry_topics` join table; `TopicIds` is reconstituted from it.
- Scalars that need querying/uniqueness (`status`, `language`, `slug`, `schedule_publish_at`, `published_on_utc`) are flattened to columns; the rest of each VO (`media`, `visible_roles`, `context_keys`, `multimedia_blocks`, `target_audience`, `action`, `included/hidden_category_ids`, category/topic `names`) maps to `json` columns.
- Optimistic-concurrency tokens are required on `articles`, `faq_entries`, and `guided_tours` (the aggregates with multi-step editing).
- **Search**: the stories explicitly accept basic case-insensitive substring search for MVP (`US-3.7.1-06`, `US-3.7.2-03` assumptions). A native full-text index is a documented future optimization, not required now.
- The scheduled-publication worker is a hosted background service that issues `PublishDueArticlesCommand` on a fixed interval (e.g. every 60 s) — see §3 module-specific notes.

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`ArticleRepository` (`GetById`, `GetDueForPublication(nowUtc)`, `BrowsePublished(filter)`, `SearchArchive(filter)`, `CountByCategory(categoryId)`, `Add`, `Update`, `Delete`), `CategoryRepository` (`GetById`, `GetBySlug`, `GetAll`, `IsSlugTaken`, `Add`, `Update`, `Delete`), `FaqEntryRepository` (`GetById`, `BrowsePublished(role, language)`, `SearchPublished(query, role, language)`, `GetByContextKey(key, role, language)`, `Add`, `Update`, `Delete`), `TopicRepository` (`GetById`, `GetAll`, `IsSlugTaken`, `CountReferencingEntries(topicId)`, `Add`, `Update`, `Delete`), `GuidedTourRepository` (`GetById`, `GetActive(audience, language)`, `Add`, `Update`), `ContentPreferenceRepository` (`GetByUserId`, `ExistsForUser`, `Add`, `Update`), `HelpFeedbackRepository` (`Add`, `GetSummaryByEntry`), `UnitOfWork` (`SaveChanges`).

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/content`. Authoring/admin endpoints require a valid access token (issued by BC-1) carrying a `ContentEditor` or `Administrator` role claim. Public read endpoints (browse/search/help/tours) are anonymous. The feedback-submit endpoint is **anonymous by design** (`US-3.7.2-05 AC-05`). Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Auth | Success | Notable failures |
|---|---|---|---|---|
| `POST /api/content/articles` | `CreateArticleDraftCommand` | editor | `201` + `ArticleId` | `400` |
| `GET /api/content/articles/{id}` | `GetArticleQuery` | editor | `200` + `ArticleDto` | `404` |
| `PUT /api/content/articles/{id}/content` | `UpdateArticleContentCommand` | editor | `200` | `404`, `400` |
| `DELETE /api/content/articles/{id}/localizations/{lang}` | `RemoveArticleLocalizationCommand` | editor | `204` | `404`, `409` last localization |
| `PUT /api/content/articles/{id}/category` | `SetArticleCategoryCommand` | editor | `200` | `404`, `400` category not found/inactive |
| `PUT /api/content/articles/{id}/tags` | `SetArticleTagsCommand` | editor | `200` | `400` too many tags |
| `POST /api/content/articles/{id}/media` | `AddArticleMediaCommand` (multipart) | editor | `201` + media ref | `400 E-MEDIA-INVALID-FORMAT`, `413 E-MEDIA-SIZE-EXCEEDED` |
| `DELETE /api/content/articles/{id}/media/{storageKey}` | `RemoveArticleMediaCommand` | editor | `204` | `404` |
| `POST /api/content/articles/{id}/publish` | `PublishArticleCommand` | editor | `200` | `409 E-ARTICLE-NO-CATEGORY`, `409` no localization |
| `POST /api/content/articles/{id}/schedule` | `ScheduleArticleCommand` | editor | `200` | `400 E-SCHEDULE-PAST`, `409 E-ARTICLE-NO-CATEGORY` |
| `DELETE /api/content/articles/{id}/schedule` | `CancelArticleScheduleCommand` | editor | `204` | `409` not scheduled |
| `PUT /api/content/articles/{id}/schedule` | `RescheduleArticleCommand` | editor | `200` | `400 E-SCHEDULE-PAST`, `409` not scheduled |
| `POST /api/content/articles/{id}/unpublish` | `UnpublishArticleCommand` | editor | `200` | `409` not published |
| `POST /api/content/articles/{id}/archive` | `ArchiveArticleCommand` | editor | `200` | `409` illegal state |
| `POST /api/content/articles/archive-bulk` | `BulkArchiveArticlesCommand` | editor | `200` + per-id results | `400 E-BULK-LIMIT-EXCEEDED` |
| `POST /api/content/articles/{id}/restore` | `RestoreArticleFromArchiveCommand` | editor | `200` | `409` not archived |
| `GET /api/content/news` *(anonymous)* | `BrowseNewsQuery` | — | `200` + `PagedResult<NewsFeedItemDto>` | |
| `GET /api/content/news/search` *(anonymous)* | `SearchNewsArchiveQuery` | — | `200` + `PagedResult` (empty + `noResults` flag if none) | |
| `GET /api/content/news/dashboard` | `GetDashboardNewsQuery` | jobseeker | `200` + dashboard items | |
| `GET /api/content/categories` *(anonymous)* | `GetCategoriesQuery` | — | `200` + categories | |
| `POST /api/content/categories` | `CreateCategoryCommand` | admin | `201` | `409` slug taken |
| `PUT /api/content/categories/{id}` | `UpdateCategoryCommand` | admin | `200` | `404`, `409` slug taken |
| `DELETE /api/content/categories/{id}` | `DeleteCategoryCommand` | admin | `204` | `409 E-CATEGORY-IN-USE` |
| `POST /api/content/faq` | `CreateFaqEntryCommand` | admin | `201` + `FaqEntryId` | `400` |
| `GET /api/content/faq/{id}` | `GetFaqEntryQuery` | admin | `200` + `FaqEntryDto` | `404` |
| `PUT /api/content/faq/{id}/content` | `UpdateFaqEntryContentCommand` | admin | `200` | `404` |
| `PUT /api/content/faq/{id}/topics` | `SetFaqTopicsCommand` | admin | `200` | `404`, `400` unknown topic |
| `PUT /api/content/faq/{id}/visible-roles` | `SetFaqVisibleRolesCommand` | admin | `200` | `400` empty/`All`-mixed |
| `PUT /api/content/faq/{id}/context-keys` | `SetFaqContextKeysCommand` | admin | `200` | `404` |
| `POST /api/content/faq/{id}/multimedia` | `AddFaqMultimediaBlockCommand` (multipart) | admin | `201` | `409` not a HelpArticle, `413 E-MEDIA-SIZE-EXCEEDED` |
| `POST /api/content/faq/{id}/publish` | `PublishFaqEntryCommand` | admin | `200` | `409` no localization |
| `POST /api/content/faq/{id}/unpublish` | `UnpublishFaqEntryCommand` | admin | `200` | `409` not published |
| `GET /api/content/help` *(anonymous)* | `BrowseHelpCenterQuery` | — | `200` + grouped-by-topic | |
| `GET /api/content/help/search` *(anonymous)* | `SearchHelpContentQuery` | — | `200` + results w/ match offsets | |
| `GET /api/content/help/context/{contextKey}` *(anonymous)* | `GetContextHelpQuery` | — | `200` + entries for context | |
| `POST /api/content/topics` / `PUT .../{id}` / `DELETE .../{id}` | Topic commands | admin | `201`/`200`/`204` | `409 E-TOPIC-IN-USE` |
| `POST /api/content/feedback` *(anonymous)* | `SubmitHelpFeedbackCommand` | — | `201` | `400` reason rule violated, `404` unknown FAQ entry |
| `GET /api/content/feedback/summary` | `GetFeedbackSummaryQuery` | admin | `200` + per-entry aggregates | |
| `POST /api/content/tours` | `CreateGuidedTourCommand` | admin | `201` + `GuidedTourId` | `400` |
| `POST /api/content/tours/{id}/steps` | `AddTourStepCommand` | admin | `201` | `400` |
| `PUT /api/content/tours/{id}/steps/{stepId}` | `UpdateTourStepCommand` | admin | `200` | `404` |
| `DELETE /api/content/tours/{id}/steps/{stepId}` | `RemoveTourStepCommand` | admin | `204` | `404` |
| `PUT /api/content/tours/{id}/steps/reorder` | `ReorderTourStepsCommand` | admin | `200` | `400` not a permutation |
| `PUT /api/content/tours/{id}/active` | `SetTourActiveCommand` | admin | `200` | `404` |
| `GET /api/content/tours` *(anonymous)* | `GetGuidedToursQuery` | — | `200` + active tours for audience+language | |
| `PUT /api/content/preferences` | `UpdateContentPreferenceCommand` | any authenticated user | `200` | `400` category in both lists |

Role checks: the endpoint reads the role claim from the access token. `editor` accepts `ContentEditor` or `Administrator`; `admin` requires `Administrator`. The story roles "Content Editor" (news) and "MoL Administrator" (FAQ/help) map to those two claims respectively.

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `LocalizedContent` (empty title fails, > 200-char title fails, script tag stripped from body), `FaqContent` (empty question/answer fails), `PublicationSchedule` (past instant fails `E-SCHEDULE-PAST`, future succeeds), `VisibleRoleSet` (empty fails, `All` mixed with `JobSeeker` fails), `MediaReference` (6 MB image fails, 600 MB video fails, wrong MIME fails), `AudienceSet` (empty fails), `MultimediaBlock` (`Video` block with no media fails, `StepGuide` with no steps fails), `TourAction` (`Navigate` with no payload fails).
- **Article aggregate:**
  - Status machine: every legal transition succeeds; every illegal one (`Draft → Unpublished`, `Archived → Published` directly, `Published → Scheduled`, etc.) returns failure.
  - `Publish()` and `Schedule()` fail with `E-ARTICLE-NO-CATEGORY` when no primary category; fail when zero localizations; succeed once both present.
  - `Schedule()` with a past `PublishAtUtc` fails; `Schedule` is non-null iff `Status == Scheduled`.
  - `IsDueForPublication(now)` is true only for `Scheduled` articles with `PublishAtUtc <= now`.
  - `Unpublish()` raises `ArticleArchived` with `ResultingStatus = "Unpublished"`; `Archive()` raises it with `"Archived"`.
  - `Archive()` records `PreviousStatus`; `RestoreFromArchive()` returns to that status and raises `ArticlePublished` only when the restored status is `Published`.
  - Editing the EN `LocalizedContent` leaves the Bangla one byte-identical (localization independence).
  - `PublishedOnUtc` is set exactly once and survives unpublish→republish.
- **FaqEntry aggregate:** `Publish()` fails with no localization; `AddMultimediaBlock` fails on a `Faq`-kind entry; overwrite-on-edit (no version history) — editing content twice keeps only the latest.
- **Category / Topic:** `EnsureDeletable(0)` succeeds; `EnsureDeletable(n>0)` returns `E-CATEGORY-IN-USE` / `E-TOPIC-IN-USE`.
- **GuidedTour:** steps stay contiguously ordered from 1 after `AddStep`, `RemoveStep`, `ReorderSteps`; `ReorderSteps` with a non-permutation fails.
- **ContentPreference:** a category id in both included and hidden lists fails.
- **HelpFeedback:** `Submit` with `wasHelpful = false` and no `reason` fails; with `wasHelpful = true` and a `reason` fails; valid cases succeed and raise `HelpFeedbackReceived`.
- **Domain services:** `DashboardNewsSelector` — table-driven: manual includes win; profile-attribute match used when no includes; global fallback when profile incomplete and no includes; hidden categories always excluded; preferred language preferred. `ContentSearchRanker` — title matches sort above body-only matches; recency tiebreak. `ContextHelpResolver` — only entries with the context key, visible to the role, with a localization in the language; `HelpArticle` before `Faq`.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `PublishArticleCommand`: article with category + localization → `Published`, `ArticlePublished` queued to outbox; article with no category → `E-ARTICLE-NO-CATEGORY`, nothing persisted.
- `ScheduleArticleCommand`: future time → `Scheduled` + `ArticleScheduled` to outbox; past time → `E-SCHEDULE-PAST`.
- `PublishDueArticlesCommand`: given two due and one not-yet-due scheduled article, exactly the two due ones transition and emit `ArticlePublished`; running the command again is a no-op (idempotent).
- `BulkArchiveArticlesCommand`: 50 ids succeed; 51 ids → `E-BULK-LIMIT-EXCEEDED`, nothing archived.
- `DeleteCategoryCommand`: category with referencing articles → `E-CATEGORY-IN-USE`; unreferenced category → deleted.
- `AddArticleMediaCommand`: calls `MediaStorage.Store`, stores the returned `MediaReference`; oversized file rejected by validator before the port is called.
- `SubmitHelpFeedbackCommand`: anonymous submission (no access token) succeeds; `wasHelpful = false` without `reason` rejected by the validator.
- `GetDashboardNewsQuery`: when `JobSeekerProfileQueryApi` returns `null`, the handler still returns global-fallback news (no exception).
- `UserRegistered` consumer: creates a default `ContentPreference`; delivering the same event twice creates exactly one preference.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each aggregate including child collections and `json` VOs; the FAQ↔Topic join table reconstitutes `TopicIds` correctly; optimistic-concurrency conflict on `articles` is detected.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `content_management`.
- **Browse/search queries:** `BrowseNewsQuery` returns only `Published`, newest-first, and AND-combines category + tag filters; `SearchNewsArchiveQuery` is case-insensitive, respects the date range, includes `Unpublished`/`Archived` but **excludes `Draft`**, and ranks title matches first; `SearchHelpContentQuery` excludes entries not visible to the viewer role.
- **Outbox:** publishing an article writes both the row change and the `ArticlePublished` outbox message in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering `UserRegisteredIntegrationEvent` twice creates one `ContentPreference` and is a no-op the second time.
- **Scheduler:** insert a `Scheduled` article with `schedule_publish_at` in the past, run `PublishDueArticlesCommand`, assert it is now `Published` with `PublishedOnUtc` set and an outbox `ArticlePublished` row exists.
- **API:** host-level tests for the editor happy path (create draft → set category → set tags → schedule → publish-due → appears in `BrowseNewsQuery`); anonymous `POST /api/content/feedback` succeeds without a token; anonymous access to an editor route returns `401`.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "Schedule a news article for future publication"

End-to-end, to pattern-match every other command against:

1. **API.** `POST /api/content/articles/{id}/schedule` with body `{ publishAtUtc }`. The endpoint checks the access token carries a `ContentEditor`/`Administrator` claim, builds `ScheduleArticleCommand { ArticleId, PublishAtUtc }`, dispatches it through the mediator.
2. **Validation step.** `ScheduleArticleCommand`'s validator runs: `publishAtUtc > now()`. On failure → `Result` with `Error("E-SCHEDULE-PAST", …)`, mapped to `400`.
3. **Handler.** `ScheduleArticleCommandHandler`:
   a. `ArticleRepository.GetById(articleId)` → `Article` (with localizations + tags). `404` if missing.
   b. `PublicationSchedule.Create(publishAtUtc)` → `Result<PublicationSchedule>`; re-checks the future constraint; propagate failure.
   c. `article.Schedule(schedule)` — the aggregate enforces the publish gate (primary category present, ≥ 1 localization), transitions `Draft → Scheduled`, stores the schedule, and raises `ArticleScheduled`. If the gate fails it returns `E-ARTICLE-NO-CATEGORY` and nothing is persisted.
   d. `repository.Update(article)`; `unitOfWork.SaveChanges()`.
4. **Domain-event / outbox step.** After the unit of work is saved, the mediator pipeline writes `ArticleScheduledIntegrationEvent` into `outbox_messages` — same transaction. ([[00-Shared-Foundations]] §6.1–6.2.)
5. **Relay.** The background outbox relay publishes the integration event; BC-9 Notification receives it and notes when the future publish will fire.
6. **Later — the scheduler fires.** On its interval the hosted scheduled-publication worker dispatches `PublishDueArticlesCommand`. Its handler queries `ArticleRepository.GetDueForPublication(now())`, finds this article (`PublishAtUtc <= now`), calls `article.MarkPublishedBySchedule()` (`Scheduled → Published`, sets `PublishedOnUtc`, raises `ArticlePublished`), persists, and `ArticlePublishedIntegrationEvent` lands in the outbox. BC-3 refreshes the dashboard widget; BC-9 builds the news digest.
7. **Response.** The original `schedule` handler returned `Result` success; the endpoint returned `200`.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.7.1-01 Publish news article | Rich-text body stored sanitized; media embeddable via `MediaStorage`; `Publish()` makes the article appear in `BrowseNewsQuery` immediately; independent EN/Bangla localizations; draft persisted server-side via `CreateArticleDraftCommand`. |
| US-3.7.1-02 Schedule publication | Future `PublishAtUtc` accepted, past rejected (`E-SCHEDULE-PAST`); scheduled article stays out of the published feed until due; scheduled-publication worker auto-publishes at/after the time; reschedule and cancel supported before publication. |
| US-3.7.1-03 Categorize & tag | Exactly one primary category from an admin-defined list; multiple free-form bilingual tags; **publish blocked without a category** (`E-ARTICLE-NO-CATEGORY`); tag auto-suggest backed by the `(language, normalized_label)` index. |
| US-3.7.1-04 Archive & unpublish | `Unpublish()` removes from active feeds; `Archive()` removes from active + unpublished lists; archived articles still returned by archive search with a status indicator; restore returns to the prior state; bulk archive ≤ 50 per request. |
| US-3.7.1-05 Browse & filter news | Paginated published feed, newest-first; filter by category and by tag; multiple filters AND-combined; clear-filters resets; content shown in the requested language. |
| US-3.7.1-06 Search news archive | Case-insensitive title/body match; date-range filter; title matches ranked first; paginated (20/page); helpful empty-result flag; language-scoped; archive search spans Published/Unpublished/Archived, never Draft. |
| US-3.7.1-07 Dashboard personalization | Profile-attribute-based category matching when a profile is available; default fallback to recent global news when not; manual include/hide category overrides honored; preferred-language display; per-session dismissal is client-side (not persisted). |
| US-3.7.2-01 Create FAQ entry | Question + rich-text answer; draft/publish states; published entry appears in the help center immediately; independent EN/Bangla versions; overwrite-on-edit with no version history. |
| US-3.7.2-02 Organize help by topic & role | One-or-more admin-defined topics per entry; `VisibleRoleSet` targeting (`JobSeeker`/`Employer`/`Administrator`/`All`); role-based filtering so an Employer-only entry never shows to a Job Seeker; topic delete blocked while referenced (`E-TOPIC-IN-USE`). |
| US-3.7.2-03 Search help content | Keyword over title/content; results role-filtered automatically; title-first relevance ranking; case-insensitive; language-scoped; drafts excluded; match offsets returned for highlighting. |
| US-3.7.2-04 Context-sensitive help | Help entries mapped to context keys; `GetContextHelpQuery` returns the entries for a context key filtered by role + language; `HelpArticle` entries surfaced before plain `Faq`. |
| US-3.7.2-05 Collect help feedback | Anonymous "Was this helpful?" with no login required; `reason` required only on a "No"; optional comment ≤ 2000; feedback stored with timestamp/role/language; admin aggregation dashboard via `GetFeedbackSummaryQuery`. |
| US-3.7.2-06 Multimedia help content | Embedded video (link or uploaded reference) and interactive step guides on `HelpArticle` entries; optional transcript reference for accessibility; bytes stored via `MediaStorage`, only references in this module. |
| US-3.7.2-07 Guided tours | Admin-authored tours with name/description/audience; ordered steps each with a CSS selector + tooltip + optional action; reorder keeps contiguous ordering; role/audience targeting; independent EN/Bangla tours; `GetGuidedToursQuery` serves active tours by audience + language. |

---

## Appendix — teaching notes & open questions

- **Two "taxonomies", one platform.** §1's boundary note: article categories / help topics are owned *here*, not in BC-11. Use this to teach that "they're both called taxonomy" is not a reason to merge bounded concepts — what matters is shared consumers and shared invariants. Contrast with the skill taxonomy, which genuinely *is* a Published Language across four BCs.
- **One event for two transitions.** `ArticleArchived` is raised by both `Unpublish()` and `Archive()`, disambiguated by a `ResultingStatus` payload field. The alternative is two distinct events (`ArticleUnpublished`, `ArticleArchived`). The [[Event_Catalog]] gives BC-12 a fixed five-event budget; this is a concrete instance of the catalog's open question #1 (granular vs. unified status events). Discuss: subscriber simplicity vs. payload-driven branching.
- **Scheduling: domain decision vs. infrastructure timer.** The module owns *what publishes when* (`PublicationSchedule`, `IsDueForPublication`, `PublishDueArticlesCommand`); the host owns the *timer* that pokes it. This keeps the Domain layer free of timer/cron concerns and keeps the scheduling rule unit-testable. Good contrast with putting a job-scheduler library inside the domain.
- **Sync query vs. event-fed copy for personalization.** §9.2 calls BC-3 synchronously rather than maintaining a local profile copy. Defensible because the read is rare and non-critical. Ask the class: at what read volume, or what consistency requirement, would you flip to an event-fed local read-model?
- **Anonymous writes.** `SubmitHelpFeedbackCommand` is a write with no authenticated principal — unusual in this codebase. Discuss how the outbox/inbox and `Result` conventions still apply, and why feedback is modeled as its own append-only aggregate rather than a child of `FaqEntry` (different lifecycle, different author, write-heavy).
- **Localization as a map-of-VOs.** EN and Bangla content live as a `map<Language, LocalizedContent>` on one aggregate, not as separate aggregates. The invariant "at least one localization" and "editing one never touches the other" are enforced in one place. Discuss the alternative (one `Article` aggregate per language) and its cost: duplicated category/tag/schedule state and a cross-aggregate "these two are the same story" invariant.
- **Bilingual / localization scope.** Stories specify EN + Bangla (`Bn`); the `Language` enum is closed at two values per the MVP assumptions. If more languages are added later, only the enum and validation widen — the map model already supports N languages.
