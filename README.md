# IronMind

Особистий тренувальний щоденник: силові вправи, кардіо, прогрес, харчування та цілі в одному місці. ASP.NET Core Web API · React · PostgreSQL · Docker.

![Build and test](https://github.com/Andrew543210/IronMindGym/actions/workflows/ci.yml/badge.svg)

![IronMind dashboard](docs/images/desktop.jpg)

[Мобільний вигляд](docs/images/mobile.jpg) · [Журнал тренування на телефоні](docs/images/workout-mobile.jpg)

## Можливості

- Журнал тренувань із силовими, кардіо та часовими підходами, оцінкою сесії, коментарями й історією.
- Пошук вправ, редагування підходів, попередній результат, таймери сесії й відпочинку. Незавершене тренування зберігається в браузері та на сервері, тож його можна продовжити на іншому пристрої.
- Суперсети й колові тренування зберігають порядок виконання; прогрес показує окремо зміну навантаження, повторень, тривалості та зусилля.
- 123 вправи у восьми групах: Arms, Back, Chest, Conditioning, Core, Legs, Mobility, Shoulders.
- Особисті рекорди, статистика, нагороди й ранг аватара за силовим коефіцієнтом.
- Журнал харчування за будь-яку дату, власні цілі КБЖВ та необов'язкові розрахункові орієнтири для дорослих.
- Профіль і план тренувань, журнал ваги, особисті цілі, зміна пароля й експорт даних у JSON.
- Друзі з підтвердженням запиту: тренування, харчування й прогрес видно лише після прийняття.
- Інтерфейс англійською та українською; вибір мови зберігається в обліковому записі.
- Стрічка з перевіреними першоджерелами WHO, CDC та наукових публікацій.

## Стек

| Частина | Технології |
| --- | --- |
| API | ASP.NET Core 10 Web API, EF Core 10, Npgsql |
| База даних | PostgreSQL 18 (`citext`, `jsonb`, advisory locks) |
| Клієнт | React 19, TypeScript, Vite, React Router, Bootstrap 5 |
| Тести | xUnit, WebApplicationFactory, Testcontainers; Vitest, Testing Library |
| Доставка | Docker (багатоетапна збірка), Docker Compose, Caddy, GitHub Actions |

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

Локальна база слухає лише `127.0.0.1:5432` з обліковими даними `ironmind` / `ironmind-local` з `appsettings.Development.json`. Цей пароль призначений лише для бази на вашому комп'ютері; продакшн вимагає власного (див. [розгортання](docs/deployment.md)).

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

## Телефон у залі

Для щоденного використання розгорніть застосунок на власному сервері з HTTPS за [інструкцією](docs/deployment.md). Інтерфейс розрахований на вузький екран і нижню навігацію.

Для перевірки в одній довіреній Wi-Fi мережі зберіть клієнт у `wwwroot` API і запустіть один процес:

```sh
cd client && npm run build && cd ..
dotnet run --project IronMindGym --launch-profile http -- --urls http://0.0.0.0:5270
```

Відкрийте `http://<IP-адреса-комп'ютера>:5270` на телефоні. Development і HTTP призначені лише для локальної перевірки.

Незавершене тренування зберігається в `localStorage` окремо для кожного користувача, а його копія надсилається на сервер через 1,5 с після останньої зміни. Воно переживає перезавантаження, закриття вкладки й вивантаження вкладки телефоном, синхронізується між відкритими вкладками, продовжується на іншому пристрої й видаляється після збереження або кнопкою **Discard workout**. Повторне надсилання того самого тренування (подвійний тап, повтор після втраченої відповіді) не створює дубліката. Для збереження потрібне з'єднання із сервером; це не офлайн-застосунок. [Правила запису й порівнянь](docs/athlete-workflows.md).

## Архітектура

Один ASP.NET Core застосунок віддає JSON API під `/api` і зібраний React-клієнт з `wwwroot`. Шар репозиторіїв поверх EF Core не потрібен: сервіси працюють з `DbContext` напряму.

```text
IronMindGym/              ASP.NET Core Web API
  Api/                    Контролери: маршрути, валідація запиту, коди відповіді
  Services/               Операції журналу, профілю, дружби, статистики й нагород
  Data/                   DbContext, обмеження, міграції, блокування записів
  Models/                 Сутності та правила предметної області
  Contracts/              Запити й відповіді API
  Security/               Паролі та поточний користувач
  Localization/           Вибір англійського чи українського тексту
  Seed/                   Довідники, каталог вправ, перевірені матеріали, демодані
IronMindGym.Tests/        Інтеграційні тести API та юніт-тести правил
client/                   React-клієнт (Vite, TypeScript)
  src/pages/              Сторінки за розділами
  src/components/         Макет, навігація, спільні елементи
  src/i18n/               Тексти інтерфейсу англійською та українською
  src/test/               Спільні засоби для тестів
```

Сесія — HttpOnly cookie; кожна зміна додатково несе antiforgery-токен у заголовку `X-CSRF-TOKEN`. Контролер передає сервісу `userId` поточного користувача, і сервіс обмежує операцію цим власником. Час зберігається в UTC, а «сьогодні», тиждень (з понеділка) і серії рахуються в часовому поясі профілю. Докладніше — в [описі архітектури](docs/architecture.md).

## Перевірка змін

API:

```sh
dotnet restore
dotnet build --configuration Release --no-restore --warnaserror
dotnet test --configuration Release --no-build --collect "XPlat Code Coverage"
dotnet tool restore
dotnet ef migrations has-pending-model-changes --project IronMindGym
dotnet list package --vulnerable --include-transitive
```

Клієнт:

```sh
cd client
npm run lint
npm run typecheck
npm run coverage
npm run build
```

Тести API запускають справжній PostgreSQL 18 у Docker (Testcontainers): кожен тест отримує власну базу, скопійовану з мігрованого шаблону, тож робоча база не використовується. Щоб узяти наявний сервер замість Docker, задайте `IRONMIND_TEST_POSTGRES` (рядок підключення до сервера з правом створювати бази). Тести клієнта працюють у jsdom з підміненим API.

CI на кожен push у `master` і pull request збирає обидві частини, запускає тести, вимагає щонайменше 80% покриття рядків і гілок, перевіряє узгодженість схеми, відомі вразливості залежностей і збірку Docker-образу.

## Дані та резервні копії

Бази, cookies, ключі, секрети, `.env` та артефакти збірки не належать до Git. Підключення задається через `ConnectionStrings:DefaultConnection`.

Резервна копія працюючої бази (узгоджений знімок без зупинки):

```sh
docker compose exec -T db pg_dump -U ironmind -Fc ironmind > ironmind.dump
```

Відновлення — у порожню базу через `pg_restore`; спершу перевірте копію на окремому екземплярі. JSON-експорт у профілі зручний для перегляду власних записів, але не є форматом повного відновлення сервера.

Додатково: [архітектура](docs/architecture.md), [тренувальні сценарії](docs/athlete-workflows.md), [каталог вправ](docs/exercises.md), [джерела стрічки](docs/sources.md), [розгортання](docs/deployment.md), [безпека](SECURITY.md).
