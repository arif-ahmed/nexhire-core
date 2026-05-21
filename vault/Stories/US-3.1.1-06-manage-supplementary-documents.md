---
story_id: "US-3.1.1-06"
title: "Manage supplementary documents"
section_id: "3.1.1"
related_requirements: ["FR-12"]
related_stories: ["US-3.1.1-04-upload-and-parse-resume"]
role: "Job Seeker"
status: draft
priority: should
tags:
  - story
  - bc/seeker-profile
---

# US-3.1.1-06 — Manage supplementary documents

## Story
As a **Job Seeker**, I want **to upload and manage supplementary documents (certificates, portfolios, references)**, so that **I can showcase additional qualifications and credentials to employers**.

## Acceptance Criteria

**AC-01 — Upload supplementary document**
- Given I am on the documents management page
- When I upload a PDF, PNG, or JPG file ≤ 10 MB
- Then the file is accepted and scanned for viruses

**AC-02 — Virus scan on supplementary upload**
- Given I upload a supplementary document
- When the virus scan completes
- Then infected files are rejected with error `E-UPLOAD-VIRUS` and quarantined

**AC-03 — Document limit**
- Given I have uploaded 10 supplementary documents
- When I attempt to upload an 11th document
- Then I see error `E-UPLOAD-LIMIT-EXCEEDED` with a prompt to delete an existing document

**AC-04 — Unsupported format rejection**
- Given I attempt to upload a file in unsupported format (e.g., XLS, DOCX, BMP)
- When I submit the upload
- Then I see error `E-UPLOAD-INVALID-FORMAT` listing supported formats (PDF, PNG, JPG)

**AC-05 — Oversized file rejection**
- Given I attempt to upload a file > 10 MB
- When I submit the upload
- Then I see error `E-UPLOAD-SIZE-EXCEEDED` with the 10 MB limit displayed

**AC-06 — Document list display**
- Given I have uploaded supplementary documents
- When I view the documents section
- Then I see a list of uploaded documents with filename, size, and upload date

**AC-07 — Delete document**
- Given I view a supplementary document in my list
- When I click delete
- Then I see a confirmation prompt; upon confirmation, the document is removed

**AC-08 — Document storage and retrieval**
- Given I have uploaded a supplementary document
- When a recruiter views my public profile or I download my documents
- Then the document is retrieved from secure storage and displayed/downloaded correctly

## Assumptions
- Supplementary documents include: certificates, portfolios, references, cover letters, etc.
- Supported formats: PDF, PNG, JPG (stricter than resume formats).
- Maximum 10 supplementary documents per profile; 10 MB per file.
- Files are scanned immediately upon upload before storage.
- Documents are stored in secure object storage (S3 or equivalent) with access control.
- Document list shows filename, size in MB, and upload date.

## Source Requirements
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] — FR-12

## Related Stories
- [[US-3.1.1-04-upload-and-parse-resume|US-3.1.1-04 Upload and parse resume]]
