---
section_id: "3.1.1"
title: "Job Seeker Registration and Profile Management"
srs_page: 13
requirement_type: FR
subdomain: supporting
bounded_contexts: ["BC-2"]
tags:
  - requirement/functional
  - subdomain/supporting
  - bc/seeker-profile
  - topic/bilingual
  - topic/privacy
---
# 3.1.1. Job Seeker Registration and Profile Management

*(SRS page 13)*

## Functional Requirements

| ID | Requirement |
|---|---|
| FR-01 | The system SHALL provide a self-registration process for job seekers. |
| FR-02 | The system SHALL support account activation via the user's mobile number using a one-time password (OTP). |
| FR-03 | The system SHALL collect job seeker information through a 3-level onboarding process (mobile + email): **Level 1** (mandatory): first name, last name, email, mobile number, password, gender. **Level 2** (gated — required before first job application): education, work experience, skills, training, certificates, recent salary (optional), current address, permanent address/residence. **Level 3** (always optional): social media links (LinkedIn, IEEE, X, etc.), personal statement, bio. |
| FR-04 | The system SHALL support resume/CV upload in PDF, DOCX, and TXT formats. |
| FR-05 | The system SHALL parse uploaded resumes/CVs via a third-party resume-parsing API (e.g., Affinda, Sovren, or RChilli) and pre-populate the profile with the extracted fields, subject to user review and confirmation. |
| FR-06 | The system SHALL allow job seekers to build their profile via a staged, stepwise form with contextual popup prompts for: **Education** (degree, institution, start/end date), **Experience** (company, role, start/end date, responsibilities), **Skills** (primary/secondary; soft/hard; proficiency 1–5), **Preferences** (job type, desired salary range, preferred location, work arrangement). |
| FR-07 | The system SHALL allow job seekers to edit and update their profile at any time, with a full revision history retained for 12 months. |
| FR-08 | The system SHALL surface a profile-completeness indicator (0–100%) and contextual recommendations encouraging users to complete their profile and explore adjacent opportunities beyond their formal education/training. |
| FR-09 | The system SHALL allow job seekers to set job preferences including job type, industry, location, salary expectations, and work arrangements (on-site / hybrid / remote). |
| FR-10 | The system SHALL provide privacy controls that allow job seekers to set profile visibility (public / private / recruiters-only) and to request account deactivation. Deactivated accounts SHALL be soft-deleted and retained indefinitely for restoration and compliance purposes; PII SHALL be excluded from search and matching while deactivated. |
| FR-11 | The system SHALL generate a shareable public profile URL of the form `/p/{slug}-{hash}` (e.g., `/p/topu-newaj-a3f2`) and a corresponding QR code that encodes the full HTTPS URL, only when the user has explicitly activated public sharing. The user SHALL be able to regenerate the slug to revoke previously shared links. |
| FR-12 | The system SHALL allow the job seeker to upload supplementary documents (e.g., certificates, portfolios, references). |

## Acceptance Criteria

**AC-3.1.1-01 (Registration):** Given a new visitor, when they submit Level 1 fields with a unique email and mobile number, then a job seeker account is created in `pending_activation` state and an SMS OTP is dispatched within 10 seconds.

**AC-3.1.1-02 (Activation):** Given a `pending_activation` account, when the user submits the correct 6-digit OTP within 5 minutes, then the account transitions to `active`. After 3 failed OTP attempts the account is locked for 15 minutes; the user may request a new OTP after a 60-second cooldown.

**AC-3.1.1-03 (Password policy):** Passwords MUST be ≥8 characters and contain at least one uppercase letter, one lowercase letter, and one digit. MFA is optional per user. Password reset SHALL require an SMS OTP to the registered mobile number.

**AC-3.1.1-04 (Level gating):** A job seeker with Level 1 only complete CAN browse jobs but CANNOT submit a job application. Attempting to apply SHALL trigger an inline prompt to complete Level 2.

**AC-3.1.1-05 (CV upload):** Resume uploads are limited to PDF / DOCX / TXT, ≤5 MB per file. The file is virus-scanned on upload; infected files are rejected with error `E-UPLOAD-VIRUS`.

**AC-3.1.1-06 (CV parsing):** When a resume is uploaded, the system SHALL submit it to the configured third-party parser, receive structured JSON within 30 seconds, and present extracted fields to the user in a review screen for confirmation before merging into the profile. Parser failures SHALL fall back to manual entry without blocking the user.

**AC-3.1.1-07 (Supplementary docs):** Each supplementary document is ≤10 MB; up to 10 documents per profile. Accepted formats: PDF, PNG, JPG. Virus-scanned identically.

**AC-3.1.1-08 (Profile completeness):** Completeness % is computed as a weighted sum: L1 = 30%, L2 = 50%, L3 = 10%, resume = 10%. The score is recomputed on every profile update.

**AC-3.1.1-09 (Public profile / QR):** When the user toggles public sharing on, the system generates a slug `{firstname-lastname-4charhash}`, a QR PNG (≥512×512, ECC level M), and exposes the URL only over HTTPS. Toggling off invalidates the URL within 60 seconds.

**AC-3.1.1-10 (Deactivation):** Deactivation is reversible by login + SMS OTP within any timeframe; while deactivated, the profile is excluded from search, recommendations, and matching, and no notifications are sent.

**AC-3.1.1-11 (Locale):** The profile UI SHALL be available in English and Bangla. Free-text fields (bio, personal statement, responsibilities) SHALL be stored as-entered without translation; search SHALL index both Latin and Bengali scripts.

## Data Model (Logical)

```
JobSeeker
  id (UUID, PK)
  email (string, unique, indexed)
  mobile_e164 (string, unique, indexed)        # +880XXXXXXXXXX
  password_hash (string, Argon2id)
  status (enum: pending_activation | active | deactivated | locked)
  gender (enum: male | female | other | prefer_not_to_say)
  first_name, last_name (string)
  preferred_locale (enum: en | bn, default: en)
  profile_visibility (enum: public | private | recruiters_only, default: private)
  public_slug (string, nullable, unique)
  completeness_score (int 0..100)
  created_at, updated_at, deactivated_at (timestamp)

JobSeekerProfile (1:1)
  job_seeker_id (FK)
  current_address, permanent_address (jsonb: line1, line2, city, district, postcode, country)
  recent_salary_bdt (numeric, nullable)
  bio_text (text, nullable)
  personal_statement (text, nullable)
  social_links (jsonb: { linkedin, ieee, x, github, ... })

Education (N)        : id, job_seeker_id, degree, institution, start_date, end_date, gpa
Experience (N)       : id, job_seeker_id, company, role, start_date, end_date, is_current, responsibilities
Skill (N)            : id, job_seeker_id, name, category (hard|soft), tier (primary|secondary), proficiency 1..5
Certificate (N)      : id, job_seeker_id, name, issuer, issue_date, expiry_date, credential_id, document_id
Preference (1:1)     : job_seeker_id, job_types[], industries[], locations[], salary_min, salary_max, work_arrangement[]

Document (N)
  id, job_seeker_id, kind (resume | supplementary), filename, mime_type, size_bytes,
  storage_key, virus_scan_status, uploaded_at

OtpChallenge (N)
  id, job_seeker_id, channel (sms), code_hash, purpose (activation|password_reset|mfa),
  attempts, expires_at, consumed_at
```

## API Contract (REST, illustrative)

```
POST  /api/v1/seekers/register           → 201 { seeker_id, status: "pending_activation" }
POST  /api/v1/seekers/{id}/activate      body { otp } → 200 { status: "active", access_token }
POST  /api/v1/seekers/{id}/otp/resend    → 202
POST  /api/v1/seekers/login              body { identifier, password } → 200 { access_token }
POST  /api/v1/seekers/password/reset     → flow: request OTP → submit OTP + new password
GET   /api/v1/seekers/{id}/profile       → 200 { profile, completeness_score }
PATCH /api/v1/seekers/{id}/profile       → 200
POST  /api/v1/seekers/{id}/resume        multipart → 202 { document_id, parse_job_id }
GET   /api/v1/seekers/{id}/resume/parse/{job_id} → 200 { status, extracted: {...} }
POST  /api/v1/seekers/{id}/resume/parse/{job_id}/confirm body { selected_fields } → 200
POST  /api/v1/seekers/{id}/documents     multipart (kind=supplementary) → 201
DELETE /api/v1/seekers/{id}/documents/{doc_id}
PUT   /api/v1/seekers/{id}/visibility    body { visibility, public_share_enabled }
POST  /api/v1/seekers/{id}/public-slug/regenerate → 200 { slug, public_url, qr_png_url }
POST  /api/v1/seekers/{id}/deactivate    → 202
```

All endpoints require Bearer JWT except register / activate / login / password-reset. Rate limits: register 5/hr/IP, OTP send 3/hr/mobile, login 10/min/IP.

## Sequence Flows

**Registration → Activation**
1. Client → `POST /seekers/register` with L1 fields.
2. Server validates uniqueness, hashes password (Argon2id), persists `pending_activation` row, creates `OtpChallenge`.
3. Server → SMS gateway: 6-digit OTP, 5-min TTL.
4. Client → `POST /seekers/{id}/activate` with OTP.
5. Server verifies OTP, marks consumed, sets status `active`, issues JWT.

**Resume Upload → Parse → Merge**
1. Client uploads resume → object storage; server records `Document` row.
2. Server enqueues parse job; calls third-party parser API with signed URL.
3. Parser returns structured JSON; server stores raw + normalized output.
4. Client polls or receives webhook; user reviews extracted fields on a confirmation screen.
5. Client → `confirm` endpoint with selected fields; server merges into profile entities.

## Validation Rules

- Email: RFC 5322; mobile: E.164, must validate as Bangladesh (+880) by default; configurable for other regions.
- Names: 1–60 chars; allow Latin + Bengali letters, spaces, hyphens, apostrophes.
- Password: ≥8 chars; rejects top-10k breached passwords (HIBP-style list).
- Dates: `start_date ≤ end_date`; future `end_date` only when `is_current = false`.
- Salary: non-negative; stored in BDT; display converts on demand.
- Slug: `[a-z0-9-]{3,40}`; collision-checked; profanity-filtered.

## Edge Cases & Error Handling

- Duplicate email or mobile → `E-REG-DUPLICATE` (409). Suggest password reset.
- OTP expired / max attempts → `E-OTP-EXPIRED` / `E-OTP-LOCKED`.
- Resume parser timeout (>30s) or 5xx → degrade to manual entry; log incident; do not block user.
- File rejected by virus scan → quarantine, notify user, audit log.
- User changes mobile number → re-verification via OTP on the new number; old number remains until verification succeeds.
- Public-profile QR scanned after slug regeneration → generic 404 page; no PII leaked.
- Concurrent profile edits across devices → last-write-wins per field with a visible "edited just now on another device" banner.

## Test Scenarios

1. Happy-path register → OTP → activate → complete L2 → upload resume → confirm parsed fields → apply to a job.
2. Reject registration with already-registered mobile.
3. Three wrong OTPs → account locked → OTP unlock after 15 min.
4. Upload 6 MB PDF → rejected with size error; upload 4 MB PDF → accepted.
5. Parser returns empty JSON → user falls back to manual entry; no profile data lost.
6. Toggle visibility from public → private; previously shared QR resolves to 404.
7. Deactivate account → profile no longer appears in search or recommendations within 60 s; reactivate via login + OTP.
8. Bangla input in bio field round-trips through save/load without mojibake; search finds the seeker by Bangla skill term.

## Dependencies & Traceability

- Authentication & authorization: see [[3_1_5_Authentication_and_Authorization|3.1.5]].
- Privacy & retention: see [[5_2_3_Privacy_and_Compliance|5.2.3]].
- Multilingual: see [[5_4_3_Multilingual_Support|5.4.3]].
- Matching consumes profile fields per [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] and [[3_3_2_Job_Recommendation_Engine|3.3.2]].

## Related

- [[3_1_User_Management_and_Authentication|3.1. User Management and Authentication]]
- [[3_1_5_Authentication_and_Authorization|3.1.5. Authentication and Authorization]]
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1. A Vector-Based (Skills-Based and Behavior) Matching Algorithms]]
- [[3_3_2_Job_Recommendation_Engine|3.3.2. Job Recommendation Engine]]
- [[5_2_3_Privacy_and_Compliance|5.2.3. Privacy and Compliance]]
- [[5_4_3_Multilingual_Support|5.4.3. Multilingual Support]]
