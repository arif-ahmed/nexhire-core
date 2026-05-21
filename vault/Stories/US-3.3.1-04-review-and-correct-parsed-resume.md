---
story_id: "US-3.3.1-04"
title: "Review and Correct Parsed Resume Information"
section_id: "3.3.1"
related_requirements: ["FR-84"]
related_stories: ["US-3.3.1-03"]
role: "Job Seeker"
status: draft
priority: must
tags:
  - story
  - bc/matching
  - bc/skill-taxonomy
---

# US-3.3.1-04 — Review and Correct Parsed Resume Information

## Story
As a **Job Seeker**, I want to **review and correct the information extracted from my resume**, so that **my profile accurately reflects my qualifications and improves my matching results**.

## Acceptance Criteria

**AC-01 — View extracted data**
- Given a resume has been parsed
- When the job seeker views their profile
- Then they see all extracted fields (personal, education, experience, skills, certifications) displayed clearly with confidence scores

**AC-02 — Edit capability**
- Given a job seeker is viewing parsed data
- When they click on any field
- Then they can edit, add, or delete entries; changes are saved and trigger re-computation of embeddings for matching

**AC-03 — Highlight low-confidence fields**
- Given extraction completes
- When the job seeker views their profile
- Then fields with confidence < 70% are visually highlighted (e.g., yellow banner) to draw attention

**AC-04 — Bulk correction workflow**
- Given low-confidence fields exist
- When a job seeker corrects them
- Then the system re-runs skill standardization and updates the candidate embedding for the next matching run

**AC-05 — Confirmation of accuracy**
- Given a job seeker has reviewed all fields
- When they mark their profile as "verified"
- Then the system records this attestation and reduces friction in future matching

## Assumptions
- **Confidence display**: Shown as percentage badge; tooltip explains what confidence means.
- **Edit interface**: Inline editing for text fields; multi-select for skills from canonical taxonomy; date pickers for education/experience dates.
- **Re-embedding**: Triggered on save; job seeker notified of match score updates within 24 hours.
- **Verification flag**: Optional attestation; does not block matching but may influence recruiter confidence weighting in future versions.
- **Bulk workflow**: Batch email to job seekers with low-confidence profiles; one-click "review now" link to correction interface.

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-84

## Related Stories
- [[US-3.3.1-03-parse-resume-and-extract-skills|US-3.3.1-03]]
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
