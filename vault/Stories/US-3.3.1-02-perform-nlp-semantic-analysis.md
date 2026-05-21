---
story_id: "US-3.3.1-02"
title: "Perform NLP Semantic Analysis on Job Descriptions"
section_id: "3.3.1"
related_requirements: ["FR-73", "FR-74"]
related_stories: ["US-3.3.1-01", "US-3.3.1-03"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
  - bc/skill-taxonomy
  - topic/ai
---

# US-3.3.1-02 — Perform NLP Semantic Analysis on Job Descriptions

## Story
As a **System**, I want to **extract semantic meaning from job descriptions and resumes using NLP, beyond simple keyword matching**, so that **I can accurately match candidates whose experience or skills are expressed differently but address the same underlying job requirements**.

## Acceptance Criteria

**AC-01 — Job description parsing**
- Given a job posting is submitted
- When the system processes the description
- Then it extracts: required skills (with proficiency levels inferred), experience levels (junior/mid/senior), job categories, and key responsibilities as structured attributes

**AC-02 — Semantic understanding**
- Given two job descriptions with equivalent but differently worded requirements (e.g., "JavaScript expertise" vs. "JS development experience")
- When the system analyzes both
- Then it recognizes semantic equivalence and treats them as matching concepts for candidate matching

**AC-03 — Confidence and flagging**
- Given NLP extraction completes
- When results are stored
- Then each extracted attribute includes a confidence score; low-confidence items are flagged for manual review

**AC-04 — Multi-language support**
- Given a job description in Arabic or English
- When the system processes it
- Then it successfully extracts attributes in the language provided with primary focus on Arabic and English

## Assumptions
- **NLP model**: Transformer-based (e.g., BERT/mBERT for Arabic-English support); fine-tuned on domain job descriptions.
- **Extraction taxonomy**: Pre-defined skill ontology and job category mapping (stored in skill-taxonomy BC); extraction mapped to canonical terms.
- **Confidence thresholds**: Attributes below 70% confidence automatically flagged; manual review workflow triggers for batch processing.
- **Processing**: Extraction runs synchronously on job publication; results cached in vector DB for matching pipeline.
- **Semantic matching threshold**: Cosine similarity > 0.75 considered semantically equivalent.

## Source Requirements
- [[3_3_1_A_Vector_Based_Skills_Based_and_Behavior_Matching_Algorithms|3.3.1]] — FR-73, FR-74

## Related Stories
- [[US-3.3.1-01-implement-ai-driven-matching-algorithm|US-3.3.1-01]]
- [[US-3.3.1-03-parse-resume-and-extract-skills|US-3.3.1-03]]
