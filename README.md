# IronMind (Lab 1.1)

ASP.NET Core MVC проєкт для відстеження тренувального прогресу, КБЖУ, базової соціальної взаємодії та статусу "аватара".

## Реалізовано

- MVC-архітектура: `Models`, `Data`, `Services`, `Controllers`, `ViewModels`, `Views`, `Seed`
- EF Core InMemory + початковий seed даних
- Базові сервіси:
  - `AvatarService` (коефіцієнт сили + статус аватара)
  - `NutritionAdviceService` (правила порад по харчуванню)
- Основні екрани:
  - Auth (Login/Register)
  - Setup
  - Dashboard
  - Exercise Library
  - Workout Logger
  - Nutrition
  - Statistics
  - Friends
  - Profile

## Швидкий старт

```powershell
cd C:\Users\Lenovo\Desktop\IronMind\IronMindGym\IronMindGym
dotnet restore
dotnet run
```

Після запуску відкрий URL з консолі (зазвичай `https://localhost:xxxx`).

## Демо-дані

- Користувач: `demo@ironmind.app` / `demo123`
- Друг: `friend@ironmind.app` / `friend123`

## Структура

- `IronMindGym/Program.cs` - конфігурація MVC, DI, seeding
- `IronMindGym/Data/IronMindDbContext.cs` - EF Core контекст і зв'язки
- `IronMindGym/Models/*.cs` - сутності предметної області
- `IronMindGym/Services/*.cs` - доменні сервіси
- `IronMindGym/Controllers/*.cs` - контролери екранів
- `IronMindGym/Views/*` - Razor представлення
- `IronMindGym/Seed/DataSeeder.cs` - початкові дані

