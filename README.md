# OOP-AAR-2026-CourseProject

Course project on object-oriented programming. As a topic choose music player app, named Rewind.

## База данных (PostgreSQL в Docker)

Приложение подключается к Postgres по адресу
`Host=localhost;Port=5432;Database=rewinddb;Username=postgres;Password=5329965`
(см. `Rewind/Helpers/DbManager.cs`).

Для разработки PostgreSQL запускается в Docker:

```powershell
# Старт БД (в фоне)
docker compose up -d

# Посмотреть статус / логи
docker compose ps
docker compose logs -f postgres

# Остановить (данные в volume остаются)
docker compose down

# Остановить и стереть данные (полный сброс БД — приложение пересоздаст схему при старте)
docker compose down -v
```

После `docker compose up -d` можно запускать `Rewind.exe` — схему и дефолтного админа (`Alexey` / `20062018no`) приложение создаст само через `Database.EnsureCreated()`.

Требуется: Docker Desktop (Windows) с включённой WSL2-интеграцией.
