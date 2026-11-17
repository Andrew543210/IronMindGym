# Security

Please report security problems privately through the repository's GitHub private vulnerability reporting feature when enabled. Do not post passwords, cookies, database files or personal training data in public issues.

## Supported version

The current `master` branch is maintained. Use a supported, patched .NET 10 SDK/runtime and rebuild container images when runtime updates are available.

## Legacy database exposure

An older public revision included a database with unsalted password hashes. Those database files have been removed from the rewritten branch history, but existing clones or cached copies cannot be recalled. Treat passwords from that database as exposed and change them anywhere they were reused. The current application starts with a new database and does not import those hashes.

## Deployment model

- HTTPS terminates at a trusted reverse proxy; production authentication cookies require HTTPS.
- Only the configured proxy may supply forwarded headers. Do not trust arbitrary client proxies.
- SQLite and data protection keys live outside the published web directory and must be backed up. Data protection keys can decrypt authentication cookies, so treat them as secrets.
- Demo accounts are opt-in and development-only. Production creates no default users.
- Workouts, nutrition, goals and progress are private to their owner. Accepted friends can see shared training data; searching alone grants no access. Removing a friendship revokes that access.
- Registration and login share an IP-based rate limit. It is a single-instance safeguard, not distributed abuse protection.
- Changing a password revokes existing sessions. Password reset by email and multifactor authentication are not provided.
- A clean dependency audit is a point-in-time result, not a guarantee of absence of vulnerabilities.
