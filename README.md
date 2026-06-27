# IronMind

Особистий тренувальний щоденник: силові вправи, кардіо, прогрес, харчування та цілі в одному місці. ASP.NET Core Web API · React · PostgreSQL · Docker.

## Стек

| Частина | Технології |
| --- | --- |
| API | ASP.NET Core 10 Web API, EF Core 10, Npgsql |
| База даних | PostgreSQL 18 (`citext`, `jsonb`, advisory locks) |
| Клієнт | React 19, TypeScript, Vite, React Router, Bootstrap 5 |
| Тести | xUnit, WebApplicationFactory, Testcontainers; Vitest, Testing Library |
| Доставка | Docker (багатоетапна збірка), Docker Compose, GitHub Actions |

## Запуск

Потрібні **Docker** (з Compose), а для розробки — **.NET 10 SDK** і **Node.js 24**.

### Усе в контейнерах

```sh
git clone https://github.com/Andrew543210/IronMindGym.git
cd IronMindGym
docker compose up --build
```

Відкрийте [localhost:8080](http://localhost:8080). Образ збирає React-клієнт і віддає його разом з API з одного контейнера. Дані PostgreSQL зберігаються в томі `db-data` і переживають перезапуск контейнерів.

### Розробка

```sh
docker compose up -d db
dotnet run --project IronMindGym --launch-profile http
```

В іншому терміналі:

```sh
cd client
npm ci
npm run dev
```

Відкрийте [localhost:5173](http://localhost:5173), створіть обліковий запис і заповніть початкові параметри. Vite оновлює сторінку після кожної зміни й передає запити `/api` до API на порту 5270, тому браузер бачить одне джерело, як у продакшні. Міграції створюють схему автоматично під час запуску API.

Локальна база слухає лише `127.0.0.1:5432` з обліковими даними `ironmind` / `ironmind-local` з `appsettings.Development.json`. Цей пароль призначений лише для бази на вашому комп'ютері.

### Демо

Використовуйте окрему базу, щоб не змішувати приклади зі своїми записами:

```sh
dotnet run --project IronMindGym --launch-profile http -- --Seed:DemoData=true --ConnectionStrings:DefaultConnection="Host=localhost;Port=5432;Database=ironmind_demo;Username=ironmind;Password=ironmind-local"
```

| Email | Пароль |
| --- | --- |
| demo@ironmind.app | DemoTraining123! |
| friend@ironmind.app | FriendTraining123! |

Демодані додаються лише в Development, з явним прапорцем і лише в базу без користувачів. У Production вони вимкнені незалежно від прапорця.

## Перевірка змін

API:

```sh
dotnet restore
dotnet build --configuration Release --no-restore --warnaserror
dotnet test --configuration Release --no-build
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project IronMindGym
dotnet list package --vulnerable --include-transitive
```

Клієнт:

```sh
cd client
npm run lint
npm run typecheck
npm test
npm run build
```

Тести API запускають справжній PostgreSQL 18 у Docker (Testcontainers): кожен тест отримує власну базу, скопійовану з мігрованого шаблону, тож робоча база не використовується. Щоб узяти наявний сервер замість Docker, задайте `IRONMIND_TEST_POSTGRES` (рядок підключення до сервера з правом створювати бази). Тести клієнта працюють у jsdom з підміненим API.

CI на кожен push у `master` і pull request збирає обидві частини, запускає тести, перевіряє узгодженість схеми, відомі вразливості залежностей і збірку Docker-образу.

## Дані та резервні копії

Бази, cookies, ключі, секрети, `.env` та артефакти збірки не належать до Git. Підключення задається через `ConnectionStrings:DefaultConnection`.

Резервна копія працюючої бази (узгоджений знімок без зупинки):

```sh
docker compose exec -T db pg_dump -U ironmind -Fc ironmind > ironmind.dump
```

Відновлення — у порожню базу через `pg_restore`; спершу перевірте копію на окремому екземплярі.

