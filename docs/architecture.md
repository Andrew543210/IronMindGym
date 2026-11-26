# Architecture

IronMind is one ASP.NET Core application with a PostgreSQL database. The application serves a JSON API under `/api` and the built React client from `wwwroot`, so the browser talks to a single origin in development and in production.

```text
Browser ── React client (Vite build, served from wwwroot)
   │  fetch /api/… with the session cookie, X-CSRF-TOKEN and Accept-Language
   ▼
ASP.NET Core ── controllers ── services ── EF Core ── PostgreSQL 18
```

## Server

| Layer | Responsibility |
| --- | --- |
| `Api/` | Attribute-routed controllers. They read the current user, validate the request model and choose the response: data, `204`, `404` or a `ProblemDetails` body with field errors. |
| `Services/` | The operations of the journal: saving workouts and records, awards, progress, nutrition, profile, friendships, statistics. Every method takes the owner's `userId` and limits its queries to that owner. |
| `Data/` | `IronMindDbContext`, constraints and indexes, migrations and the advisory locks used for writes. |
| `Models/` | Entities with small domain rules, for example the weight change of a tracked exercise. |
| `Contracts/` | Request and response records of the API. |
| `Localization/` | Picks the English or Ukrainian text of reference data for the current request. |
| `Seed/` | Reference data (set types, splits with their training days, ranks, awards, research notes), the exercise catalog from `exercises.json` and the opt-in demo data. |

Services use `DbContext` directly. A repository layer would only repeat EF Core's own abstractions.

### Request pipeline

Forwarded headers (only from the configured proxy) → exception handler and HSTS in Production → request localization → security headers (`nosniff`, `DENY` framing, referrer policy) → static client files → routing → rate limiter → authentication → authorization → controllers. Unknown `/api` paths return `404`; every other unknown path returns `index.html`, so client routes survive a reload. Hashed bundles under `/assets` are cached for a year, while `index.html` is revalidated on every visit.

### Authentication and CSRF

- Sign-in issues an HttpOnly, `SameSite=Lax` cookie (Secure outside Development) that lasts 30 days with sliding expiration. Passwords are hashed with ASP.NET Core's salted `PasswordHasher` and need at least 10 characters.
- The cookie carries a security stamp. Each request compares it with the stored one, so changing the password ends every other session.
- Every API endpoint requires a signed-in user except the session endpoints. The client files and the `index.html` fallback are public. An unauthenticated API call gets `401` instead of a redirect.
- Every request that changes data must send the antiforgery token in `X-CSRF-TOKEN`. The client gets it from `GET /api/session/csrf` after signing in or out. A refused token returns `400` with the problem type `urn:ironmind:problem:csrf-token`; the client then fetches a new token and retries once. Any other `400` is a refused value and is never retried.
- Login, registration and password changes share a fixed-window limit of 10 requests per minute per IP address, which also stops guessing the current password from a stolen session.

### Errors

Services and controllers report a refusal by throwing `DomainValidationException` (answered `400` with validation problem details, the message listed under its field) or `DomainConflictException` (answered `409` with the message as the title). `DomainExceptionFilter` is the only place that turns them into responses, so a refused value and a failed model validation look the same to the client. Any other exception is a server error and reaches the exception handler.

### Data and concurrency

- Migrations run at startup, followed by the reference data. Each reference set is matched by its key: missing rows are added and their shown values refreshed, and rows that saved data may point at are never deleted.
- The schema uses `citext` for email and username, so uniqueness ignores letter case while the entered spelling is kept, and `jsonb` for the unfinished workout.
- Every moment is stored as UTC `timestamptz`. Calendar days (nutrition entries, weigh-ins, goal deadlines) are `DateOnly` values stored as `date` and sent as `yyyy-MM-dd`. `AthleteCalendar` turns a moment into the athlete's day, Monday-based week and streaks using the time zone from the profile.
- Bodyweight has one source, the weigh-in log: the weigh-in with the latest date is the current bodyweight for the rank, the nutrition estimates and the profile. The height is kept on the profile.
- Writes that check existing rows before inserting and recalculate records and awards take a transaction-scoped advisory lock per athlete (`pg_advisory_xact_lock`). A double tap or a retry after a lost response therefore cannot save twice.
- A saved workout remembers the id of the draft it came from. Repeating the request returns the first workout, and a late draft update for a saved workout is refused with `409`.
- Personal records are recalculated when a workout is deleted. Earned awards stay earned.

### Localization

The client sends `Accept-Language: en` or `uk`. The server switches only the wording: names of exercises, groups, ranks, awards and validation messages. Numbers and dates are always parsed in the invariant form. Reference tables store both names, and neither is a key the code matches on: split types, their training days and set types have a code, the bench press, squat and deadlift used for the rank are marked as key lifts, and the remaining rows are found by their unique English name.

## Client

| Folder | Responsibility |
| --- | --- |
| `src/pages/` | One module per section: dashboard, workouts, progress, nutrition, statistics, goals, friends, profile, sign-in. Each section is a separate bundle loaded on first visit. |
| `src/components/` | Layout with the sidebar and the mobile navigation, the session guard and shared UI pieces. |
| `src/i18n/` | English and Ukrainian dictionaries. The compiler checks that every English key has a Ukrainian text and the plural forms Ukrainian needs. |
| `api.ts`, `hooks.ts` | The fetch wrapper (CSRF token, language, `ProblemDetails` errors) and hooks for loading a page and running one change at a time. |
| `SessionProvider.tsx` | The signed-in user, sign-out and the reason a session ended. |
| `pages/workouts/draft.ts`, `serverDraft.ts` | The unfinished workout: kept in `localStorage`, shared between tabs and sent to the server 1.5 s after the last change, so another device can continue it. |

Messages after an action (for example "Workout saved") travel with the navigation and are shown once; a reload does not repeat them.

## Tests

- **API integration tests** (`IronMindGym.Tests/Api`) start the real application with `WebApplicationFactory` on a PostgreSQL 18 container. Each test gets its own database copied from a migrated and seeded template. They cover authentication, CSRF, rate limits, ownership and friend access, validation messages in both languages, records, awards, the draft lifecycle and SPA hosting.
- **Domain unit tests** (`IronMindGym.Tests/Domain`) cover calendar boundaries across time zones and daylight saving, nutrition estimates, strength coefficients, record rules and seeding.
- **Client tests** render pages with React Testing Library against a mocked API and cover forms, validation errors, navigation, language switching and the draft logic.

CI requires at least 80% line and branch coverage for both the API and the client. Migrations are excluded from the API measurement.

## Deliberate limits

- One application instance. The rate limiter is in memory, and data protection keys live on a volume. Several instances would need a shared limiter and shared keys.
- There is no password reset by email and no multifactor authentication.
- The client needs the server to save a workout; it is not an offline application.
