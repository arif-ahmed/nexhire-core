---
story_id: "US-3.3.2-02"
title: "Compute Recommendation Embeddings Nightly"
section_id: "3.3.2"
related_requirements: ["FR-85", "FR-87", "FR-88"]
related_stories: ["US-3.3.2-01"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.2-02 — Compute Recommendation Embeddings Nightly

## Story
As a **System**, I want to **compute and cache recommendation embeddings nightly for all active job postings and job seeker profiles**, so that **real-time recommendation generation is fast and the collaborative filtering / content-based filtering models stay fresh**.

## Acceptance Criteria

**AC-01 — Nightly batch job**
- Given the nightly recommendation computation window (e.g., 2–3 AM UTC)
- When the batch job is triggered
- Then it processes all active job postings and all active job seekers, computing fresh embeddings

**AC-02 — Job embedding computation**
- Given all active job postings
- When the batch job runs
- Then it generates/updates embeddings for each posting based on current job description, skills, requirements, and historical application patterns

**AC-03 — Job seeker embedding computation**
- Given all active job seeker profiles
- When the batch job runs
- Then it generates/updates embeddings for each job seeker based on current profile (skills, experience, education), preferences, and historical interaction patterns

**AC-04 — Caching**
- Given embeddings are computed
- When storage is complete
- Then embeddings are cached in a vector database (or embedding cache layer) with metadata (version, computation timestamp, TTL)

**AC-05 — Monitoring and alerting**
- Given the nightly batch job executes
- When it completes or fails
- Then a report is generated showing: number of postings processed, number of job seekers processed, compute time, any errors; alerts triggered if compute time > 30 min or error rate > 5%

**AC-06 — Incremental updates**
- Given a job posting or job seeker profile is updated during the day
- When it is changed (profile edit, job description update, etc.)
- Then the change is queued for inclusion in the next nightly batch; interim recommendations use cached prior embedding

## Assumptions
- **Embedding dimension**: 768-dim vectors (e.g., sentence-BERT); stored in vector DB (Pinecone, Weaviate, or Milvus).
- **Model**: Transformer-based (BERT/MPNet) fine-tuned on job-market data; model versioning to support A/B testing of model updates.
- **Computation time**: Target <30 min for 100K job seekers + 50K postings on current hardware; parallelized by batch.
- **Staleness tolerance**: Cached embeddings used during day; maximum 24 hours old acceptable.
- **Incremental logic**: Modified entities queued in a "pending_recompute" table; next batch processes only updated + new entities (skip unchanged).
- **Failure handling**: If batch fails, prior embeddings retained; manual retry trigger available for admins; Slack alert sent to ML team.
- **Versioning**: Each embedding run tagged with version ID; old versions retained for 7 days for debugging.

## Source Requirements
- [[3_3_2_Job_Recommendation_Engine|3.3.2]] — FR-85, FR-87, FR-88

## Related Stories
- [[US-3.3.2-01-see-personalized-job-recommendations|US-3.3.2-01]]
