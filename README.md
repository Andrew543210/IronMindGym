# IronMind

Особистий тренувальний щоденник: силові вправи, кардіо, прогрес, харчування та цілі в одному місці. ASP.NET Core Web API · React · PostgreSQL · Docker.

## Розробка

Потрібні **Docker** (з Compose) і **.NET 10 SDK**.

```sh
docker compose up -d db
dotnet run --project IronMindGym --launch-profile http
```

API слухає [localhost:5270](http://localhost:5270). Міграції створюють схему автоматично під час запуску API.

Локальна база слухає лише `127.0.0.1:5432` з обліковими даними `ironmind` / `ironmind-local` з `appsettings.Development.json`. Цей пароль призначений лише для бази на вашому комп'ютері.
