# Deployment

The application is designed for one instance with a persistent SQLite database. Use a current patched .NET 10 runtime and HTTPS for production.

## Docker Compose with HTTPS

Requirements: a Linux server with Docker Engine/Compose, a hostname whose DNS points to that server, and incoming TCP ports 80/443 available for Caddy.

1. Copy `.env.example` to `.env` and replace `DOMAIN` with your actual hostname.
2. Run `docker compose up --build -d`.
3. Open `https://your-hostname`, register your own account and complete setup.

Caddy terminates HTTPS and forwards to the internal application port. The application trusts forwarded headers only from the configured fixed proxy address. If you change the Compose network, update `ReverseProxy__KnownProxy` to the proxy's new address.

The container runs as a non-root user. `gym-data` stores the database and `gym-keys` stores data protection keys. Keep both volumes across upgrades. Caddy's volumes preserve its certificate state. Do not run `docker compose down --volumes` when you want to retain data.

Demo accounts are disabled in Production. SQLite and key paths are configured with environment variables; no secret needs to be committed.

## Backup and restore

Stop the application before making a file-level SQLite backup, or use SQLite's online backup API. Preserve the data protection key volume separately. Encrypt and restrict access to backups, since they contain account and training data.

Restore to an isolated instance first, then run the same build's migrations. Take a fresh backup before upgrading the application or adding a migration.

## Without Docker

```sh
dotnet publish IronMindGym/IronMindGym.csproj --configuration Release --output artifacts/publish
```

Configure `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`, `DataProtection__KeyDirectory`, `AllowedHosts` and the trusted reverse proxy address. Run the published application as an unprivileged service behind an HTTPS proxy. The service account needs write access to the database and key directories, which must remain outside `wwwroot`.

Migrations run at startup. Use a single application instance during migration. This release does not automatically import old `EnsureCreated` databases.
