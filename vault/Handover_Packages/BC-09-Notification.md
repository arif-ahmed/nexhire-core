---
title: "Handover Package — BC-9 Notification"
type: handover-package
bc_id: BC-9
bc_name: Notification
bc_class: generic
stack: "language-neutral — see [[00-Shared-Foundations]] (Target Stack declared there)"
generated: 2026-05-15
related:
  - "[[00-Shared-Foundations]]"
  - "[[BC_Mapping]]"
  - "[[Context_Map]]"
  - "[[Event_Catalog]]"
tags:
  - handover-package
  - bc/notification
---

# Handover Package — BC-9 Notification

> **Audience:** an AI coding agent. This package owns the **domain design** for the `Notification` module — aggregates, behaviors, invariants, domain events, the relational schema, the API contract, and the test cases. Everything stack-related and cross-cutting (the Target Stack, notation, layering, shared-kernel building blocks, outbox/inbox, testing strategy) lives in **[[00-Shared-Foundations]]** — read that file first; this package assumes it throughout.
>
> The two files together — this package **plus** `00-Shared-Foundations.md` — are the complete brief for this module. Hand both to the agent.

## 0. How to use this package

Build a single module, `Notification`, inside a modular monolith. **First, read [[00-Shared-Foundations]]** — it declares the Target Stack (§1 there), the neutral notation (§2), the type vocabulary (§3), the 5-layer structure (§4), the shared-kernel building blocks (§5), the cross-cutting conventions — outbox/inbox, domain-event dispatch, persistence rules (§6) — and the testing strategy (§7). If the Target Stack block in that file is blank, ask the implementer to fill it in before writing any code.

Then work through this package in section order: model the Domain layer first (§5–8), then the Application layer (§10), then persistence (§11), then the API (§12). Write tests as specified in §13. §14 is a full worked vertical slice — pattern-match against it.

The module owns its own persistence boundary — schema `notification`. It communicates with other modules **only** through (a) integration events and (b) the public contract interfaces reproduced in §9. It never reaches into another module's tables or internal types.

This BC is a **Conformist** consumer (see [[Context_Map]]): it subscribes broadly to events emitted by *many* other BCs and accepts their payload shapes as-is — it never asks an upstream to reshape an event for its convenience. Its job is to translate "something happened" facts into delivered email/in-app/SMS messages, governed by user preferences and compliance rules.

---

## 1. Purpose & scope boundaries

### What this BC is for

Notification owns the **act of telling a user that something happened**, across three channels — **email, in-app, SMS** — plus the supporting machinery: per-user channel preferences, templates with dynamic placeholders, immediate-vs-digest batching, delivery tracking, and regulatory compliance (CAN-SPAM, GDPR, TCPA, PIPEDA, Ofcom). It is a **generic** subdomain — "send a message" is a solved, non-differentiating capability — but it is platform-wide: every other domain BC depends on it to reach users.

### In scope

The `Notification` module is responsible for:

- **Subscribing** to integration events from BC-1, BC-2, BC-4, BC-5, BC-6, BC-7, BC-8, BC-12 (full list in §9.1) and turning each into zero-or-more `Notification` aggregates.
- **Channel routing** — deciding, per recipient and per event, which channels (email / in-app / SMS) a notification should go out on, based on stored preferences.
- **Templates** — admin-managed, versioned templates with `{{placeholder}}` substitution, HTML + plain-text bodies, preview with sample data (`US-3.6.1-03`).
- **Notification preferences** — per-user, per-channel, per-notification-type toggles; frequency choice (immediate / daily digest / weekly digest); do-not-disturb windows; preferred email address; SMS opt-in with phone verification (`US-3.6.1-02`, `US-3.6.2-05`, `US-3.6.3-02`).
- **Immediate delivery** — render template, hand to a channel adapter, within a 5-minute SLA (email/in-app) or 30-second SLA (SMS).
- **Digest batching** — queue events for users on digest frequency; assemble and send a consolidated message at the scheduled window; suppress empty digests (`US-3.6.1-04`).
- **In-app notification center** — the read-model of a user's notifications: unread counts, filtering, search, mark-read, soft-delete/archive, undo, real-time toast push (`US-3.6.2-01..07`).
- **SMS** — for critical/time-sensitive events only; frequency caps; DND respect; opt-in/opt-out; STOP keyword handling (`US-3.6.3-01..05`).
- **Delivery tracking & logging** — every send logged with status lifecycle (`Pending → Sent → Delivered | Bounced | Failed | Opened | Clicked`); retry on soft bounce; hard-bounce flagging; 3-year retention then archive (`US-3.6.1-05`, `US-3.6.3-04`).
- **Compliance** — unsubscribe links, sender identity blocks, bounce/complaint handling, DNC-list checks, consent audit trail (`US-3.6.1-06`, `US-3.6.3-05`).
- Publishing the integration events in §8 and reacting to the integration events in §9.

### Out of scope — explicitly NOT this BC

Do not build any of the following into this module. They belong to other BCs (or external systems) and are reached via the contracts in §9:

- **Deciding *whether* a business event happened** — every other BC owns its own events. This module only reacts. It does not poll, it does not compute job matches, it does not change application status.
- **The actual email/SMS transport** — third-party providers are external, reached via `EmailChannel` / `SmsChannel` ports. This module orchestrates and tracks; it does not implement SMTP or carrier protocols.
- **Real-time transport plumbing for in-app toasts** — the WebSocket/SSE hub is external infrastructure, reached via the `RealtimePush` port. This module decides *what* to push and *to whom*; the hub delivers bytes.
- **OTP generation and OTP validation** — BC-1 IAM/UAM owns OTP. When BC-1 needs an OTP SMS sent it emits an event carrying the already-generated code; this module only *delivers* it. This module never generates or validates a code.
- **Credentials, login, sessions, access tokens, MFA** → BC-1. The notification API trusts the access token BC-1 issued.
- **The user's identity / which email and mobile are on file at the identity level** → BC-1. This module keeps its *own* contact + preference record per `UserId`, seeded from BC-1 events, but BC-1 remains the system of record for identity.
- **Computing recommendations for the weekly digest** → BC-7 Recommendation Engine. BC-7 emits `RecommendationGenerated`; this module only formats and delivers it (`US-3.6.2-03`).
- **Reporting / analytics on notification volume** → BC-10 Reporting. This module emits delivery events; BC-10 builds the dashboards. (The SMS admin metrics dashboard in `US-3.6.3-04 AC-07` is served by BC-10 from our emitted events; this module exposes the raw log via its admin API but does not build LMIS-style analytics.)
- **The skill/job/employer data referenced inside a notification body** — placeholders are filled from event payloads only. This module does not call back into BC-3/BC-4 to enrich; if a payload lacks a field, the template degrades gracefully.
- **DNS-level SPF/DKIM/DMARC record configuration** (`US-3.6.1-06 AC-04`) — that is an infrastructure/ops concern, not application code. This module's responsibility is to *assume* the domain is authenticated and to set the correct From/Reply-To/List-Unsubscribe headers.

### Boundary note — preferences ownership (teaching point)

[[Context_Map]] open question #3 asks: *should BC-9 own user notification preferences, or should each profile BC?* This package puts preferences **here**, in BC-9, because (a) preferences are inherently cross-channel and channel knowledge lives here, and (b) it keeps profile BCs from each re-implementing the same toggle UI. The cost is that BC-9 now holds a small slice of per-user state seeded from BC-1/BC-3/BC-2 events. Discuss the trade-off: cohesion of the notification concern vs. a generic BC holding user-scoped data.

---

## 2. Ubiquitous language

Terms as used **inside this BC**. Use these exact names in code.

| Term | Meaning |
|---|---|
| **Notification** | The `Notification` aggregate — one instance of "user X should be told about event Y on channel Z". Root of the BC. |
| **Channel** | A delivery medium: `Email`, `InApp`, `Sms`. |
| **Notification Type** | The business category of a notification: `JobRecommendation`, `ApplicationUpdate`, `Message`, `ProfileView`, `RecruiterActivity`, `Announcement`, `AccountSecurity`, `Transactional`. Drives icon/colour and preference toggles. |
| **Priority** | `High` (urgent, bypasses DND and frequency caps), `Normal` (informational). |
| **Recipient** | The `RecipientPreferences` aggregate — one per `UserId`. Holds contact points + all channel/type preferences + compliance state. |
| **Contact Point** | A verified delivery address: an email address or a phone number, with a `Verified` flag. |
| **Template** | The `NotificationTemplate` aggregate — admin-managed, channel-specific, versioned content with placeholders. |
| **Template Version** | An immutable snapshot of a template's content; previous versions are retained 12 months and restorable. |
| **Placeholder** | A `{{variable.property}}` token in a template body, substituted at render time from the triggering event's payload. |
| **Rendered Message** | The concrete subject + body produced by applying a template version to a payload — what actually gets sent. |
| **Frequency** | A recipient's batching choice for a (channel, type): `Immediate`, `DailyDigest`, `WeeklyDigest`. |
| **Digest** | The `Digest` aggregate — a queue of pending notifications for one user + one window, assembled into a single consolidated message. |
| **Digest Window** | `Daily` (08:00 user-timezone) or `Weekly` (Monday 08:00 user-timezone). |
| **Do Not Disturb (DND)** | A recipient-configured quiet window during which non-`High` notifications are held, not dropped. |
| **Delivery Status** | `Pending → Sent → Delivered`, or `→ Bounced` / `→ Failed` / `→ Complaint`; plus engagement states `Opened`, `Clicked`. |
| **Bounce** | `Soft` (temporary — retry up to 3×) or `Hard` (permanent — flag contact point invalid). |
| **Complaint** | Recipient marked a message as spam — forces unsubscribe of that channel. |
| **Consent Record** | An immutable log entry of an opt-in or opt-out decision (timestamp, IP, method) — kept 3 years for regulatory audit. |
| **Suppression** | A contact point flagged so no further *non-critical* sends occur (hard bounce ×3, complaint, opt-out, or DNC hit). |
| **Critical Notification** | A `High`-priority `AccountSecurity` or `Transactional` notification — cannot be opted out of (TCPA carve-out), bypasses caps/DND. |
| **Frequency Cap** | Max SMS per user per rolling 24h (default 5); `High`/critical SMS bypass it. |

---

## 3. Module structure & layering

Follows the 5-layer Clean Architecture model, dependency rule, module-registration pattern, and public-surface rule in **[[00-Shared-Foundations]] §4**.

- Module name: `Notification`.
- Public surface (`Contracts`): the integration events published in §8 + the public API in §9.3.
- **Module-specific notes:** this module runs several **background workers** registered in its composition entry point — an outbox relay (also drives queued channel sends), a **digest scheduler** that opens new digest windows and dispatches due ones, a **DND-release worker** that wakes held notifications, a **retry worker** for soft-bounced sends (1h/6h/24h schedule), and a nightly **retention worker** (90-day in-app archival, 3-year log archival, 12-month template-version purge, 30-day digest-item expiry). Inbound provider webhooks (email/SMS delivery status, SMS STOP keyword) arrive at the API layer and are translated into commands.

---

## 4. Shared kernel reference

Uses the shared-kernel building blocks — `Entity`, `AggregateRoot`, `ValueObject`, `DomainEvent`, `IntegrationEvent`, `Result` / `Result<T>`, `Error` — defined in **[[00-Shared-Foundations]] §5**. No module-specific additions.

---

## 5. Aggregates, entities, value objects

This BC has **four aggregates**: `Notification` (the workhorse root), `RecipientPreferences` (per-user settings + contact + compliance state), `NotificationTemplate` (admin content), and `Digest` (a batching buffer). (Notation: see [[00-Shared-Foundations]] §2.)

### 5.1 Aggregate: Notification

**Aggregate root.** Identity: `NotificationId` (strongly-typed id wrapping `uuid`). One instance per (recipient, channel, triggering event) tuple — a single inbound event that fans out to 3 channels creates 3 `Notification`s.

| Member | Type | Notes |
|---|---|---|
| `Id` | `NotificationId` | |
| `RecipientUserId` | `uuid` | Identity owned by BC-1. No FK. |
| `Channel` | `Channel` | enum: `Email`, `InApp`, `Sms` |
| `Type` | `NotificationType` | enum (see §2) |
| `Priority` | `Priority` | enum: `High`, `Normal` |
| `SourceEvent` | `SourceEventRef` | VO — which integration event produced this (`EventId`, `EventType`, `SourceBc`) |
| `Payload` | `NotificationPayload` | VO — the substitution data extracted from the source event |
| `TemplateId` | `NotificationTemplateId?` | resolved template; null for in-app where rendering is inline |
| `Rendered` | `RenderedMessage?` | VO — subject + body once rendered; null until `Render()` runs |
| `DeliveryStatus` | `DeliveryStatus` | enum: `Pending`, `Queued`, `Sent`, `Delivered`, `Bounced`, `Failed`, `Complaint` |
| `Engagement` | `EngagementState` | VO — `OpenedOnUtc?`, `ClickedOnUtc?` |
| `Attempts` | `list<DeliveryAttempt>` | child entities — one per send/retry |
| `ScheduledForUtc` | `datetime?` | set when held for a DND window or queued into a digest |
| `IsRead` | `bool` | in-app only; meaningless for email/SMS |
| `IsArchived` | `bool` | soft-delete flag (in-app center) |
| `DigestId` | `DigestId?` | set when this notification was batched into a digest instead of sent immediately |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entity:**

- `DeliveryAttempt` — `DeliveryAttemptId`, `AttemptNumber` (`int`), `Channel` (`Channel`), `Outcome` (`AttemptOutcome`: `Succeeded`/`SoftBounce`/`HardBounce`/`ProviderError`), `ProviderMessageId` (`string?`), `ProviderResponse` (`string?`), `AttemptedOnUtc` (`datetime`).

### 5.2 Aggregate: RecipientPreferences

**Aggregate root.** Identity: `RecipientPreferencesId`. Exactly **one per `UserId`** — created when BC-1's `UserRegistered` arrives. Holds contact points, all per-channel/per-type preferences, DND, and compliance/suppression state.

| Member | Type | Notes |
|---|---|---|
| `Id` | `RecipientPreferencesId` | |
| `UserId` | `uuid` | BC-1 identity. UNIQUE. No FK. Immutable. |
| `Role` | `string` | from `UserRegistered` — used to default sensible preferences |
| `EmailContact` | `EmailContactPoint` | VO — address + `Verified` + `IsPreferred` |
| `SmsContact` | `PhoneContactPoint?` | VO — E.164 number + `Verified` + `OptedIn`; null until provided |
| `Timezone` | `string` | IANA tz id; defaults to `Asia/Dhaka` |
| `ChannelTypePrefs` | `list<ChannelTypePreference>` | child entities — one per (channel, type) cell |
| `DoNotDisturb` | `DndWindow?` | VO — quiet hours; null = always-on |
| `GlobalEmailOptOut` | `bool` | `US-3.6.1-02 AC-05` — kills all email except `Transactional`/`AccountSecurity` |
| `SmsSuppressed` | `bool` | set on hard bounce / complaint / STOP / DNC hit |
| `EmailSuppressed` | `bool` | set on hard bounce ×3 / complaint |
| `Consents` | `list<ConsentRecord>` | child entities — append-only opt-in/opt-out audit log |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

**Child entities:**

- `ChannelTypePreference` — `ChannelTypePreferenceId`, `Channel`, `Type`, `Enabled` (`bool`), `Frequency` (`Frequency`), `ToastMode` (`ToastMode`: `Toast`/`CenterOnly`/`Disabled` — in-app only). Unique on (channel, type).
- `ConsentRecord` — `ConsentRecordId`, `Channel`, `Decision` (`ConsentDecision`: `OptIn`/`OptOut`), `Method` (`string` — `RegistrationCheckbox`/`SettingsToggle`/`SmsStopKeyword`/`AdminAction`), `IpAddress` (`string?`), `RecordedOnUtc`.

### 5.3 Aggregate: NotificationTemplate

**Aggregate root.** Identity: `NotificationTemplateId`. Admin-managed (`US-3.6.1-03`). One template per (channel, type). Append-only version history.

| Member | Type | Notes |
|---|---|---|
| `Id` | `NotificationTemplateId` | |
| `Channel` | `Channel` | |
| `Type` | `NotificationType` | unique on (channel, type) |
| `Name` | `string` | human label |
| `CurrentVersion` | `TemplateVersion` | VO — the live version |
| `History` | `list<TemplateVersion>` | append-only; 12-month retention |
| `IsActive` | `bool` | inactive templates are not used; fall back to a built-in default |
| `CreatedOnUtc` / `UpdatedOnUtc` | `datetime` | |

### 5.4 Aggregate: Digest

**Aggregate root.** Identity: `DigestId`. A buffer: at most **one open `Digest` per (UserId, Channel, DigestWindow)** at a time. Notifications whose preference is `DailyDigest`/`WeeklyDigest` are appended here instead of being sent; a scheduled worker closes and dispatches it.

| Member | Type | Notes |
|---|---|---|
| `Id` | `DigestId` | |
| `UserId` | `uuid` | BC-1 identity. No FK. |
| `Channel` | `Channel` | `Email` or `InApp` (SMS never digests) |
| `Window` | `DigestWindow` | enum: `Daily`, `Weekly` |
| `Status` | `DigestStatus` | enum: `Open`, `Dispatched`, `Discarded` |
| `Items` | `list<DigestItem>` | child entities — references to queued notifications |
| `OpenedOnUtc` | `datetime` | when the buffer was created |
| `ScheduledSendUtc` | `datetime` | next window boundary in the user's timezone |
| `DispatchedOnUtc` | `datetime?` | |

**Child entity:**

- `DigestItem` — `DigestItemId`, `NotificationId` (`NotificationId`), `Type`, `Summary` (`string`), `ActionUrl` (`string?`), `QueuedOnUtc` (`datetime`). Items expire from the buffer after 30 days (`US-3.6.1-04` assumption).

### 5.5 Value objects

Each is immutable, validated in a factory (`Create(...) -> Result<T>`), with structural equality.

| VO | Fields | Validation rules |
|---|---|---|
| `EmailContactPoint` | `Address`, `Verified` (`bool`), `IsPreferred` (`bool`) | RFC 5322; lower-cased on store |
| `PhoneContactPoint` | `E164Number`, `Verified` (`bool`), `OptedIn` (`bool`) | E.164; default region `+880` |
| `SourceEventRef` | `EventId` (`uuid`), `EventType` (`string`), `SourceBc` (`string`) | all non-empty |
| `NotificationPayload` | `Values` (`map<string,string>`) | keys are placeholder paths e.g. `job.title` |
| `RenderedMessage` | `Subject` (`string?`), `BodyHtml` (`string?`), `BodyText` (`string`) | `BodyText` always present (plain-text fallback); SMS uses `BodyText` only |
| `EngagementState` | `OpenedOnUtc?`, `ClickedOnUtc?` | open ≤ click when both set |
| `DndWindow` | `StartLocalTime` (time-of-day), `EndLocalTime` (time-of-day) | window may wrap midnight |
| `TemplateVersion` | `VersionNumber` (`int`), `Subject` (`string?`), `BodyHtml` (`string`), `BodyText` (`string`), `Footer` (`string`), `Placeholders` (`list<string>`), `CreatedOnUtc`, `CreatedByUserId` (`uuid`) | `BodyHtml` ≤ 100 KB and ≤ 50 000 chars; `VersionNumber` strictly increasing |
| `SmsBody` | `Text` | ≤ 160 chars for a single segment; longer is allowed but flagged multi-segment |

---

## 6. Domain behaviors, invariants & business logic

All mutating behavior lives on the aggregate roots. Handlers never set properties directly. Method shapes below are neutral specifications (see [[00-Shared-Foundations]] §2).

### 6.1 Notification — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static Create(recipientUserId, channel, type, priority, sourceEvent, payload)` | Creates in `DeliveryStatus = Pending`, `IsRead = false`, `IsArchived = false`. | `NotificationCreated` *(internal)* |
| `Render(templateVersion, renderer)` | Substitutes placeholders; produces `RenderedMessage`. SMS rendered text must pass `SmsBody` rules. Only from `Pending`. | — |
| `QueueIntoDigest(digestId)` | Only from `Pending`; sets `DigestId`, `DeliveryStatus = Queued`, `ScheduledForUtc = digest window`. | — |
| `HoldForDnd(releaseAtUtc)` | Only from `Pending`/`Queued`; `Priority` must be `Normal` (`High` bypasses DND). Sets `ScheduledForUtc`. | — |
| `MarkQueued()` | `Pending → Queued`. Used when handed to a digest or DND hold without an immediate send. | — |
| `RecordSendAttempt(attemptNumber, providerMessageId)` | Appends a `DeliveryAttempt` with `Outcome = Succeeded`; `Pending`/`Queued` → `Sent`. | `NotificationDispatched` |
| `RecordSoftBounce(reason)` | Appends attempt `SoftBounce`. If `AttemptNumber < 3` stay `Sent` (eligible for retry); if `== 3` → `Failed`. | `NotificationFailed` *(only on the 3rd)* |
| `RecordHardBounce(reason)` | Appends attempt `HardBounce`; → `Bounced`. | `NotificationFailed` |
| `RecordProviderError(reason)` | Appends attempt `ProviderError`; → `Failed` if attempts exhausted, else stays `Sent`. | `NotificationFailed` *(if exhausted)* |
| `MarkDelivered()` | Only from `Sent`; → `Delivered`. Idempotent. | — |
| `MarkOpened()` | Email/in-app only; sets `Engagement.OpenedOnUtc`. Idempotent. | — |
| `MarkClicked()` | Sets `Engagement.ClickedOnUtc`; implies opened. Idempotent. | — |
| `MarkComplaint()` | → `Complaint`. Caller must also suppress the contact point on `RecipientPreferences`. | `NotificationFailed` |
| `MarkRead()` | In-app only (`Channel == InApp`, else `E-NOTIF-WRONG-CHANNEL`). Idempotent. | `NotificationRead` *(internal)* |
| `Archive()` | In-app only. Soft-delete: `IsArchived = true`. | `NotificationArchived` *(internal)* |
| `Unarchive()` | In-app only; supports undo within the 10-second window. | — |

### 6.2 Core invariants (Notification)

1. **Channel correctness.** `MarkRead` / `Archive` / `Unarchive` are valid only when `Channel == InApp`. `MarkOpened` is valid only for `Email`/`InApp`.
2. **Status machine.** `Pending → {Queued, Sent}`; `Queued → Sent`; `Sent → {Delivered, Bounced, Failed, Complaint}`. No backward transitions. `MarkDelivered` only from `Sent`.
3. **Attempt cap.** At most **3** `DeliveryAttempt`s with a bounce/error outcome. The 3rd failure forces `Failed`.
4. **Render-before-send.** `RecordSendAttempt` requires `Rendered != null` for `Email`/`Sms` (in-app may carry inline content in the payload).
5. **Digest exclusivity.** A notification with a non-null `DigestId` must be in status `Queued` (until the digest dispatches, which transitions it).
6. **High priority bypass.** `HoldForDnd` and `QueueIntoDigest` are illegal when `Priority == High` — critical notifications are always immediate.
7. **SMS body length.** A rendered SMS `BodyText` over 160 chars is allowed but the `RenderedMessage` must record it as multi-segment (cost signal for §8 `NotificationDispatched`).

### 6.3 RecipientPreferences — behaviors

| Method | Rules / invariants enforced | Domain event raised |
|---|---|---|
| `static CreateDefault(userId, role, email)` | Creates with all `ChannelTypePreference`s defaulted: every type **enabled**, `Immediate`, in-app `ToastMode = Toast`; `SmsContact = null`; one `ConsentRecord` for email opt-in (`Method = RegistrationCheckbox`). | — |
| `SetChannelTypePreference(channel, type, enabled, frequency, toastMode)` | Updates the (channel,type) cell. SMS may only be `Immediate` (no SMS digest). `Transactional`/`AccountSecurity` on email cannot be disabled (`E-NOTIF-CRITICAL-LOCKED`). | `NotificationPreferencesUpdated` |
| `SetPreferredEmail(address)` | Address must be a known, `Verified` contact point. | `NotificationPreferencesUpdated` |
| `AddOrReplaceEmailContact(address, verified)` | Sets `EmailContact`; clears `EmailSuppressed` if address changed. | — |
| `ProvidePhoneNumber(e164)` | Sets `SmsContact` with `Verified = false`, `OptedIn = false`. Triggers verification flow (handler sends a verification SMS). | — |
| `ConfirmPhoneNumber()` | Sets `SmsContact.Verified = true`. Only if a phone is on file. | — |
| `OptInSms(method, ip)` | Requires `SmsContact.Verified == true` (`E-NOTIF-PHONE-UNVERIFIED`). Sets `OptedIn = true`, `SmsSuppressed = false`; appends `ConsentRecord` `OptIn`. | `NotificationPreferencesUpdated` |
| `OptOutSms(method, ip)` | Sets `OptedIn = false`; appends `ConsentRecord` `OptOut`. Critical security SMS still allowed (TCPA carve-out). | `NotificationPreferencesUpdated` |
| `SetGlobalEmailOptOut(optedOut, method, ip)` | Toggles `GlobalEmailOptOut`; appends a `ConsentRecord`. Transactional/security email unaffected. | `NotificationPreferencesUpdated` |
| `SetDoNotDisturb(window?)` | Sets/clears `DndWindow`. | `NotificationPreferencesUpdated` |
| `SetTimezone(ianaTz)` | Validates against the IANA tz database. | — |
| `SuppressEmail(reason)` | Sets `EmailSuppressed = true` — called after 3 hard bounces or a complaint. | — |
| `SuppressSms(reason)` | Sets `SmsSuppressed = true`, `SmsContact.OptedIn = false`; appends `ConsentRecord` `OptOut` (`Method` = reason). Called on hard bounce / complaint / STOP / DNC hit. | `NotificationPreferencesUpdated` |
| `CanReceive(channel, type, priority) -> ChannelDecision` | **Central routing rule** — see §6.4. Pure query, raises nothing. | — |

### 6.4 Routing decision logic (`RecipientPreferences.CanReceive`)

Returns a `ChannelDecision` value object: `{ Allowed: bool, Frequency: Frequency, HeldForDnd: bool, Reason: string? }`. Evaluated in this order — **first match wins**:

1. If `priority == High` **and** `type ∈ {AccountSecurity, Transactional}` → **Allowed, Immediate**, ignore everything below (critical carve-out).
2. If `channel == Email` and (`GlobalEmailOptOut` or `EmailSuppressed`) and `type ∉ {Transactional, AccountSecurity}` → **Denied** (`Reason = "email-suppressed"`).
3. If `channel == Sms` and (`SmsContact == null` or not `OptedIn` or not `Verified` or `SmsSuppressed`) → **Denied** (`Reason = "sms-not-opted-in"`).
4. If `channel == Sms` and `type ∉ {AccountSecurity, Transactional}` → **Denied** (`Reason = "sms-non-critical"`) — SMS is critical-only per `US-3.6.3-03 AC-01`.
5. Look up the `ChannelTypePreference` cell. If missing or `Enabled == false` → **Denied** (`Reason = "type-disabled"`).
6. If `channel == InApp` and `ToastMode == Disabled` → **Denied**; if `CenterOnly` → **Allowed but no toast** (still written to the center).
7. If `DndWindow` covers "now" in the user's timezone and `priority == Normal` → **Allowed, HeldForDnd = true**.
8. Otherwise → **Allowed**, with `Frequency` taken from the cell.

This single method is the heart of the BC. Every inbound-event handler calls it once per candidate channel.

### 6.5 NotificationTemplate — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Create(channel, type, name, initialVersion, createdBy)` | Unique on (channel, type). `CurrentVersion = initialVersion` (version 1). `IsActive = true`. | — |
| `PublishNewVersion(version, createdBy)` | `version.VersionNumber == CurrentVersion.VersionNumber + 1`; old `CurrentVersion` pushed to `History`; new content validated (size/char limits, placeholders well-formed). | `TemplateVersionPublished` *(internal)* |
| `RollbackTo(versionNumber, createdBy)` | `versionNumber` must exist in `History`; that version is re-published as a *new* version number (history is append-only — rollback never rewrites). | `TemplateVersionPublished` *(internal)* |
| `Deactivate()` / `Activate()` | toggles `IsActive`. | — |
| `PurgeHistoryOlderThan(cutoffUtc)` | removes `History` versions older than 12 months; never removes `CurrentVersion`. | — |

**Invariants:** `History` is append-only; `CurrentVersion.VersionNumber` is the max of all versions; a template body must declare every placeholder it uses in `Placeholders` (validated at publish so previews and renders never silently fail).

### 6.6 Digest — behaviors

| Method | Rules | Event |
|---|---|---|
| `static Open(userId, channel, window, scheduledSendUtc)` | At most one `Open` digest per (userId, channel, window). Status `Open`. | `DigestScheduled` |
| `Append(notificationId, type, summary, actionUrl)` | Only while `Open`. Adds a `DigestItem`. | — |
| `RemoveExpiredItems(cutoffUtc)` | drops `DigestItem`s queued > 30 days ago. | — |
| `Dispatch()` | Only from `Open`. If `Items` is **empty → `Discarded`**, raise nothing (`US-3.6.1-04 AC-04` "empty digest not sent"). If non-empty → `Dispatched`, set `DispatchedOnUtc`. | `DigestSent` *(only when items present)* |

**Invariant:** an `Open` digest is the only one that accepts `Append`; once `Dispatched`/`Discarded` it is immutable. Each `DigestItem.NotificationId` is unique within a digest.

---

## 7. Domain services

Stateless. Live in `Domain`. Used when logic spans entities or needs a small external input.

### 7.1 `TemplateRenderer`

```
Render(version: TemplateVersion, payload: NotificationPayload, channel: Channel) -> Result<RenderedMessage>
```

Performs `{{placeholder}}` substitution against `payload.Values`. Missing placeholder → substituted with empty string and the result flagged (graceful degradation, never a hard failure — `US-3.6.1-03` assumption). Supports `{{#if key}}...{{/if}}` conditional blocks. For `Channel.Sms`, ignores HTML and validates the output against `SmsBody` rules. For preview (`US-3.6.1-03 AC-03`), the handler passes a sample payload — same code path.

### 7.2 `ChannelFanoutPlanner`

```
Plan(inbound: IntegrationEventEnvelope, prefs: RecipientPreferences) -> list<PlannedNotification>
```

Given one inbound integration event and the recipient's preferences, decides the full set of `(channel, type, priority)` notifications to create. It maps each inbound event type to a `NotificationType` + default `Priority` (the mapping table in §9.1), then calls `prefs.CanReceive(...)` for each of the three channels and emits a `PlannedNotification { Channel, Type, Priority, Frequency, HeldForDnd }` for every `Allowed` result. This is where "one event → up to three notifications" is decided. Pure; no I/O.

### 7.3 `DigestAssembler`

```
Assemble(digest: Digest, digestTemplate: NotificationTemplate, itemPayloads: list<NotificationPayload>) -> Result<RenderedMessage>
```

Builds the single consolidated `RenderedMessage` for a digest from its `DigestItem`s — count summary, per-item rows with action links. Uses the digest-specific template (separate from the immediate-send template, `US-3.6.1-04` assumption). Returns failure only if the digest template is missing.

### 7.4 `FrequencyCapEvaluator`

```
CheckSmsCap(userId: uuid, sentInLast24h: int, priority: Priority, capPerDay: int) -> Result
```

Enforces `US-3.6.3-03 AC-03` — max N SMS per user per rolling 24h (default 5). `High`/critical SMS bypass the cap. Returns `E-NOTIF-SMS-CAP-EXCEEDED` when a `Normal` SMS would breach the cap (the handler then queues it for the next window rather than dropping it). The `sentInLast24h` count is supplied by the caller from the log repository — the service stays pure.

### 7.5 `DndScheduleCalculator`

```
NextReleaseTimeUtc(window: DndWindow, ianaTimezone: string, nowUtc: datetime) -> datetime?
CheckSmsSendWindow(ianaTimezone: string, nowUtc: datetime) -> ComplianceWindowResult
```

First method: given a DND window and the user's timezone, returns the UTC instant the hold should release (or `null` if "now" is outside the window). Second method enforces the **TCPA quiet-hours rule** (`US-3.6.3-05 AC-01`): no SMS before 08:00 or after 21:00 in the *recipient's* timezone — returns the next legal send time if "now" is outside 08:00–21:00 local.

---

## 8. Domain events published

Two categories. **Internal domain events** (`DomainEvent`) are handled in-process within this module. **Integration events** (`IntegrationEvent`) cross the BC boundary — they go through the **outbox** ([[00-Shared-Foundations]] §6.2) and must match the [[Event_Catalog]] contract exactly. BC-10 Reporting depends on these payloads — do not change a field without versioning.

### 8.1 Integration events — PUBLISHED (live in the `Contracts` surface)

| Integration event | Raised when | Payload |
|---|---|---|
| `NotificationDispatchedIntegrationEvent` | `Notification.RecordSendAttempt` succeeds | `NotificationId`, `Channel` (`string`), `RecipientUserId`, `TemplateId?`, `NotificationType` (`string`), `IsMultiSegmentSms` (`bool`), `OccurredOnUtc` |
| `NotificationDeliveredIntegrationEvent` | `Notification.MarkDelivered` | `NotificationId`, `Channel` (`string`), `DeliveredOnUtc`, `OccurredOnUtc` |
| `NotificationFailedIntegrationEvent` | `RecordHardBounce` / 3rd `RecordSoftBounce` / `RecordProviderError` exhausted / `MarkComplaint` | `NotificationId`, `Channel` (`string`), `Reason` (`string`), `FailureKind` (`string`: `HardBounce`/`SoftBounceExhausted`/`ProviderError`/`Complaint`), `OccurredOnUtc` |
| `NotificationPreferencesUpdatedIntegrationEvent` | any `RecipientPreferences` mutation that changes a preference/consent | `UserId`, `Channel` (`string`), `ChangeSummary` (`string`), `OccurredOnUtc` |
| `DigestScheduledIntegrationEvent` | `Digest.Open` | `DigestId`, `UserId`, `Channel` (`string`), `Window` (`string`), `ScheduledSendUtc`, `OccurredOnUtc` |
| `DigestSentIntegrationEvent` | `Digest.Dispatch` with items present | `DigestId`, `UserId`, `Channel` (`string`), `ItemCount` (`int`), `OccurredOnUtc` |

Consumers (for context only — you do not code them): **BC-10 Reporting** consumes all six (notification volume, delivery rate, bounce rate, cost tracking — it builds the SMS admin dashboard of `US-3.6.3-04 AC-07`). No other BC subscribes to BC-9.

### 8.2 Internal domain events (NOT published outside the module)

`NotificationCreated`, `NotificationRead`, `NotificationArchived`, `TemplateVersionPublished`. Use these for in-module reactions (e.g., `NotificationCreated` → update the recipient's unread-count read-model; `NotificationRead` → decrement it; `TemplateVersionPublished` → invalidate the render cache). They never reach the outbox.

---

## 9. Integration contracts — what this BC consumes & calls

Everything this module needs from neighbours, reproduced so this package stays self-contained for its domain. (Outbox/inbox mechanics: [[00-Shared-Foundations]] §6.2–6.3.)

### 9.1 Integration events CONSUMED (this module subscribes; write idempotent handlers)

This BC is a **Conformist** — it subscribes broadly and accepts upstream payloads as-is. Every consumed event is mapped to a `NotificationType` + default `Priority`, then `ChannelFanoutPlanner` (§7.2) decides which channels fire. The handler is the same shape for every row: dedupe on `EventId` via the inbox → load/create `RecipientPreferences` → plan → create `Notification`s → render or queue → persist.

| Integration event | From | Payload you receive | Maps to NotificationType / Priority | Your reaction |
|---|---|---|---|---|
| `UserRegisteredIntegrationEvent` | BC-1 | `UserId`, `Role`, `Email`, `CreatedAtUtc` | `Transactional` / `High` | **Create `RecipientPreferences` for this `UserId`** (`CreateDefault`). Then send a welcome notification. |
| `UserAccountActivatedIntegrationEvent` | BC-1 | `UserId`, `ActivatedOnUtc` | `Transactional` / `Normal` | Send "account activated" confirmation. |
| `UserAccountSuspendedIntegrationEvent` | BC-1 | `UserId`, `Reason`, `By`, `At` | `AccountSecurity` / `High` | Notify the user their account was suspended. |
| `UserAccountReinstatedIntegrationEvent` | BC-1 | `UserId`, `By`, `At` | `AccountSecurity` / `Normal` | Notify reinstatement. |
| `AccountDeactivatedIntegrationEvent` | BC-1 | `UserId`, `DeactivatedOnUtc` | `Transactional` / `Normal` | Send a confirmation; **stop all future non-critical sends** (treat like global opt-out until reinstated). |
| `UserLoginFailedIntegrationEvent` | BC-1 | `Identifier`, `Reason`, `At` | `AccountSecurity` / `High` | Security alert SMS+email (bypasses opt-out). De-dupe bursts per `US-3.6.3-03 AC-02`. |
| `PasswordResetIntegrationEvent` | BC-1 | `UserId`, `At` | `AccountSecurity` / `High` | Send "your password was reset" confirmation. |
| `OtpRequestedIntegrationEvent` | BC-1 | `UserId`, `Mobile`, `Email`, `OtpCode`, `Purpose` (`Registration`/`PasswordReset`/`Mfa`), `ExpiresOnUtc` | `Transactional` / `High` | **Deliver the BC-1-generated code** by SMS (and email if `Purpose == Registration`). This module never generates the code. |
| `RoleAssignedIntegrationEvent` | BC-1 | `UserId`, `Role`, `By`, `At` | `AccountSecurity` / `Normal` | Notify role change. |
| `EmployerVerifiedIntegrationEvent` | BC-2 | `EmployerId`, `UserId`, `VerifiedAtUtc`, `EvidenceRef` | `ApplicationUpdate` / `Normal` | Notify the employer they are verified. |
| `EmployerVerificationFailedIntegrationEvent` | BC-2 | `EmployerId`, `UserId`, `Reason`, `At` | `ApplicationUpdate` / `High` | Notify the employer verification failed + next steps. |
| `CandidateSavedToTalentPoolIntegrationEvent` | BC-2 | `EmployerId`, `JobSeekerUserId`, `PoolId`, `At` | `RecruiterActivity` / `Normal` | Notify the job seeker a recruiter saved them. |
| `JobPostingExpiredIntegrationEvent` | BC-4 | `PostingId`, `EmployerUserId`, `ExpiredAtUtc` | `ApplicationUpdate` / `Normal` | Notify the employer their posting expired. |
| `JobPostingClosedIntegrationEvent` | BC-4 | `PostingId`, `EmployerUserId`, `Reason`, `At` | `ApplicationUpdate` / `Normal` | Notify employer + applicants the posting closed. |
| `JobPostingSuspendedIntegrationEvent` | BC-4 | `PostingId`, `EmployerUserId`, `By`, `Reason`, `At` | `ApplicationUpdate` / `High` | Notify employer the posting was suspended by moderation. |
| `ApplicationSubmittedIntegrationEvent` | BC-5 | `ApplicationId`, `JobSeekerUserId`, `PostingId`, `EmployerUserId`, `At` | `ApplicationUpdate` / `Normal` | Two notifications: confirm to seeker, alert employer. |
| `ApplicationStatusChangedIntegrationEvent` | BC-5 | `ApplicationId`, `JobSeekerUserId`, `FromStatus`, `ToStatus`, `By`, `At` | `ApplicationUpdate` / `Normal` (`High` if `ToStatus == Hired`) | Notify the seeker of the status change. |
| `SavedSearchMatchFoundIntegrationEvent` | BC-6 | `SavedSearchId`, `UserId`, `PostingIds` (`list<uuid>`), `At` | `JobRecommendation` / `Normal` | Build a saved-search alert (digest-eligible). |
| `RecommendationGeneratedIntegrationEvent` | BC-7 | `JobSeekerUserId`, `PostingIds` (`list<uuid>`), `ComputedAtUtc` | `JobRecommendation` / `Normal` | Build the **weekly recommendation** in-app notification + email digest (`US-3.6.2-03`). |
| `CandidateRecommendationGeneratedIntegrationEvent` | BC-7 | `EmployerUserId`, `PostingId`, `JobSeekerUserIds` (`list<uuid>`), `At` | `RecruiterActivity` / `Normal` | Notify the employer ranked candidates are ready. |
| `ProfileCompletenessChangedIntegrationEvent` | BC-3 | `JobSeekerProfileId`, `JobSeekerUserId`, `Score` (`int`), `At` | `ProfileView` / `Normal` (treated as a profile-nudge type) | Send a completeness nudge **only on milestone crossings** (50/75/100) — suppress otherwise to avoid spam. |
| `SyncErrorDetectedIntegrationEvent` | BC-8 | `PartnerId`, `ErrorClass`, `PayloadRef`, `At` | `Announcement` / `High` | Page ops — deliver to the admin recipient group (email + SMS). |
| `ArticlePublishedIntegrationEvent` | BC-12 | `ArticleId`, `Title`, `Categories` (`list<string>`), `At` | `Announcement` / `Normal` | Build a news-digest item for users opted in to announcements. |

**Idempotency** is mandatory — see [[00-Shared-Foundations]] §6.3 (inbox). Inbound events arrive via the module's integration-event subscription wired in the module composition entry point.

**Note on the OTP event:** [[Event_Catalog]] lists BC-1 emitting `UserRegistered`/`PasswordReset` etc. but does **not** list a dedicated OTP-carrying event. This package introduces `OtpRequestedIntegrationEvent` as the explicit contract by which BC-1 hands a generated code to BC-9 for delivery — because the stories (`US-3.6.3-01 AC-01/02/04`) require this module to *send* a code it does not *own*. BC-1's package (`BC-01-IAM-and-UAM.md`) publishes it. This is the clean seam: BC-1 generates and validates, BC-9 delivers.

### 9.2 Public APIs this module CALLS (port interfaces — define in `Application`, implement adapters in `Infrastructure`)

```
Port: EmailChannel              (external email transport — orchestrated, not implemented, here)
  Send(request: EmailSendRequest) -> Result<string>     // returns provider message id; honours the unsubscribe header
  EmailSendRequest {
    ToAddress: string, FromName: string, FromAddress: string, ReplyToAddress: string,
    Subject: string, BodyHtml: string, BodyText: string,
    ListUnsubscribeUrl: string, SenderPostalAddress: string
  }

Port: SmsChannel                (external SMS transport — orchestrated, not implemented, here)
  Send(request: SmsSendRequest) -> Result<string>       // returns provider message id
  SmsSendRequest { ToE164: string, SenderId: string, Body: string }

Port: RealtimePush              (real-time push hub for in-app toasts — WebSocket/SSE — infrastructure, not built here)
  PushToast(userId: uuid, toast: InAppToastDto) -> Result   // no-op success if user is not connected
  InAppToastDto { NotificationId: uuid, Type: string, Title: string, Body: string, ActionUrl: string?, Priority: string }

Port: DncRegistry               (national Do-Not-Call / Do-Not-Contact registry checker — US-3.6.3-05 AC-09)
  IsRegistered(e164Number: string) -> bool

// Delivery-status webhooks from the email/SMS providers arrive at the API layer and are
// translated into commands (RecordDeliveryStatusCommand) — no port needed for inbound webhooks.
```

For the exercise, `Infrastructure` may provide **stub adapters** for `EmailChannel`, `SmsChannel`, `RealtimePush`, and `DncRegistry` (in-memory / always-succeed) so the module runs standalone. Keep the port shapes exactly as above so real adapters drop in later.

This module **does not call any other BC's public API synchronously.** All cross-BC input arrives as events. (Contrast with BC-3/BC-2 which call BC-1's `IdentityProvisioningApi` synchronously during registration.)

### 9.3 Public API this module EXPOSES (in the `Contracts` surface)

```
Public API: NotificationPublicApi
  GetUnreadCount(userId: uuid) -> int
      // used by any BC building a UI that shows an unread badge without going through the REST API
  GetDeliveryLog(userId: uuid, fromUtc: date, toUtc: date) -> list<NotificationLogEntryDto>
      // used by BC-1's admin user-management screens (US-3.1.4-01) to show a user's comms history

NotificationLogEntryDto {
  NotificationId: uuid, Channel: string, Type: string, DeliveryStatus: string,
  CreatedOnUtc: datetime, DeliveredOnUtc: datetime?
}
```

---

## 10. Application layer

Every use case is a **command** or a **query** with exactly one handler. Commands return `Result`/`Result<T>`; queries return DTOs. Mediator dispatch, the validation step, domain-event dispatch, and the outbox all follow [[00-Shared-Foundations]] §6.

### 10.1 Commands

| Command | Story | Handler responsibilities |
|---|---|---|
| `IngestIntegrationEventCommand` | all consumed events | The generic intake. Inbox-dedupe on `EventId` → load/`CreateDefault` `RecipientPreferences` → `ChannelFanoutPlanner.Plan` → for each `PlannedNotification`: `Notification.Create`; if `Frequency == Immediate` and not held → resolve template, `Render`, hand to channel adapter via the outbox-relayed send; if digest-eligible → find/`Open` the `Digest`, `QueueIntoDigest` + `Digest.Append`; if `HeldForDnd` → `HoldForDnd` with `DndScheduleCalculator.NextReleaseTimeUtc` → persist all in one transaction. |
| `SendImmediateNotificationCommand` | US-3.6.1-01/04, US-3.6.2-02, US-3.6.3-01 | Internal command queued by the intake handler / DND-release worker. Loads the `Notification`, picks the channel adapter, for SMS runs `DndScheduleCalculator.CheckSmsSendWindow` + `FrequencyCapEvaluator.CheckSmsCap` + `DncRegistry.IsRegistered` first, calls the port, `RecordSendAttempt` / bounce methods, persists. For in-app, also calls `RealtimePush.PushToast` unless `ToastMode == CenterOnly`. |
| `RecordDeliveryStatusCommand` | US-3.6.1-05, US-3.6.3-04 | Driven by provider webhooks. Loads `Notification` by `ProviderMessageId`, applies `MarkDelivered` / `RecordSoftBounce` / `RecordHardBounce` / `MarkOpened` / `MarkClicked` / `MarkComplaint`. On hard bounce ×3 or complaint also calls `RecipientPreferences.SuppressEmail`/`SuppressSms`. |
| `RetrySoftBouncedNotificationCommand` | US-3.6.1-05 AC-03, US-3.6.3-04 AC-04 | Driven by the retry worker. Re-attempts a `Sent` notification with a `SoftBounce` last attempt and `AttemptNumber < 3`. Retry schedule: 1h, 6h, 24h. |
| `DispatchDigestCommand` | US-3.6.1-04 | Driven by the digest scheduler worker at each window. Loads the `Open` `Digest`, removes expired items, if empty → `Discard`, else `DigestAssembler.Assemble` → send via channel adapter → `Digest.Dispatch` → transition each item's `Notification`. |
| `MarkNotificationReadCommand` | US-3.6.2-07 AC-01 | Load in-app `Notification` (must belong to caller) → `MarkRead` → persist. |
| `MarkAllNotificationsReadCommand` | US-3.6.2-07 AC-02 | Bulk `MarkRead` over the caller's unread in-app notifications. |
| `ArchiveNotificationCommand` | US-3.6.2-07 AC-03 | Load → `Archive` → persist. Returns an undo token valid 10s. |
| `ArchiveNotificationsBatchCommand` | US-3.6.2-07 AC-04 | Up to 50 ids; bulk `Archive`. |
| `UndoArchiveNotificationCommand` | US-3.6.2-07 AC-07 | Within 10s of archive → `Unarchive`. |
| `SetEmailNotificationPreferencesCommand` | US-3.6.1-02 | Load `RecipientPreferences` → `SetChannelTypePreference` per toggle + `SetPreferredEmail` → persist. |
| `SetInAppNotificationPreferencesCommand` | US-3.6.2-05 | `SetChannelTypePreference` (toast mode) + `SetDoNotDisturb` → persist. |
| `SetGlobalEmailOptOutCommand` | US-3.6.1-02 AC-05, US-3.6.1-06 AC-02 | `SetGlobalEmailOptOut` → persist. Also the target of the one-click unsubscribe link. |
| `ProvidePhoneNumberCommand` | US-3.6.3-02 AC-01/02/06 | `ProvidePhoneNumber` → send verification SMS via `SmsChannel` → persist. |
| `ConfirmPhoneNumberCommand` | US-3.6.3-02 AC-04 | Validates the code the user typed against the pending verification → `ConfirmPhoneNumber` → persist. (The verification code here is generated by *this* module for phone-ownership proof — distinct from BC-1's auth OTP.) |
| `SetSmsOptInCommand` | US-3.6.3-02 AC-03/05, US-3.6.3-05 AC-02 | `OptInSms` / `OptOutSms` with `Method` + caller IP → persist. |
| `HandleSmsStopKeywordCommand` | US-3.6.3-05 AC-03/04 | Driven by an inbound-SMS webhook. Matches STOP/OPT OUT → `OptOutSms(SmsStopKeyword)` → send opt-out confirmation. |
| `CreateTemplateCommand` | US-3.6.1-03 AC-01/05 | Admin only. `NotificationTemplate.Create` with version 1 → persist. |
| `PublishTemplateVersionCommand` | US-3.6.1-03 AC-02/04 | Admin only. Validate placeholders/size → `PublishNewVersion` → persist. |
| `RollbackTemplateCommand` | US-3.6.1-03 AC-04 | Admin only. `RollbackTo(versionNumber)` → persist. |

### 10.2 Queries

| Query | Story | Returns |
|---|---|---|
| `GetNotificationCenterQuery` | US-3.6.2-01 | `NotificationCenterDto` — page of the caller's in-app notifications (last 100, 20/page, load-more cursor), unread count badge. |
| `GetNotificationHistoryQuery` | US-3.6.2-04 | `list<NotificationDto>` — 90-day history, filterable by `Type`, keyword-searchable on subject/body. |
| `GetUnreadCountQuery` | US-3.6.2-01 AC-03 | `int` — backs the bell badge; also the `NotificationPublicApi` method. |
| `GetMyNotificationPreferencesQuery` | US-3.6.1-02, US-3.6.2-05 | `NotificationPreferencesDto` — full per-channel/per-type grid + DND + contacts + opt-out flags. |
| `PreviewTemplateQuery` | US-3.6.1-03 AC-03 | `RenderedMessageDto` — renders a template version against a supplied sample payload. Admin only. |
| `GetTemplateQuery` / `ListTemplatesQuery` | US-3.6.1-03 | template content + version history. Admin only. |
| `GetDeliveryLogQuery` | US-3.6.1-05 AC-04, US-3.6.3-04 AC-06 | `list<NotificationLogEntryDto>` — queryable by user id / date range / channel / status. Admin only; also the `NotificationPublicApi` method. |

### 10.3 Validators — representative rules

- `SetEmailNotificationPreferencesCommand`: every toggle's `type` in the enum; `frequency` in the enum; if any toggled type is `Transactional`/`AccountSecurity` and `enabled == false` → reject with `E-NOTIF-CRITICAL-LOCKED`.
- `ProvidePhoneNumberCommand`: phone is valid E.164 (`E-NOTIF-INVALID-PHONE`).
- `SetSmsOptInCommand`: if opting in, a `Verified` phone must be on file (`E-NOTIF-PHONE-UNVERIFIED`).
- `PublishTemplateVersionCommand`: `bodyHtml` ≤ 100 KB and ≤ 50 000 chars (`E-NOTIF-TEMPLATE-TOO-LARGE`); every `{{placeholder}}` in the body is declared in `placeholders`; `versionNumber` is exactly current + 1.
- `ArchiveNotificationsBatchCommand`: ≤ 50 ids.
- `IngestIntegrationEventCommand`: `EventId` non-empty; `EventType` recognised (unknown event types are logged and dropped, not errored — Conformist resilience).
- `MarkNotificationReadCommand` / `ArchiveNotificationCommand`: notification id present (ownership is checked in the handler against the caller's `UserId` from the access token).

### 10.4 DTOs

Plain data records in `Application`. Never expose Domain entities/VOs across the API. Map aggregate → DTO in the handler or a mapping helper. Key DTOs: `NotificationDto`, `NotificationCenterDto`, `NotificationPreferencesDto`, `RenderedMessageDto`, `TemplateDto`, `NotificationLogEntryDto`, `InAppToastDto` (also in Contracts).

---

## 11. Persistence & data model

Schema/namespace: `notification`. Follows the persistence rules, neutral type vocabulary, and standard infrastructure tables in **[[00-Shared-Foundations]] §3 and §6** — including the standard `outbox_messages` and `inbox_messages` tables (§6.5), which are **not** repeated below. The module-specific relational model follows.

### 11.1 Reference relational model — schema `notification`

```
TABLE notifications
  id                    uuid        PK
  recipient_user_id     uuid        NOT NULL                -- BC-1 identity, no FK
  channel               enum        NOT NULL                -- Email|InApp|Sms
  type                  enum        NOT NULL                -- JobRecommendation|ApplicationUpdate|...
  priority              enum        NOT NULL                -- High|Normal
  source_event          json        NOT NULL                -- SourceEventRef VO
  payload               json        NOT NULL                -- NotificationPayload VO
  template_id           uuid        NULL                    -- references notification_templates.id
  rendered              json        NULL                    -- RenderedMessage VO
  delivery_status       enum        NOT NULL                -- Pending|Queued|Sent|Delivered|Bounced|Failed|Complaint
  engagement            json        NOT NULL                -- EngagementState VO
  scheduled_for_utc     datetime    NULL
  is_read               bool        NOT NULL DEFAULT false
  is_archived           bool        NOT NULL DEFAULT false
  digest_id             uuid        NULL                    -- references digests.id
  provider_message_id   string      NULL                    -- denormalised from latest attempt, for webhook lookup
  created_on_utc        datetime    NOT NULL
  updated_on_utc        datetime    NOT NULL
  version_token         (optimistic-concurrency token — see [[00-Shared-Foundations]] §6.4)
  INDEX (recipient_user_id) WHERE channel = 'InApp' AND is_read = false AND is_archived = false
  INDEX (recipient_user_id, created_on_utc DESC)
  INDEX (provider_message_id)
  INDEX (delivery_status)
  INDEX (scheduled_for_utc) WHERE scheduled_for_utc IS NOT NULL

TABLE delivery_attempts
  id                    uuid        PK
  notification_id       uuid        NOT NULL                -- FK → notifications.id ON DELETE CASCADE
  attempt_number        int         NOT NULL
  channel               enum        NOT NULL
  outcome               enum        NOT NULL                -- Succeeded|SoftBounce|HardBounce|ProviderError
  provider_message_id   string      NULL
  provider_response     string      NULL
  attempted_on_utc      datetime    NOT NULL
  INDEX (notification_id)

TABLE recipient_preferences
  id                    uuid        PK
  user_id               uuid        NOT NULL UNIQUE         -- BC-1 identity, no FK
  role                  string      NOT NULL
  email_contact         json        NOT NULL                -- EmailContactPoint VO
  sms_contact           json        NULL                    -- PhoneContactPoint VO
  timezone              string      NOT NULL DEFAULT 'Asia/Dhaka'
  do_not_disturb        json        NULL                    -- DndWindow VO
  global_email_opt_out  bool        NOT NULL DEFAULT false
  email_suppressed      bool        NOT NULL DEFAULT false
  sms_suppressed        bool        NOT NULL DEFAULT false
  created_on_utc        datetime    NOT NULL
  updated_on_utc        datetime    NOT NULL
  version_token         (optimistic-concurrency token)
  INDEX (user_id)

TABLE channel_type_preferences
  id                            uuid        PK
  recipient_preferences_id      uuid        NOT NULL        -- FK → recipient_preferences.id ON DELETE CASCADE
  channel                       enum        NOT NULL
  type                          enum        NOT NULL
  enabled                       bool        NOT NULL DEFAULT true
  frequency                     enum        NOT NULL DEFAULT 'Immediate'  -- Immediate|DailyDigest|WeeklyDigest
  toast_mode                    enum        NULL            -- Toast|CenterOnly|Disabled (in-app only)
  UNIQUE (recipient_preferences_id, channel, type)

TABLE consent_records
  id                            uuid        PK
  recipient_preferences_id      uuid        NOT NULL        -- FK → recipient_preferences.id ON DELETE CASCADE
  channel                       enum        NOT NULL
  decision                      enum        NOT NULL        -- OptIn|OptOut
  method                        string      NOT NULL        -- RegistrationCheckbox|SettingsToggle|SmsStopKeyword|AdminAction
  ip_address                    string      NULL
  recorded_on_utc               datetime    NOT NULL
  INDEX (recipient_preferences_id, recorded_on_utc)

TABLE notification_templates
  id                    uuid        PK
  channel               enum        NOT NULL
  type                  enum        NOT NULL
  name                  string      NOT NULL
  current_version       json        NOT NULL                -- TemplateVersion VO
  is_active             bool        NOT NULL DEFAULT true
  created_on_utc        datetime    NOT NULL
  updated_on_utc        datetime    NOT NULL
  UNIQUE (channel, type)

TABLE template_versions
  id                    uuid        PK
  template_id           uuid        NOT NULL                -- FK → notification_templates.id ON DELETE CASCADE
  version_number        int         NOT NULL
  subject               string      NULL
  body_html             string      NOT NULL
  body_text             string      NOT NULL
  footer                string      NOT NULL
  placeholders          json        NOT NULL
  created_by_user_id    uuid        NOT NULL
  created_on_utc        datetime    NOT NULL
  UNIQUE (template_id, version_number)
  INDEX (template_id, version_number DESC)

TABLE digests
  id                    uuid        PK
  user_id               uuid        NOT NULL                -- BC-1 identity, no FK
  channel               enum        NOT NULL
  window                enum        NOT NULL                -- Daily|Weekly
  status                enum        NOT NULL                -- Open|Dispatched|Discarded
  opened_on_utc         datetime    NOT NULL
  scheduled_send_utc    datetime    NOT NULL
  dispatched_on_utc     datetime    NULL
  INDEX (user_id, channel, window) WHERE status = 'Open'
  INDEX (scheduled_send_utc) WHERE status = 'Open'

TABLE digest_items
  id                    uuid        PK
  digest_id             uuid        NOT NULL                -- FK → digests.id ON DELETE CASCADE
  notification_id       uuid        NOT NULL
  type                  enum        NOT NULL
  summary               string      NOT NULL
  action_url            string      NULL
  queued_on_utc         datetime    NOT NULL
  UNIQUE (digest_id, notification_id)

(plus the standard outbox_messages and inbox_messages tables — see [[00-Shared-Foundations]] §6.5)
```

### 11.2 Module-specific mapping notes

- Child collections (`delivery_attempts`, `channel_type_preferences`, `consent_records`, `template_versions`, `digest_items`) are **owned** by their aggregate root and loaded with it.
- Scalar columns (`channel`, `type`, `priority`, `delivery_status`, `provider_message_id`, `user_id`) are flattened out of `json` because they are queried, indexed, or uniquely constrained.
- Optimistic-concurrency tokens are required on `notifications` and `recipient_preferences` (both see concurrent writes — provider webhooks vs. user actions).
- The outbox relay also drives actual channel sends triggered by `SendImmediateNotificationCommand`, so a crash mid-send never loses the intent.
- **Archival**: rows in `notifications` older than 90 days are moved (in-app) / `delivery_attempts` + log rows older than 3 years are moved to cold storage by the retention worker — never hard-deleted (`US-3.6.1-05 AC-06`, `US-3.6.3-04 AC-08`).

### 11.3 Repositories (interfaces in `Application`, adapters in `Infrastructure`)

`NotificationRepository` (`GetById`, `GetByProviderMessageId`, `GetInAppForUser`, `CountUnreadForUser`, `GetDueScheduled`, `Add`, `Update`), `RecipientPreferencesRepository` (`GetByUserId`, `Add`, `Update`, `CountSmsSentInLast24h`), `NotificationTemplateRepository` (`GetByChannelAndType`, `GetById`, `ListAll`, `Add`, `Update`), `DigestRepository` (`GetOpen`, `GetDue`, `Add`, `Update`), `NotificationLogRepository` (`Query` by user/date/channel/status), `UnitOfWork` (`SaveChanges`).

### 11.4 Background workers

Listed in §3 module-specific notes. Each is a long-running component registered in the module composition entry point: outbox relay (also drives queued channel sends), digest scheduler, DND-release worker, retry worker (1h/6h/24h), nightly retention worker.

---

## 12. API / presentation contract

HTTP endpoints in the API layer, built with the chosen application framework. Route prefix `/api/notifications`. All endpoints except the unsubscribe link and the provider webhooks require a valid access token (issued by BC-1); the authenticated `UserId` is taken from the token. Admin endpoints additionally require the `MoLAdministrator` role. Map `Result` failures to problem-details / structured error responses preserving the `Error.Code`.

| Verb & route | Command/Query | Success | Notable failures |
|---|---|---|---|
| `GET /api/notifications` | `GetNotificationCenterQuery` | `200` + `NotificationCenterDto` | |
| `GET /api/notifications/history` | `GetNotificationHistoryQuery` | `200` + list | |
| `GET /api/notifications/unread-count` | `GetUnreadCountQuery` | `200` + `{ count }` | |
| `POST /api/notifications/{id}/read` | `MarkNotificationReadCommand` | `204` | `404`, `409 E-NOTIF-WRONG-CHANNEL` |
| `POST /api/notifications/read-all` | `MarkAllNotificationsReadCommand` | `204` | |
| `DELETE /api/notifications/{id}` | `ArchiveNotificationCommand` | `200` + `{ undoToken }` | `404` |
| `DELETE /api/notifications` (body: id list) | `ArchiveNotificationsBatchCommand` | `204` | `400` > 50 ids |
| `POST /api/notifications/{id}/undo-archive` | `UndoArchiveNotificationCommand` | `204` | `409` undo window expired |
| `GET /api/notifications/preferences` | `GetMyNotificationPreferencesQuery` | `200` + `NotificationPreferencesDto` | |
| `PUT /api/notifications/preferences/email` | `SetEmailNotificationPreferencesCommand` | `200` | `400 E-NOTIF-CRITICAL-LOCKED` |
| `PUT /api/notifications/preferences/in-app` | `SetInAppNotificationPreferencesCommand` | `200` | `400` |
| `POST /api/notifications/preferences/email/opt-out` | `SetGlobalEmailOptOutCommand` | `200` | |
| `GET /unsubscribe?token={t}` *(anonymous)* | `SetGlobalEmailOptOutCommand` | `200` confirmation page | `400` bad/expired token |
| `POST /api/notifications/phone` | `ProvidePhoneNumberCommand` | `202` verification SMS sent | `400 E-NOTIF-INVALID-PHONE` |
| `POST /api/notifications/phone/confirm` | `ConfirmPhoneNumberCommand` | `200` | `400` wrong/expired code |
| `PUT /api/notifications/preferences/sms` | `SetSmsOptInCommand` | `200` | `409 E-NOTIF-PHONE-UNVERIFIED` |
| `POST /api/notifications/templates` | `CreateTemplateCommand` | `201` + id | `403`, `409` duplicate (channel,type) |
| `PUT /api/notifications/templates/{id}/versions` | `PublishTemplateVersionCommand` | `201` + version number | `403`, `400 E-NOTIF-TEMPLATE-TOO-LARGE` |
| `POST /api/notifications/templates/{id}/rollback` | `RollbackTemplateCommand` | `200` | `403`, `404` version unknown |
| `POST /api/notifications/templates/{id}/preview` | `PreviewTemplateQuery` | `200` + `RenderedMessageDto` | `403` |
| `GET /api/notifications/templates` / `/{id}` | `ListTemplatesQuery` / `GetTemplateQuery` | `200` | `403` |
| `GET /api/notifications/admin/log` | `GetDeliveryLogQuery` | `200` + log entries | `403` |
| `POST /api/notifications/webhooks/email` *(provider auth, not access token)* | `RecordDeliveryStatusCommand` | `200` | `401` bad signature |
| `POST /api/notifications/webhooks/sms` *(provider auth)* | `RecordDeliveryStatusCommand` / `HandleSmsStopKeywordCommand` | `200` | `401` |

Internal commands (`IngestIntegrationEventCommand`, `SendImmediateNotificationCommand`, `DispatchDigestCommand`, `RetrySoftBouncedNotificationCommand`) have **no HTTP route** — they are issued by the inbox subscription and background workers.

---

## 13. Test requirements

Follows the three-layer testing strategy, coverage target, and tooling roles in **[[00-Shared-Foundations]] §7**. Module-specific test cases below.

### 13.1 Unit tests — Domain (pure)

- **Value objects:** every VO factory — valid input succeeds; each invalid case returns the right `Error`. Specifically: `EmailContactPoint` (RFC 5322), `PhoneContactPoint` (E.164/+880), `TemplateVersion` (body > 100 KB fails, > 50 000 chars fails, version number not increasing fails), `SmsBody` (>160 chars flagged multi-segment, not rejected), `DndWindow` (midnight-wrapping window is valid).
- **Notification aggregate:**
  - Status machine: every legal transition succeeds; every illegal one (`Delivered → Sent`, `MarkDelivered` from `Pending`, etc.) returns failure.
  - `MarkRead`/`Archive`/`Unarchive` fail with `E-NOTIF-WRONG-CHANNEL` for an `Email` or `Sms` notification; succeed for `InApp`.
  - Attempt cap: a 4th bounce attempt is rejected; the 3rd `SoftBounce` flips status to `Failed` and raises `NotificationFailed`.
  - `HoldForDnd` and `QueueIntoDigest` fail when `Priority == High`.
  - `RecordSendAttempt` fails for `Email`/`Sms` when `Rendered == null`.
- **RecipientPreferences aggregate & `CanReceive` routing:** table-driven over §6.4 — prove first-match-wins ordering. Specifically: a `High` `AccountSecurity` email is `Allowed` even with `GlobalEmailOptOut == true`; a `Normal` `JobRecommendation` email is `Denied` under global opt-out; a non-critical SMS type is always `Denied`; SMS is `Denied` when phone is unverified or not opted-in; a `Normal` notification inside the DND window returns `HeldForDnd = true`; `CenterOnly` toast mode returns `Allowed` with no toast.
- `SetChannelTypePreference` disabling `Transactional` email fails with `E-NOTIF-CRITICAL-LOCKED`.
- `OptInSms` fails with `E-NOTIF-PHONE-UNVERIFIED` when the phone is not `Verified`; succeeds and appends a `ConsentRecord` when it is.
- **NotificationTemplate:** `PublishNewVersion` rejects a non-sequential version number; rejects a body using an undeclared placeholder; `RollbackTo` an unknown version fails; rollback creates a *new* version number (history untouched).
- **Digest:** at most one `Open` digest per (user, channel, window); `Dispatch` on an empty digest → `Discarded`, no `DigestSent` event; `Dispatch` with items → `Dispatched`, raises `DigestSent`.
- **Domain services:** `TemplateRenderer` — placeholder substitution, missing placeholder degrades to empty + flag, `{{#if}}` blocks, SMS strips HTML. `ChannelFanoutPlanner` — one event maps to exactly the channels `CanReceive` allows. `FrequencyCapEvaluator` — `Normal` SMS over cap → `E-NOTIF-SMS-CAP-EXCEEDED`; `High` SMS bypasses. `DndScheduleCalculator` — `CheckSmsSendWindow` rejects 22:00 local, returns next 08:00; `NextReleaseTimeUtc` correct across a timezone offset.

### 13.2 Unit tests — Application (handlers, ports replaced with test doubles)

- `IngestIntegrationEventCommand`: `UserRegistered` creates a `RecipientPreferences` then a welcome notification; a duplicate `EventId` is a no-op (inbox dedupe); an unknown event type is logged and dropped without error; an event for a user whose preference disables that type produces **no** notification.
- `IngestIntegrationEventCommand` for `OtpRequestedIntegrationEvent`: creates a `Transactional`/`High` SMS notification carrying the BC-1-supplied code; the module never generates a code itself.
- `SendImmediateNotificationCommand`: clean email → `EmailChannel` called, `RecordSendAttempt`, `NotificationDispatched` queued to outbox; SMS outside 08:00–21:00 local → not sent, rescheduled; SMS over the frequency cap → queued not dropped; DNC-listed number → not sent, suppressed.
- `RecordDeliveryStatusCommand`: a hard bounce marks the notification `Bounced` and, on the 3rd hard bounce for that address, calls `SuppressEmail`; a complaint forces `Complaint` + suppression + a `ConsentRecord`.
- `DispatchDigestCommand`: an empty digest is discarded and **no email is sent** (`US-3.6.1-04 AC-04`); a non-empty digest renders via `DigestAssembler`, sends once, transitions every item.
- `SetSmsOptInCommand`: opting in without a verified phone returns `E-NOTIF-PHONE-UNVERIFIED`.
- `PublishTemplateVersionCommand`: oversized body → `E-NOTIF-TEMPLATE-TOO-LARGE`; undeclared placeholder rejected; non-admin caller → `403` (authorization filter).
- `ArchiveNotificationCommand` + `UndoArchiveNotificationCommand`: archive returns an undo token; undo within 10s restores; undo after 10s → `409`.
- Validation step: each validator rejects the documented bad inputs before the handler runs.

### 13.3 Integration tests (real database in a container)

- **Repositories:** round-trip each aggregate including child collections and `json` VOs; optimistic-concurrency conflict is detected; `CountUnreadForUser`, `GetByProviderMessageId`, `GetDueScheduled`, `GetOpen` digest queries work.
- **Migrations:** the schema migration applies cleanly to an empty database; all tables land in schema/namespace `notification`.
- **Outbox:** ingesting an event writes both the `Notification` rows and the `NotificationDispatched` outbox message in one transaction; rolling back leaves neither.
- **Inbox / idempotency:** delivering the same `ApplicationStatusChangedIntegrationEvent` twice creates exactly one notification.
- **Digest end-to-end:** three digest-eligible events for one user land in one `Open` digest; the digest scheduler at the window dispatches a single consolidated message and transitions all three notifications.
- **DND:** a `Normal` notification ingested during the user's DND window is `Queued` with `scheduled_for_utc` set; the DND-release worker sends it after the window.
- **Compliance:** an SMS STOP webhook opts the user out and writes a `ConsentRecord`; a subsequent non-critical SMS event produces no SMS notification but a critical security SMS still does.
- **API:** host-level tests for: open notification center → mark read → unread count drops; set email preferences then ingest a now-disabled event → no notification; one-click `/unsubscribe?token=` flips `global_email_opt_out`.
- **Public API:** `NotificationPublicApi.GetUnreadCount` and `GetDeliveryLog` return correct data.

### 13.4 Acceptance-criteria coverage

Every AC in §14.2 must map to at least one test. Treat the §14.2 table as the definition-of-done checklist.

---

## 14. Worked example & acceptance criteria

### 14.1 Worked vertical slice — "An application status change reaches the job seeker"

End-to-end, to pattern-match every other inbound-event flow against:

1. **Inbound event.** BC-5 Job Application publishes `ApplicationStatusChangedIntegrationEvent { EventId, ApplicationId, JobSeekerUserId, FromStatus: "Submitted", ToStatus: "Shortlisted", By, At }`. The module's integration-event subscription (wired in the module composition entry point) receives it and issues `IngestIntegrationEventCommand` through the mediator.
2. **Inbox dedupe.** `IngestIntegrationEventCommandHandler` checks `inbox_messages` for `EventId`. Not present → proceed (and the `EventId` row is written in the same transaction at step 7).
3. **Load preferences.** `RecipientPreferencesRepository.GetByUserId(JobSeekerUserId)` → the `RecipientPreferences` aggregate. (If somehow missing — event arrived before `UserRegistered` — create a default and continue.)
4. **Plan fan-out.** `ChannelFanoutPlanner.Plan(envelope, prefs)` maps `ApplicationStatusChanged` → `NotificationType.ApplicationUpdate`, `Priority.Normal` (would be `High` if `ToStatus == "Hired"`). It calls `prefs.CanReceive` for `Email`, `InApp`, `Sms`:
   - `Email`: cell enabled, `Frequency = Immediate`, not in DND → `Allowed`.
   - `InApp`: cell enabled, `ToastMode = Toast` → `Allowed`.
   - `Sms`: type is not critical → `Denied` (`Reason = "sms-non-critical"`).
   Result: two `PlannedNotification`s.
5. **Create & route.** For each planned notification: `Notification.Create(...)`. The email one — `Frequency == Immediate`, not held — resolves the `(Email, ApplicationUpdate)` template via `NotificationTemplateRepository`, calls `TemplateRenderer.Render` with the event payload, and is marked for immediate send. The in-app one — `Render` inline, marked for immediate send + toast.
6. **Persist.** `NotificationRepository.Add` for both; `inbox_messages` row added; `UnitOfWork.SaveChanges()` — all in one transaction.
7. **Outbox + send.** The outbox relay picks up the queued sends: calls `EmailChannel.Send` and `RealtimePush.PushToast`. On success each notification gets `RecordSendAttempt(...)`, status → `Sent`, and a `NotificationDispatchedIntegrationEvent` is written to `outbox_messages`.
8. **Relay.** The relay publishes `NotificationDispatched`; BC-10 Reporting receives it for its delivery dashboards.
9. **Webhook later.** The email provider POSTs a delivery webhook → `RecordDeliveryStatusCommand` → `Notification.MarkDelivered()` → `NotificationDeliveredIntegrationEvent` to the outbox.

### 14.2 Acceptance criteria — definition of done

| Story | Key ACs the module must satisfy |
|---|---|
| US-3.6.1-01 Receive event email | Email delivered within the 5-min SLA on a triggering event; body carries subject + details + action link; the four event types (job match, app status, message, profile view) each produce a notification when enabled. |
| US-3.6.1-02 Email preferences | Independent per-type toggles; `Immediate`/`DailyDigest`/`WeeklyDigest` frequency; preferred-email selection; preferences apply immediately; global opt-out kills all email except `Transactional`/`AccountSecurity`. |
| US-3.6.1-03 Email templates | Create template (subject/HTML/footer); `{{placeholder}}` substitution at send time; preview with sample data; version control with rollback; one template per event type; 12-month version retention; ≤100 KB / ≤50 000 chars enforced. |
| US-3.6.1-04 Immediate & digest | `Immediate` sends within 5 min; `DailyDigest`/`WeeklyDigest` batch into one consolidated message at the window; digest contains all batched items; **empty digest is not sent**; a frequency change applies to the next event. |
| US-3.6.1-05 Log all email | Every send logged (user, recipient, template, type, UTC timestamp, status `Pending`); status updated from webhooks (delivered/bounced/opened/clicked); retries logged; hard bounce records type + flags the address; log queryable by user/date/email; 3-year retention then archive. |
| US-3.6.1-06 Spam compliance | One-click unsubscribe link in the footer; unsubscribe honoured immediately (except transactional); sender name + postal address + support contact in every email; correct List-Unsubscribe header; complaint → auto-unsubscribe + log; 3 hard bounces → address marked invalid. |
| US-3.6.2-01 In-app center | Bell-icon center lists recent notifications with type/title/description/timestamp/read-state; unread-count badge; load-more past 20; read state persisted across sessions. |
| US-3.6.2-02 Real-time in-app | Toast on a triggering event when the user is active; clicking navigates to the relevant page; toast also written to the center; bursts < 10s batched into one toast. |
| US-3.6.2-03 Weekly recs in-app | On `RecommendationGenerated`, a weekly in-app notification with a count + top-job preview + "View all"; clicking a job opens its detail; disable in preferences stops it; not sent if < 3 matches. |
| US-3.6.2-04 Notification history | 90-day history newest-first; filter by type; keyword search on subject/body/related names; read state persisted; detail view; > 90 days archived, not deleted. |
| US-3.6.2-05 In-app preferences | Per-type in-app toggles independent of email; `Toast`/`CenterOnly`/`Disabled` per type; sound/badge flags; DND window holds non-critical toasts (still logged to center); preferences apply immediately. |
| US-3.6.2-06 Types & indicators | Every notification carries a `NotificationType`; icon + colour + text label per type; `High` priority shows an urgent visual cue; consistent, WCAG-AA, colour-blind-safe (icon + text, not colour alone). |
| US-3.6.2-07 Manage notifications | Mark-as-read (single + all); soft-delete to archive (single + batch ≤ 50); take-action button routes to the relevant page; undo-delete within 10s restores. |
| US-3.6.3-01 SMS for critical updates | SMS sent for registration/MFA/password-reset codes (code supplied by BC-1) and security alerts; ≤ 160-char format with purpose + code/link + expiry + support; sent via the SMS provider port; 30-second SLA. |
| US-3.6.3-02 SMS opt-in | Opt-in at registration and in settings; E.164 validation + a verification SMS to confirm number ownership; confirmation enables SMS; opt-out stops SMS except critical security alerts; number change re-verified. |
| US-3.6.3-03 Limit SMS frequency | SMS only for critical types (verification, reset, security, emergency) — never marketing/recs; bursts de-duped into one SMS; ≤ 5 SMS/user/24h cap with over-cap messages queued not dropped; DND respected; critical security bypasses caps + DND. |
| US-3.6.3-04 Track SMS delivery | Every SMS logged (user, masked phone, redacted content, template, UTC timestamp, status `Pending`); status updated from provider webhooks; soft bounce retried (1h/6h/24h, max 3); hard bounce marks the number invalid + disables SMS for the user; log queryable; 3-year retention then archive. |
| US-3.6.3-05 SMS compliance | TCPA quiet hours (08:00–21:00 recipient timezone) enforced; explicit opt-in with a logged `ConsentRecord` (timestamp + IP + method); STOP keyword → immediate opt-out + confirmation; sender identity + reason + support in every SMS; DNC-registry check before sending; 3-year consent audit trail. |

---

## Appendix — teaching notes & open questions

- **Conformist by design.** BC-9 never negotiates an event shape. If BC-5 renames a field in `ApplicationStatusChanged`, BC-9 adapts — it does not get a vote. Contrast this with the BC-3↔BC-7 Partnership where both sides must change together. Ask the class: *what does a Conformist give up, and what does it gain?* (Gives up: influence over upstream contracts. Gains: it can subscribe to 20+ event types cheaply without 20 negotiations.)
- **The OTP seam.** BC-1 owns OTP *generation and validation*; BC-9 owns OTP *delivery*. This package introduces `OtpRequestedIntegrationEvent` as the explicit handoff. A tempting alternative is for BC-1 to call an SMS port directly — but then SMS compliance, frequency caps, logging, and DND would be duplicated in BC-1. Discuss: where should a cross-cutting capability like "send an SMS" live, and what is the cost of the event indirection (BC-1 can't get a synchronous "SMS delivered" confirmation)?
- **One event, many notifications.** A single `ApplicationSubmitted` can become up to six `Notification` aggregates (seeker + employer, × up to 3 channels). Modelling each (recipient, channel) pair as its own aggregate keeps delivery tracking and retries clean — each can succeed or fail independently. Ask: *would a single "Notification with three channel children" aggregate be better or worse?*
- **Preferences as borrowed state.** `RecipientPreferences` holds `UserId`, role, email, phone — all *originally* owned by BC-1/BC-2/BC-3. BC-9 keeps a copy, seeded and updated by events. This is deliberate (a generic BC must be self-sufficient at routing time) but it is duplicated state. Discuss eventual-consistency staleness: what happens if a user changes their email in BC-1 and an event is in flight?
- **Digest vs. immediate is a per-cell choice.** Frequency lives on the `(channel, type)` preference cell, not globally — a user can have immediate application updates but a weekly recommendations digest. This is more flexible than the stories strictly require; flag it as a modelling choice, not a requirement.
- **Compliance is domain logic, not config.** Quiet hours, frequency caps, opt-out carve-outs, and consent records are all modelled inside the Domain layer (`DndScheduleCalculator`, `FrequencyCapEvaluator`, `RecipientPreferences.CanReceive`). A weaker design would bury these in the channel adapters. Discuss why pulling regulatory rules into the domain — where they are unit-testable — matters.
- **Localization.** Story assumptions mix US/EU/Canada/UK compliance frameworks and Bangladesh `+880` defaults. This package defaults `Timezone` to `Asia/Dhaka` and SMS region to `+880` (consistent with the rest of the platform) while keeping the compliance rules (TCPA quiet hours etc.) timezone-driven so they work for any recipient.
