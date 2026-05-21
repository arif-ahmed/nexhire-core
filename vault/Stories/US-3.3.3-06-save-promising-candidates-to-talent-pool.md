---
story_id: "US-3.3.3-06"
title: "Save Promising Candidates to Talent Pool (Shortlist)"
section_id: "3.3.3"
related_requirements: ["FR-96"]
related_stories: ["US-3.3.3-01"]
role: "Employer Recruiter"
status: draft
priority: should
tags:
  - story
  - bc/matching
---

# US-3.3.3-06 — Save Promising Candidates to Talent Pool (Shortlist)

## Story
As an **Employer Recruiter**, I want to **save promising candidates to a talent pool (shortlist) for future opportunities**, so that **I can build a pipeline of qualified candidates and quickly fill future positions without re-searching**.

## Acceptance Criteria

**AC-01 — Shortlist candidate**
- Given a recruiter views a candidate or candidate list
- When they click "Add to Shortlist" or "Save for Later"
- Then the candidate is added to a talent pool and a confirmation message is shown

**AC-02 — Multiple talent pools**
- Given a recruiter may have different categories of candidates (e.g., "Python Developers", "Senior Engineers", "UX Designers")
- When they add a candidate
- Then they can select an existing talent pool or create a new one; candidate added to selected pool(s)

**AC-03 — Talent pool view**
- Given a recruiter wants to view saved candidates
- When they navigate to "Talent Pools" or "Shortlists"
- Then they see a list of created pools with candidate counts; they can click a pool to view all candidates in it

**AC-04 — Candidate management within pool**
- Given a recruiter has candidates in a talent pool
- When they view the pool
- Then they can: view candidate profiles, send messages to multiple candidates (bulk), remove candidates, tag candidates with notes, sort/filter candidates in the pool

**AC-05 — Pool metadata and notes**
- Given a recruiter manages talent pools
- When they create or edit a pool
- Then they can add: pool name, description (e.g., "Candidates for Q2 expansion"), and associated job category/skills; notes are added per-candidate

**AC-06 — Reusability across time**
- Given a talent pool has been created
- When months later the recruiter needs to fill a similar role
- Then they can view and revisit the talent pool; candidate availability status is refreshed on view

**AC-07 — Collaboration**
- Given multiple recruiters at the same employer work together
- When one recruiter adds a candidate to a shared talent pool
- Then other recruiters can view the pool and see candidate additions and notes (if access is granted)

## Assumptions
- **Storage**: talent_pool and talent_pool_candidate tables; each pool belongs to one employer; max 20 active pools per employer (soft limit).
- **Naming**: Pools named by recruiter (e.g., "Senior Python Devs Q2"); auto-creation of a "General / Saved" pool for each recruiter as default.
- **Candidate removal**: Soft delete (mark inactive in pool); candidates not deleted from system; can be re-added.
- **Bulk messaging**: If > 3 candidates selected, confirm before sending message (safety check).
- **Availability refresh**: job_search_status and available_start_date fetched on pool view (not cached); user sees current data.
- **Access control**: Pools owned by recruiter; sharing requires explicit permission (not auto-shared to all org recruiters); configurable at pool creation.
- **Retention**: Candidates remain in pool indefinitely until explicitly removed; recruitment team responsible for pruning stale candidates.

## Source Requirements
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3]] — FR-96

## Related Stories
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
