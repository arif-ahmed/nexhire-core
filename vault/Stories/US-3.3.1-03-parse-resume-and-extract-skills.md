---
story_id: "US-3.3.1-03"
title: "Parse Resume and Extract Structured Information"
section_id: "3.3.1"
related_requirements: ["FR-79", "FR-80", "FR-81", "FR-82", "FR-83"]
related_stories: ["US-3.3.1-04-review-and-correct-parsed-resume"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
  - bc/skill-taxonomy
  - topic/ai
---

# US-3.3.1-03 — Parse Resume and Extract Structured Information

## Story
As a **System**, I want to **use AI-powered resume parsing to extract personal details, education, work experience, skills, certifications, and achievements from uploaded documents**, so that **I can populate structured job seeker profiles and enable accurate skill-based matching**.

## Acceptance Criteria

**AC-01 — Resume upload and parsing**
- Given a job seeker uploads a resume (PDF, DOCX, or plain text)
- When the system processes it
- Then it extracts: personal details (name, email, phone), education history (degree, institution, graduation year), work experience (title, company, duration, description), skills, certifications, and achievements

**AC-02 — Multi-language support**
- Given a resume is in Arabic or English
- When the system parses it
- Then it successfully extracts all fields with primary focus on Arabic and English; language is detected automatically

**AC-03 — Skill standardization**
- Given raw skills are extracted from resume text
- When the system processes them
- Then skills are mapped to a canonical skill taxonomy to enable matching across different skill names (e.g., "JS" → "JavaScript")

**AC-04 — Confidence scoring and flagging**
- Given parsing completes
- When results are returned
- Then each extracted field includes a confidence score (0–100); fields below 70% are highlighted as needing verification

**AC-05 — Extraction accuracy**
- Given a well-formatted resume
- When parsing completes
- Then at least 90% of clearly stated fields are extracted correctly

## Assumptions
- **Resume parser**: Commercial or fine-tuned transformer model (e.g., LayoutLM for document understanding); supports PDF extraction via OCR for scanned resumes.
- **Supported formats**: PDF, DOCX, TXT; binary handling via pdf2image + Tesseract for scanned PDFs.
- **Skill mapping**: Extracted skills matched against skill-taxonomy BC (canonical list); ambiguous terms flagged for manual curation.
- **Confidence threshold**: 70% used to identify fields for manual review; stored with all extractions.
- **Processing**: Synchronous for UX; async batch re-parsing for bulk updates.
- **Cold-start**: Job seekers without prior resumes use manual profile entry or simplified extraction from form fields.

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-79, FR-80, FR-81, FR-82, FR-83

## Related Stories
- [[US-3.3.1-04-review-and-correct-parsed-resume|US-3.3.1-04]]
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
