---
section_id: "3.6.3"
title: "SMS Notifications"
srs_page: 22
requirement_type: FR
subdomain: generic
bounded_contexts: ["BC-10"]
tags:
  - requirement/functional
  - subdomain/generic
  - bc/notifications
  - topic/notifications
---
# 3.6.3. SMS Notifications

*(SRS page 22)*

3.6.3. SMS Notifications
ID Requirement
FR-156. The system SHALL provide SMS notifications for critical updates and time-
sensitive information (byt the integration with SMS service providers
(gateway)). Such as welcoming SMS with a code for user registration,
employer registration, and SMS for resetting the password.
FR-157. The system SHALL allow users to opt-in to SMS notifications and provide their
mobile number.
FR-158. The system SHALL limit SMS notifications to essential communications to
avoid overwhelming users.
FR-159. The system SHALL track SMS delivery status for monitoring and
troubleshooting. + loging as sms messages
FR-160. The system SHALL comply with telecommunications regulations regarding
SMS messaging.

## Related

- [[3_1_5_Authentication_and_Authorization|3.1.5. Authentication and Authorization]]
- [[3_6_Notification_System|3.6. Notification System]]
- [[3_6_1_Email_Notifications|3.6.1. Email Notifications]]
