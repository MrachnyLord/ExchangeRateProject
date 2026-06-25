# Як запустити застосунок

Ця інструкція для того, хто вперше відкриває проєкт на своєму комп'ютері.

## Що потрібно встановити

Потрібен Windows 10 або 11, Visual Studio 2022 (з компонентом «ASP.NET і веб-розробка»), .NET SDK 10 і SQL Server LocalDB. LocalDB зазвичай ставиться разом з Visual Studio, окремо нічого качати не треба.

## Відкриття проєкту

1. Запустити Visual Studio.
2. File → Open → Project/Solution.
3. Вибрати файл `ExchangeRateProject.slnx` у папці з проєктом.
4. Почекати, поки внизу завантажаться NuGet-пакети.

## База даних

Рядок підключення лежить у `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CurrencyRatesDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
}
```

Щоб створити базу, відкрити Package Manager Console (View → Other Windows → Package Manager Console), у полі Default project вибрати `ExchangeRateProject` і виконати:

```
Update-Database
```

При першому запуску через F5 база теж створюється сама в `Program.cs`, тому `Update-Database` можна не робити, якщо одразу запускаєте проєкт.

Якщо змінювали моделі в коді:

```
Add-Migration НазваМіграції
Update-Database
```

## Запуск

Найпростіше — натиснути F5 у Visual Studio. Відкриється браузер з адресою з `Properties/launchSettings.json`.

Можна і через Package Manager Console:

```
dotnet run
```

Але для навчання зручніше F5.

## Що відбувається після запуску

При старті застосунок сам застосовує міграції, підключається до LocalDB, завантажує курси з API банків і записує їх у таблицю Rates. Далі у фоні кожні 5 хвилин курси оновлюються знову (налаштування в `appsettings.json`, секція ExchangeRateSync). На головній є кнопка «Оновити», якщо треба оновити вручну.

Детальніше про базу — у файлі DATABASE.md.

## Сторінки сайту

- `/` — поточні курси USD і EUR
- `/History` — курси за обрану дату
- `/Converter` — конвертер валют

## Публікація

У Package Manager Console:

```
dotnet publish -c Release -o ./publish
```

Або через меню Build → Publish.

## Якщо щось не працює

**Не підключається до бази** — перевірити рядок у `appsettings.json`, виконати `Update-Database`, переконатися що в PMC вибрано проєкт ExchangeRateProject.

**Курси не з'являються** — перевірити інтернет, натиснути «Оновити» на головній, подивитися вікно Output у Visual Studio на помилки.

**Порт зайнятий** — змінити порт у `Properties/launchSettings.json`.

## Структура папок

- `Pages/` — сторінки сайту
- `Services/` — завантаження курсів з API
- `Data/` і `Migrations/` — база даних
- `Models/` — класи для БД і для відображення
- `Program.cs` — запуск і налаштування
- `appsettings.json` — підключення до БД

Джерела API банків описані в BANK_SOURCES.md.
