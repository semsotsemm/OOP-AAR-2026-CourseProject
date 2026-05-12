# Rewind — установщик

Этот каталог содержит всё необходимое для сборки **Windows-установщика** Rewind,
который пользователь может скачать с GitHub и установить «в один клик».

## Что делает установщик

1. Копирует приложение и `docker-compose.yml` в `C:\Program Files\Rewind`.
2. При отсутствии Docker Desktop предлагает скачать и установить его автоматически.
3. Создаёт ярлыки в меню «Пуск» и (опционально) на рабочем столе.
4. Ярлык запускает `RewindLauncher.exe`, который:
   - запускает Docker Desktop (если не запущен) и ждёт его готовности;
   - поднимает контейнер PostgreSQL (`docker compose up -d`);
   - ждёт открытия порта `5433`;
   - запускает `Rewind.exe`.
5. При деинсталляции останавливает контейнер БД (данные в volume `rewind-pgdata` сохраняются).

## Структура

```
installer/
├── RewindLauncher/      WPF-лаунчер (.NET 10), показывает прогресс старта
├── setup.iss            Inno Setup скрипт
├── build-installer.ps1  Сборочный скрипт (publish + iscc)
└── README.md            этот файл
```

После сборки результат лежит в `build/installer/RewindSetup.exe`.

## Сборка

### Что нужно установить (один раз)

- [.NET SDK 10.x](https://dotnet.microsoft.com/download)
- [Inno Setup 6](https://jrsoftware.org/isdl.php)

### Команда

Из корня репозитория:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

(Если установлен PowerShell 7+, можно использовать `pwsh -File installer\build-installer.ps1`.)

Готовый установщик: `build\installer\RewindSetup.exe`.

## Параметры подключения к БД

Устанавливаются согласованно в трёх местах:

- `docker-compose.yml` — порт `5433:5432`, пароль `5329965`
- `Rewind/Helpers/AppDbContext.cs` — `Host=localhost;Port=5433;...`
- `installer/RewindLauncher/MainWindow.xaml.cs` — константы `DbHost`, `DbPort`

Если меняете порт/пароль — поменяйте во всех трёх местах.
