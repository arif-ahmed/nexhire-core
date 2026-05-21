---
story_id: "US-3.1.2-03"
title: "Manage employer profile"
section_id: "3.1.2"
related_requirements: ["FR-18", "FR-19", "FR-21"]
related_stories: ["US-3.1.2-02-verify-employer-account"]
role: "Employer Owner"
status: draft
priority: should
tags:
  - story
  - bc/employer-profile
---

# US-3.1.2-03 — Manage employer profile

## Story
As an **Employer Owner**, I want **to manage my company profile with logo, images, company information, and supplementary documents**, so that **I can showcase my company to job seekers and enhance employer branding**.

## Acceptance Criteria

**AC-01 — Upload company logo**
- Given I am on the profile management page
- When I upload a PNG or JPG logo (≤ 5 MB)
- Then the file is scanned and stored; the logo appears on my public profile

**AC-02 — Upload company images**
- Given I am on the profile management page
- When I upload company images (PNG, JPG ≤ 5 MB per file, up to 5 images)
- Then files are scanned, stored, and displayed in a gallery on my profile

**AC-03 — Unsupported image format**
- Given I attempt to upload an image in unsupported format (e.g., BMP, GIF)
- When I submit the upload
- Then I see error `E-UPLOAD-INVALID-FORMAT`

**AC-04 — Image virus scan**
- Given I upload images
- When virus scanning completes
- Then infected files are rejected with error `E-UPLOAD-VIRUS`

**AC-05 — Company profile display**
- Given I have completed my profile
- When a job seeker visits my company profile
- Then they see company name, logo, description, job openings, and company images

**AC-06 — Upload supplementary documents**
- Given I am on the documents section
- When I upload company registration, VAT certificate, or other docs (PDF, JPG, PNG ≤ 10 MB, up to 10 files)
- Then files are scanned and stored with my profile

**AC-07 — Document management**
- Given I have uploaded supplementary documents
- When I view the documents section
- Then I can see, download, or delete documents

**AC-08 — Edit company information**
- Given I am on the profile edit page
- When I update company name, description, website, industry, or address
- Then changes are saved and reflected on my public profile within 60 seconds

## Assumptions
- Logo and images use separate upload buckets (profile images vs. supplementary).
- Supported image formats: PNG, JPG/JPEG.
- Logo image size: ≤ 5 MB; company images: ≤ 5 MB each, max 5 images.
- Supplementary documents: ≤ 10 MB each, max 10 files.
- All uploads are virus-scanned before storage.
- Profile updates are reflected in search and recommendations within 1 minute.

## Source Requirements
- [[3_1_2_Employer_Registration_and_Profile_Management|3.1.2]] — FR-18, FR-19, FR-21

## Related Stories
- [[US-3.1.2-02-verify-employer-account|US-3.1.2-02 Verify employer account]]
