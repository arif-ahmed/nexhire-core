---
story_id: "US-3.3.3-04"
title: "Respect Candidate Privacy in Recommendations"
section_id: "3.3.3"
related_requirements: ["FR-94"]
related_stories: ["US-3.3.3-01"]
role: "System"
status: draft
priority: must
tags:
  - story
  - bc/matching
---

# US-3.3.3-04 — Respect Candidate Privacy in Recommendations

## Story
As a **System**, I want to **enforce candidate privacy settings when generating employer recommendations, ensuring hidden profiles are not exposed to recruiters except when candidates have explicitly applied**, so that **candidates maintain control over their visibility and trust the platform**.

## Acceptance Criteria

**AC-01 — Privacy level enforcement**
- Given a candidate has set privacy_level = "hidden"
- When the recommendation system generates candidate lists for employers
- Then this candidate is excluded from all recommendations unless they have actively applied to the employer's posting

**AC-02 — Applied candidates override**
- Given a hidden-profile candidate explicitly applies to a job posting
- When an employer views candidates for that posting
- Then the candidate appears in recommendations and their profile is visible (application implies consent to visibility)

**AC-03 — Apply-only privacy level**
- Given a candidate has set privacy_level = "apply-only"
- When employers search the candidate database or view recommendations
- Then the candidate is not shown in search results or general recommendations; they are shown only in job posting-specific candidate lists if they applied

**AC-04 — Public privacy level**
- Given a candidate has set privacy_level = "public"
- When employers view recommendations or search candidates
- Then the candidate is visible and their full profile (as configured) can be viewed

**AC-05 — Privacy setting change**
- Given a candidate changes their privacy level from "hidden" to "public"
- When the change is saved
- Then the system updates privacy enforcement immediately; the candidate becomes eligible for new recommendations on next batch run

**AC-06 — Privacy audit logging**
- Given a recommendation is shown to an employer
- When it includes a candidate
- Then the system logs: candidate ID, employer ID, timestamp, whether candidate is public/applied; logs retained for privacy compliance audits

## Assumptions
- **Privacy levels**: Three tiers stored in job_seeker profile: public (visible in all recommendations/searches), apply-only (visible only if candidate applied), hidden (visible only if candidate applied).
- **Default**: New job seekers default to "apply-only" to provide privacy by default.
- **Recommendation query**: Recommendations filtered by privacy check at query time; candidates with hidden level and no active application are removed from results before sending to employer.
- **Search fallback**: Advanced candidate search (US-3.3.3-03) also respects privacy settings; hidden profiles not indexed for general search.
- **Logging**: privacy_log table tracks all recommendation exposures; queryable for candidate data subject access requests.
- **Batch updates**: If privacy setting changes, affected candidate is re-evaluated in next nightly recommendation batch; interim cache respects prior setting.

## Source Requirements
- [[3_3_3_Candidate_Recommendation_for_Employers|3.3.3]] — FR-94

## Related Stories
- [[US-3.3.3-01-see-ranked-candidate-recommendations|US-3.3.3-01]]
- [[3_1_1_Job_Seeker_Registration_and_Profile_Management|3.1.1]] (for privacy setting management)
