---
story_id: "US-3.1.1-07"
title: "Manage profile visibility and sharing"
section_id: "3.1.1"
related_requirements: ["FR-10", "FR-11"]
related_stories: ["US-3.1.1-08-edit-profile-history"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-07 — Manage profile visibility and sharing

## Story
As a **Job Seeker**, I want **to control who can see my profile and share a public profile link/QR code**, so that **I can selectively expose my profile to recruiters and control my privacy**.

## Acceptance Criteria

**AC-01 — Set profile visibility level**
- Given I am on the privacy settings page
- When I select a visibility level (public / private / recruiters-only)
- Then the setting is saved and applied immediately

**AC-02 — Private profile (default)**
- Given my profile is set to private
- When recruiters search for job seekers
- Then my profile does not appear in search results

**AC-03 — Recruiters-only visibility**
- Given my profile is set to recruiters-only
- When only users with recruiter role search
- Then my profile is returned in search results; other users cannot access it

**AC-04 — Public profile visibility**
- Given my profile is set to public
- When anyone accesses the platform or my public URL
- Then my profile is visible and searchable

**AC-05 — Generate public sharing URL and QR code**
- Given I toggle public sharing ON and my profile is complete
- When the toggle is activated
- Then the system generates slug `{firstname-lastname-4charhash}`, public URL `/p/{slug}`, and QR PNG (≥512×512, ECC M)

**AC-06 — Public sharing URL structure**
- Given public sharing is enabled
- When I view the sharing settings
- Then I see the public URL in format `/p/{slug}` (e.g., `/p/topu-newaj-a3f2`)

**AC-07 — QR code display and download**
- Given public sharing is enabled
- When I view sharing settings
- Then I can see the QR code image (≥512×512) and download it as PNG

**AC-08 — Regenerate slug to revoke old links**
- Given I have shared my public profile
- When I click "Regenerate Slug"
- Then a new slug is generated and old links are invalidated within 60 seconds

**AC-09 — Old public URL returns 404 after regeneration**
- Given I regenerate my public slug
- When someone accesses the old `/p/{old-slug}` URL
- Then they see a generic 404 page with no PII leakage

**AC-10 — Disable public sharing invalidates URL**
- Given I have public sharing enabled
- When I toggle public sharing OFF
- Then the URL is invalidated within 60 seconds and returns 404

**AC-11 — Deactivate account hides profile**
- Given I deactivate my account
- When deactivation is processed
- Then my profile is excluded from search and recommendations, no notifications are sent

**AC-12 — Reactivate account via login and OTP**
- Given my account is deactivated
- When I log in with credentials and verify via SMS OTP
- Then my account transitions back to active and profile visibility is restored

## Assumptions
- Default visibility is private (opt-in for sharing).
- Public URL slug uses format: `{firstname-lowercase}-{lastname-lowercase}-{4-char-hash}`.
- QR code is generated as PNG with error correction level M, size ≥ 512×512 pixels.
- Slug collision is checked; profanity is filtered.
- Public sharing URL requires HTTPS only.
- Profile deactivation is reversible by login + OTP at any time.
- Deactivated accounts remain in database (soft delete) for compliance and recovery.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-10, FR-11

## Related Stories
- [[US-3.1.1-08-edit-profile-history|US-3.1.1-08 View and restore profile edit history]]
