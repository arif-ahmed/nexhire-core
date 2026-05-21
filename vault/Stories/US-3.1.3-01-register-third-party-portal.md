---
story_id: "US-3.1.3-01"
title: "Register third-party job portal"
section_id: "3.1.3"
related_requirements: ["FR-22", "FR-23"]
related_stories: ["US-3.1.3-02-push-jobs-via-api", "US-3.1.3-04-view-integration-logs"]
role: "Third-Party Portal"
status: draft
priority: should
tags:
  - story
  - bc/partner-integration
---

# US-3.1.3-01 — Register third-party job portal

## Story
As a **Third-Party Job Portal**, I want **to register and onboard with the platform and receive an API key**, so that **I can push jobs and integrate with the job matching system**.

## Acceptance Criteria

**AC-01 — Portal registration process**
- Given I am a new third-party portal
- When I submit the registration form (portal name, contact email, website, company info)
- Then my account is created in `pending_activation` state

**AC-02 — Registration activation**
- Given my account is pending activation
- When an admin verifies my details and approves registration
- Then my account transitions to `active` and I receive an API key via email

**AC-03 — API key generation**
- Given my portal account is activated
- When I receive or access my API key
- Then the key is a unique, long, randomly-generated token with 32+ character length

**AC-04 — API key management page**
- Given I am logged in as a portal admin
- When I navigate to API credentials
- Then I can view my API key and copy it; I can also regenerate or revoke it

**AC-05 — Optional IP whitelisting**
- Given I am on the API credentials page
- When I add one or more IP addresses to the whitelist
- Then API requests from non-whitelisted IPs are rejected with error `E-API-IP-FORBIDDEN`

**AC-06 — Optional usage limits**
- Given I configure usage limits
- When I set a rate limit (e.g., 100 requests per hour)
- Then requests exceeding this limit are rejected with error `E-API-RATE-LIMITED`

**AC-07 — Token expiration configuration**
- Given I am on the API credentials page
- When I set an optional expiration date for my API key
- Then the key is automatically revoked on that date; subsequent requests fail with `E-API-KEY-EXPIRED`

**AC-08 — API key regeneration**
- Given I have an active API key
- When I click "Regenerate Key"
- Then a new key is issued and the old key is revoked immediately

## Assumptions
- Registration requires admin approval before activation.
- API key is issued via secure email; no key is displayed in plaintext on the platform after initial generation.
- IP whitelisting is optional; if not configured, any IP can use the key.
- Rate limits are configurable per portal (default: unlimited unless specified).
- Token expiration is optional; if not set, key remains valid indefinitely.
- Regenerated keys immediately revoke old keys; brief transition window (~5 minutes) for old key to stop working.

## Source Requirements
- [[3_1_3_Third_Party_Job_Portals_Registration_and_Integration|3.1.3]] — FR-22, FR-23

## Related Stories
- [[US-3.1.3-02-push-jobs-via-api|US-3.1.3-02 Push jobs via API]]
- [[US-3.1.3-04-view-integration-logs|US-3.1.3-04 View integration logs and sync dashboard]]
