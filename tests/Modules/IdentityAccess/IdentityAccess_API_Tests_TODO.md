# IdentityAccessModule Integration Tests - TODO

This document tracks the pending integration tests required to fully cover the `IdentityAccessModule` endpoints. 

### 1. Anonymous / Public Endpoints
- [x] `POST /api/identity/login` (Completed)
- [ ] `POST /api/identity/activate`
- [ ] `POST /api/identity/activate/resend`
- [ ] `POST /api/identity/token/refresh`
- [ ] `POST /api/identity/token/revoke`
- [ ] `POST /api/identity/password/reset-request`
- [ ] `POST /api/identity/password/reset-verify`
- [ ] `POST /api/identity/password/reset`
- [ ] `POST /api/identity/oauth/token`

### 2. Authenticated User Endpoints
- [ ] `GET /api/identity/me`
- [ ] `GET /api/identity/me/sessions`
- [ ] `GET /api/identity/me/mfa`
- [ ] `POST /api/identity/mfa/enroll`
- [ ] `POST /api/identity/mfa/enroll/confirm`
- [ ] `DELETE /api/identity/mfa`
- [ ] `POST /api/identity/mfa/verify`
- [ ] `POST /api/identity/logout`
- [ ] `POST /api/identity/logout-all`
- [ ] `POST /api/identity/password/change`

### 3. Administrator Endpoints
- [ ] `GET /api/identity/admin/users`
- [ ] `GET /api/identity/admin/users/{id}`
- [ ] `POST /api/identity/admin/users/{id}/approve`
- [ ] `POST /api/identity/admin/users/{id}/reject`
- [ ] `POST /api/identity/admin/users/{id}/suspend`
- [ ] `POST /api/identity/admin/users/{id}/reinstate`
- [ ] `POST /api/identity/admin/users/{id}/deactivate`
- [ ] `POST /api/identity/admin/users/{id}/unlock`
- [ ] `POST /api/identity/admin/users/{id}/password-reset`
- [ ] `POST /api/identity/admin/users/{id}/role`
- [ ] `GET /api/identity/admin/audit`
