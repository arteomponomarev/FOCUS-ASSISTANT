# Authentication and authorization

The Windows Agent creates a separate sign-in session through the existing login,
refresh and logout operations. It reads the account identifier from `/api/v1/auth/me`
and uses an explicit, owned session UUID supplied by the operator. Credentials are
kept in Agent process memory. The Agent accepts HTTPS and local loopback HTTP origins
and uses a masked interactive password prompt. [Windows pairing guide](../agent/windows-setup.md)

Register with `email`, `password`, and `displayName`. Email is canonicalized to lower
case. Use a password of 12–128 characters. Registration returns the profile and 201.
Existing historical users without credentials require migration through an authorized
account-provisioning workflow. Registration will not adopt historical data by guessing
an email address.

Login returns `tokenType`, `accessToken`, `expiresIn`, and `refreshToken`. Pass
`Authorization: Bearer <accessToken>` over HTTPS. ASP.NET Core protects opaque tickets
using Data Protection. Password storage uses ASP.NET Core Identity PasswordHasher.
Server-side login rows contain IDs and expiry timestamps. Raw credentials are excluded.

Access tickets expire after 15 minutes. Use `POST /api/v1/auth/refresh` with
`{"refreshToken":"<refresh token>"}` to obtain a new pair. Refresh is single use.
Clients should serialize refresh attempts and replace both credentials together.
Refresh extends the login for seven days. Logout revokes the login family. Changing
the password revokes all logins. Expired login rows are removed on successful login.

For Windows/Unity clients, use OS-protected credential storage. A browser client can
hold credentials in memory or use an approved same-origin backend-for-frontend with
secure cookies. Do not persist refresh tokens in browser local storage. In local mode
without a configured key directory, tickets expire on process restart. Production must
use a private, persistent key ring.

Authentication routes are throttled to 20 requests/minute per client IP. Behind a
reverse proxy, configure its specific IP to restore scheme and client address.
Login failure is generic. Duplicate registration returns 409. Email verification,
password recovery, account lockout and MFA are deployment-hardening extensions.
