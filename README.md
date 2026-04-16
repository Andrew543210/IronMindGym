# IronMind

Особистий тренувальний щоденник: силові вправи, кардіо, прогрес, харчування та цілі в одному місці. ASP.NET Core Web API · React · PostgreSQL · Docker.

## Запуск

Потрібні **Docker** (з Compose), а для розробки — **.NET 10 SDK**.

### Усе в контейнерах

```sh
git clone https://github.com/Andrew543210/IronMindGym.git
cd IronMindGym
docker compose up --build
```

API слухає [localhost:8080](http://localhost:8080). Дані PostgreSQL зберігаються в томі `db-data` і переживають перезапуск контейнерів.

### Розробка

```sh
docker compose up -d db
dotnet run --project IronMindGym --launch-profile http
```

API слухає [localhost:5270](http://localhost:5270). Міграції створюють схему автоматично під час запуску API.

Локальна база слухає лише `127.0.0.1:5432` з обліковими даними `ironmind` / `ironmind-local` з `appsettings.Development.json`. Цей пароль призначений лише для бази на вашому комп'ютері.

## Перевірка змін

```sh
dotnet restore
dotnet build --configuration Release --no-restore --warnaserror
dotnet test --configuration Release --no-build
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project IronMindGym
dotnet list package --vulnerable --include-transitive
```

Тести API запускають справжній PostgreSQL 18 у Docker (Testcontainers): кожен тест отримує власну базу, скопійовану з мігрованого шаблону, тож робоча база не використовується. Щоб узяти наявний сервер замість Docker, задайте `IRONMIND_TEST_POSTGRES` (рядок підключення до сервера з правом створювати бази).

CI на кожен push у `master` і pull request збирає API, запускає тести, перевіряє узгодженість схеми та відомі вразливості залежностей.
