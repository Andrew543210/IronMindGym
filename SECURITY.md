# Security

Please report security problems privately through the repository's GitHub private vulnerability reporting feature when enabled. Do not post passwords, cookies, database dumps or personal training data in public issues.

## Supported version

The current `master` branch is maintained. Use a supported, patched .NET 10 SDK/runtime and rebuild container images when runtime updates are available.

## Deployment model

- HTTPS terminates at a trusted reverse proxy; production authentication cookies require HTTPS.
- Only the configured proxy may supply forwarded headers. Do not trust arbitrary client proxies.
- PostgreSQL is reachable only inside the Compose network in production; its password comes from `.env`, which is not committed. Data protection keys live outside the published web directory and must be backed up. They can decrypt authentication cookies, so treat them as secrets.
- The session cookie is HttpOnly and `SameSite=Lax`. Every request that changes data must also carry the antiforgery token in the `X-CSRF-TOKEN` header.
- Demo accounts are opt-in and development-only. Production creates no default users.
- Workouts, nutrition, goals and progress are private to their owner. Accepted friends can see shared training data; searching alone grants no access. Removing a friendship revokes that access.
- Registration, login and password changes share an IP-based rate limit. It is a single-instance safeguard, not distributed abuse protection.
- Changing a password revokes existing sessions. Password reset by email and multifactor authentication are not provided.
- A clean dependency audit is a point-in-time result, not a guarantee of absence of vulnerabilities.
