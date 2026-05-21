---
story_id: "US-3.4.1-04"
title: "Configure External Portal Credentials"
section_id: "3.4.1"
related_requirements: ["FR-97", "FR-102"]
related_stories: ["US-3.4.1-01", "US-3.4.1-02"]
role: "MoL Administrator"
status: draft
priority: must
tags:
  - story
  - bc/partner-integration
---

# US-3.4.1-04 — Configure External Portal Credentials

## Story
As a **MoL Administrator**, I want to **securely register and manage API credentials for external job portals**, so that **the system can authenticate and authorize integration with approved partner portals**.

## Acceptance Criteria

**AC-01 — Add external portal connection**
- Given the MoL Admin navigates to Integration Settings
- When they enter portal name, API endpoint, and API credentials (key/secret)
- Then the system validates the connection, stores credentials securely (encrypted), and marks the portal as "connected"

**AC-02 — Validate connection**
- Given credentials have been entered
- When the admin clicks "Test Connection"
- Then the system makes a test request to the external portal's health check endpoint and reports success/failure

**AC-03 — Configure sync options**
- Given a portal connection is established
- When the admin configures sync settings
- Then they can set: pull interval (hourly/daily/weekly), push on publish (yes/no), data mapping profile (if multiple profiles exist)

**AC-04 — Secure credential storage**
- Given credentials are entered
- When the system stores them
- Then the credentials are encrypted at rest using AES-256 and never logged in plaintext

**AC-05 — Rotate credentials**
- Given an admin needs to update API keys (e.g., due to key rotation policy)
- When they update the credentials in the portal configuration
- Then the system validates the new credentials and updates the stored (encrypted) values

## Assumptions
- Credentials are API key/secret pairs (OAuth tokens also supported per FR-113)
- Portal endpoints are registered by MoL/PEF; not user-discoverable
- Encryption key is managed by system infrastructure (e.g., HSM, AWS KMS)
- "Test Connection" endpoint is available on all supported portals
- Admin role has permission to manage portal integrations (RBAC enforced)

## Source Requirements
- [[3_4_1_External_Job_Site_Integration|3.4.1]] — FR-97, FR-102

## Related Stories
- [[US-3.4.1-01-ingest-jobs-from-external-portal|US-3.4.1-01 — Ingest Jobs from External Portal]]
- [[US-3.4.1-02-export-jobs-to-external-portal|US-3.4.1-02 — Export Jobs to External Portal]]
