# Code review

Review date: 2026-11-17.

| Priority | Original problem | Resolution |
| --- | --- | --- |
| High | Unsalted SHA-256 password hashing | ASP.NET Core salted adaptive password hasher; new password length limits |
| High | Several POST actions lacked CSRF validation | Global automatic validation and tokens for form/AJAX requests |
| High | Search and direct friend URLs exposed data without an accepted relationship | A single relationship service gates friend reads and writes; requests need recipient acceptance |
| High | Authentication state duplicated in session and cookies | Claims-based identity with cookie validation and revocable security stamps |
| High | Committed database contained account and training data | Database artifacts excluded from the repository and removed from rewritten history; original backup retained separately |
| High | Known vulnerable OpenAPI and SQLite dependencies | Unused OpenAPI removed, EF/SQLite dependencies updated, full dependency audit enforced |
| Medium | Seeding modified named users and repeatedly deleted avatar rows | Idempotent reference seed; personal data untouched; opt-in development demo seed |
| Medium | Ad-hoc SQL table creation and EnsureCreated schema drift | Tracked EF Core migrations |
| Medium | Workout JSON skipped nested validation; cardio fields were discarded | Server-side shape, size, foreign-key and numeric validation; typed strength/cardio/timed persistence |
| Medium | Errors could leave a saved workout while reporting failure | Workout and achievement persistence share a transaction |
| Medium | Exception messages leaked database implementation details | Expected validation errors are explicit; unhandled errors use the production error page |
| Medium | Mixed-controller helpers and duplicated progress JavaScript | Shared logic lives in services and focused static JavaScript files |
| Medium | Inaccurate exercise categories, duplicate names and false cardio classification | Canonical exercise catalog with unique names and three tracking modes |
| Medium | Nutrition used an undocumented averaged sex coefficient and inconsistent macro energy | Explicit optional adult equation, personal overrides and a consistent macro budget |
| Medium | Research feed contained mismatched citations | Four manually verified primary sources with limited, attributed summaries |
| Low | Progress editing used the progress-row ID instead of the exercise ID | Correct exercise key passed to editor |
| Low | Historical PR comparisons included later sessions | Details compare only earlier sessions |
| Low | Weight-volume values were labelled as tonnes | Consistent kg × reps labels |

## Validation

Integration tests run against temporary SQLite files and the real MVC pipeline. They cover anonymous access, anti-forgery checks, registration, case-insensitive uniqueness, password revocation, rate limits, setup, mixed workout types, malformed drafts, ownership, record recalculation, friend acceptance/removal, nutrition, export privacy, goals, seeding and page rendering.

Browser validation covers the dashboard, responsive navigation and the workout workflow. Container files are supplied for deployment; local container execution requires a running Docker engine.

## Deliberate limits

- One application instance and SQLite. Multi-instance deployment would require shared rate limiting, shared keys and a database strategy.
- Daily journal boundaries are UTC; per-user time zones are not implemented.
- Password changes are supported; email recovery and multifactor authentication are not implemented.
- A draft is local to its browser tab. Saving a workout requires the server.
- Earned achievements remain earned after entries are deleted; personal records are recalculated.
- Old course databases need a separate import plan. This release intentionally starts with a clean migration baseline.
