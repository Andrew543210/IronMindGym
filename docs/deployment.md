# Deployment

The application runs as one container with a PostgreSQL 18 database. The Docker image builds the React client, publishes the API and serves both from a single ASP.NET Core process. Use a current patched .NET 10 runtime and HTTPS in production.

## Docker Compose with HTTPS

Requirements: a Linux server with Docker Engine and Compose, a hostname whose DNS points to that server, and incoming TCP ports 80 and 443 free for Caddy.

1. Copy `.env.example` to `.env`. Set `DOMAIN` to your hostname and `POSTGRES_PASSWORD` to a long random value.
2. Run `docker compose -f compose.yml -f compose.prod.yml up --build -d`.
3. Open `https://your-hostname`, register your own account and complete setup.

`compose.prod.yml` adds Caddy, requires both values from `.env` and removes the local ports, so only 80 and 443 are reachable from outside. PostgreSQL is reachable only from the application container. Caddy obtains the certificate, terminates HTTPS, compresses responses and forwards to the internal application port. The application trusts forwarded headers only from the configured proxy address; if you change the Compose network, update `ReverseProxy__KnownProxy` to the proxy's new address.

The application reports its state at `/healthz`, which answers `Healthy` when the database is reachable. Compose checks it every 30 seconds with `dotnet IronMindGym.dll --check-health`, since the runtime image has no curl, and Caddy starts only once the application is healthy. `docker compose ps` shows the result.

The application container runs as a non-root user. `db-data` stores the database and `gym-keys` stores data protection keys. Keep both volumes across upgrades; Caddy's volumes keep its certificates. Do not run `docker compose down --volumes` when you want to keep data.

Demo accounts are disabled in Production. The connection string and key path come from environment variables; no secret is committed.

## Upgrades

Take a backup, pull the new revision and run the same `up --build -d` command. Migrations run when the application starts. Run a single application instance while it migrates.

## Backup and restore

Make a consistent backup of the running database with `pg_dump`:

```sh
docker compose exec -T db pg_dump -U ironmind -Fc ironmind > ironmind-$(date +%F).dump
```

Back up the data protection key volume separately. Encrypt and restrict access to backups, since they contain account and training data.

Restore into an empty database on an isolated instance first (`pg_restore --dbname ...`), then start the same application build so that it applies any newer migrations.

## Without Docker

Build the client into the API's `wwwroot` and publish:

```sh
cd client
npm ci
npm run build
cd ..
dotnet publish IronMindGym/IronMindGym.csproj --configuration Release --output artifacts/publish
```

Configure `ASPNETCORE_ENVIRONMENT=Production`, `ConnectionStrings__DefaultConnection`, `DataProtection__KeyDirectory`, `AllowedHosts` and the trusted reverse proxy address (`ReverseProxy__KnownProxy`). Run the published application as an unprivileged service behind an HTTPS proxy. The service account needs write access to the key directory, which must stay outside `wwwroot`, and the database user needs rights to create and alter the application's tables.

The first migration enables the `citext` extension, which PostgreSQL allows for a database owner.
