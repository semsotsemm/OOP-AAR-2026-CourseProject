"""
seed.py — Rewind DB Seeder
==========================
Наполняет PostgreSQL-базу приложения Rewind пользователями, треками,
альбомами и плейлистами, воспроизводя ту же логику хеширования паролей
и хранения файлов, что используется в C#-приложении.

Зависимости:
    pip install psycopg2-binary mutagen

    mutagen опционален: без него длительность треков будет записана как 0.

Использование:
    python seed.py --music-dir C:/путь/к/музыке --app-dir C:/путь/к/Rewind/bin/Debug/net8.0-windows

Формат music-dir/tracks.json см. в tracks_example.json рядом со скриптом.
"""

import argparse
import base64
import hashlib
import json
import os
import random
import re
import secrets
import shutil
import subprocess
import sys
from pathlib import Path

try:
    import psycopg2
    from psycopg2.extras import RealDictCursor
except ImportError:
    print("[ОШИБКА] Установите psycopg2: pip install psycopg2-binary")
    sys.exit(1)

DB_HOST     = "localhost"
DB_PORT     = 5433
DB_NAME     = "rewinddb"
DB_USER     = "postgres"
DB_PASSWORD = "5329965"

_SALT_SIZE  = 16        
_KEY_SIZE   = 32        
_ITERATIONS = 100_000   
_HASH_ALGO  = "sha256"  

ROLE_ADMIN    = 1
ROLE_ARTIST   = 2
ROLE_LISTENER = 3

LISTENERS = [
    {"nickname": "Иван Петров",       "email": "ivan@rewind.local",       "password": "password123",  "status": "Активен"},
    {"nickname": "Марина Иванова",    "email": "marina@rewind.local",     "password": "password123",  "status": "Активен"},
    {"nickname": "Сергей Морозов",    "email": "sergey@rewind.local",     "password": "qwerty456",    "status": "Активен"},
    {"nickname": "Ольга Сидорова",     "email": "olga@rewind.local",       "password": "mypass789",    "status": "Активен"},
    {"nickname": "Дмитрий Волков",     "email": "dmitry@rewind.local",     "password": "pass2024",     "status": "Активен"},
    {"nickname": "Алексей Кузнецов",  "email": "alexei@rewind.local",     "password": "alex2024",     "status": "Активен"},
    {"nickname": "Наталья Смирнова",  "email": "natalia@rewind.local",    "password": "nata_pass",    "status": "Активен"},
    {"nickname": "Павел Федоров",     "email": "pavel@rewind.local",      "password": "pavel123",     "status": "Активен"},
    {"nickname": "Елена Попова",      "email": "elena@rewind.local",      "password": "elena_pw",     "status": "Активен"},
    {"nickname": "Михаил Соколов",   "email": "mikhail@rewind.local",    "password": "misha2024",    "status": "Активен"},
    {"nickname": "Анна Козлова",      "email": "anna@rewind.local",       "password": "annpass1",     "status": "Активен"},
    {"nickname": "Никита Новиков",    "email": "nikita@rewind.local",     "password": "nik_secret",   "status": "Активен"},
    {"nickname": "Ульяна Морозова",    "email": "yulia@rewind.local",      "password": "yulia_pw",     "status": "Активен"},
    {"nickname": "Андрей Волков",     "email": "andrei@rewind.local",     "password": "andrei456",    "status": "Активен"},
    {"nickname": "Ксения Лебедева",   "email": "ksenia@rewind.local",     "password": "ksenya2024",   "status": "Активен"},
    {"nickname": "Роман Орлов",       "email": "roman@rewind.local",      "password": "roma_pass",    "status": "Активен"},
    {"nickname": "Татьяна Виноградова","email": "tatiana@rewind.local",   "password": "tanya123",     "status": "Активен"},
    {"nickname": "Артем Соловьев",    "email": "artem@rewind.local",      "password": "art3m_pw",     "status": "Активен"},
    {"nickname": "Дарья Зайцева",    "email": "daria@rewind.local",      "password": "dashka99",     "status": "Активен"},
    {"nickname": "Игорь Белов",        "email": "igor@rewind.local",       "password": "igpass2024",   "status": "Активен"},
    {"nickname": "Светлана Титова",   "email": "svetlana@rewind.local",   "password": "sveta_pw",     "status": "Активен"},
    {"nickname": "Максим Гусев",       "email": "maxim@rewind.local",      "password": "max1mum",      "status": "Активен"},
    {"nickname": "Полина Зайцева", "email": "polina@rewind.local",     "password": "polpass",      "status": "Активен"},
    {"nickname": "Виктор Рубанов",     "email": "viktor@rewind.local",     "password": "vik_2025",     "status": "Активен"},
    {"nickname": "Алена Степанова",   "email": "alina@rewind.local",      "password": "alinaqwerty",  "status": "Активен"},
    {"nickname": "Alex Reid",         "email": "alex.reid@rewind.local",  "password": "reidpass1",    "status": "Активен"},
    {"nickname": "Sophie Martin",     "email": "sophie@rewind.local",     "password": "sofipass",     "status": "Активен"},
    {"nickname": "James Wright",      "email": "james@rewind.local",      "password": "jwright99",    "status": "Активен"},
    {"nickname": "Lena Fischer",      "email": "lena@rewind.local",       "password": "lena_de",      "status": "Активен"},
    {"nickname": "Carlos Mendez",     "email": "carlos@rewind.local",     "password": "carlito24",    "status": "Активен"},
    {"nickname": "Yuki Tanaka",       "email": "yuki@rewind.local",       "password": "yukipass",     "status": "Активен"},
    {"nickname": "Emma Johnson",      "email": "emma@rewind.local",       "password": "emmarock",     "status": "Активен"},
    {"nickname": "Lucas Bernard",     "email": "lucas@rewind.local",      "password": "lucasb2024",   "status": "Активен"},
    {"nickname": "Mia Chen",          "email": "mia.chen@rewind.local",   "password": "mia_pw123",    "status": "Активен"},
    {"nickname": "Noah Wilson",       "email": "noah@rewind.local",       "password": "noahwils",     "status": "Активен"},
    {"nickname": "Chloe Dubois",      "email": "chloe@rewind.local",      "password": "chloefr",      "status": "Активен"},
    {"nickname": "Ethan  Brooks",      "email": "ethan@rewind.local",      "password": "eth4n_pw",     "status": "Активен"},
    {"nickname": "Isabella Garcia",   "email": "isabella@rewind.local",   "password": "isagc2024",    "status": "Активен"},
    {"nickname": "Oliver Smith",      "email": "oliver@rewind.local",     "password": "olivs_1",      "status": "Активен"},
    {"nickname": "Amelia Jones",      "email": "amelia@rewind.local",     "password": "amelj99",      "status": "Активен"},
    {"nickname": "William_ Taylor",    "email": "william@rewind.local",    "password": "willy_t",      "status": "Активен"},
    {"nickname": "Aria Patel",        "email": "aria@rewind.local",       "password": "ariapat",      "status": "Активен"},
    {"nickname": "Liam Oconnor",      "email": "liam@rewind.local",       "password": "liamocon",     "status": "Активен"},
    {"nickname": "Zoe Kim",           "email": "zoe.kim@rewind.local",    "password": "zoekim24",     "status": "Активен"},
    {"nickname": "Samue Lee",        "email": "samuel@rewind.local",     "password": "sam_lee1",     "status": "Активен"},
    {"nickname": "Grace White",       "email": "grace@rewind.local",      "password": "gracepw",      "status": "Активен"},
    {"nickname": "Daniel Brown",      "email": "daniel@rewind.local",     "password": "dan_brow",     "status": "Активен"},
    {"nickname": "Nadia Muller",      "email": "nadia@rewind.local",      "password": "nadiaml",      "status": "Активен"},
    {"nickname": "Tom Nguyen",        "email": "tom@rewind.local",        "password": "tomngu24",     "status": "Активен"},
    {"nickname": "sara Hassan",       "email": "sara@rewind.local",       "password": "sarahpw",      "status": "Активен"},
]

DEFAULT_ARTISTS = [
    {"nickname": "Nocturne_Wave",   "email": "nocturne@rewind.local",   "password": "artist_pass1",  "status": "Верифицирован"},
    {"nickname": "Solar_Beats",     "email": "solar@rewind.local",      "password": "artist_pass2",  "status": "Верифицирован"},
    {"nickname": "Luna_Echo",       "email": "luna@rewind.local",       "password": "artist_pass3",  "status": "Верифицирован"},
    {"nickname": "Crimson_Tide",    "email": "crimson@rewind.local",    "password": "artist_pass4",  "status": "Верифицирован"},
    {"nickname": "Velvet_Storm",    "email": "velvet@rewind.local",     "password": "artist_pass5",  "status": "Верифицирован"},
    {"nickname": "Neon_Pulse",      "email": "neonpulse@rewind.local",  "password": "artist_pass6",  "status": "Верифицирован"},
    {"nickname": "Arctic_Void",     "email": "arctic@rewind.local",     "password": "artist_pass7",  "status": "Верифицирован"},
    {"nickname": "Golden_Ratio",    "email": "golden@rewind.local",     "password": "artist_pass8",  "status": "Верифицирован"},
    {"nickname": "Prism_Break",     "email": "prism@rewind.local",      "password": "artist_pass9",  "status": "Верифицирован"},
    {"nickname": "Echo_Valley",     "email": "echoval@rewind.local",    "password": "artist_pass10", "status": "Верифицирован"},
    {"nickname": "Thunder_Road",    "email": "thunder@rewind.local",    "password": "artist_pass11", "status": "Верифицирован"},
    {"nickname": "Sapphire_Sky",    "email": "sapphire@rewind.local",   "password": "artist_pass12", "status": "Верифицирован"},
    {"nickname": "Digital_Ghost",   "email": "digital@rewind.local",    "password": "artist_pass13", "status": "Верифицирован"},
    {"nickname": "Forest_Dream",    "email": "forest@rewind.local",     "password": "artist_pass14", "status": "Верифицирован"},
    {"nickname": "Violet_Noise",    "email": "violet@rewind.local",     "password": "artist_pass15", "status": "Верифицирован"},
    {"nickname": "Iron_Phoenix",    "email": "ironphx@rewind.local",    "password": "artist_pass16", "status": "Верифицирован"},
    {"nickname": "Crystal_Cave",    "email": "crystal@rewind.local",    "password": "artist_pass17", "status": "Верифицирован"},
    {"nickname": "Red_Frequency",   "email": "redfreq@rewind.local",    "password": "artist_pass18", "status": "Верифицирован"},
    {"nickname": "Midnight_Sun",    "email": "midnightsun@rewind.local","password": "artist_pass19", "status": "Верифицирован"},
    {"nickname": "Storm_Atlas",     "email": "stormatlас@rewind.local", "password": "artist_pass20", "status": "Верифицирован"},
]

PLAYLIST_TEMPLATES = [
    ("Мои любимые",          False),
    ("Для тренировки",       False),
    ("Вечерний релакс",      False),
    ("Рабочая атмосфера",    False),
    ("В дорогу",             False),
    ("Ночной драйв",         False),
    ("Утреннее настроение",  False),
    ("Вечеринка",            False),
    ("Концентрация",         True),
    ("Пляжный сезон",        False),
    ("Ретро-волна",          False),
    ("Медитация",            False),
    ("Спорт и энергия",      False),
    ("Осенняя меланхолия",   True),
    ("Для готовки",          False),
    ("Открытие недели",      False),
    ("Гитарный вечер",       False),
    ("Электронный хаос",     True),
]

PLAYLISTS_PER_USER = 3
TRACKS_PER_PLAYLIST = 10

random.seed(42)



def hash_password(password: str) -> str:
    salt = secrets.token_bytes(_SALT_SIZE)
    dk = hashlib.pbkdf2_hmac(_HASH_ALGO, password.encode("utf-8"), salt, _ITERATIONS, dklen=_KEY_SIZE)
    return f"{base64.b64encode(salt).decode()}:{base64.b64encode(dk).decode()}"


def sanitize_filename(name: str) -> str:
    invalid = set(r'\/:*?"<>|')
    result = "".join("_" if c in invalid else c for c in name).strip()
    return result or "file"


def copy_to_music_library(src: Path, track_title: str, library_dir: Path) -> str:
    library_dir.mkdir(parents=True, exist_ok=True)
    ext       = src.suffix
    base_name = sanitize_filename(track_title) or "track"
    file_name = f"{base_name}{ext}"
    dest      = library_dir / file_name

    n = 1
    while dest.exists() and not _same_file(src, dest):
        file_name = f"{base_name}_{n}{ext}"
        dest      = library_dir / file_name
        n += 1

    if not dest.exists():
        shutil.copy2(src, dest)

    return file_name  


def copy_to_image_folder(src: Path, relative_folder: str, app_dir: Path) -> str:
    dest_dir = app_dir / "Images" / relative_folder
    dest_dir.mkdir(parents=True, exist_ok=True)

    base_name = src.stem
    ext       = src.suffix
    file_name = f"{base_name}{ext}"
    dest      = dest_dir / file_name

    n = 1
    while dest.exists() and not _same_file(src, dest):
        file_name = f"{base_name}_{n}{ext}"
        dest      = dest_dir / file_name
        n += 1

    if not dest.exists():
        shutil.copy2(src, dest)

    return f"Images/{relative_folder}/{file_name}"


def _same_file(a: Path, b: Path) -> bool:
    try:
        return a.resolve() == b.resolve()
    except Exception:
        return False


# Проверяем mutagen один раз при импорте, чтобы дать пользователю чёткую ошибку.
try:
    from mutagen import File as _MutagenFile  # type: ignore
    _MUTAGEN_AVAILABLE = True
except ImportError:
    _MUTAGEN_AVAILABLE = False
    _MutagenFile = None  # type: ignore


def get_audio_duration(file_path: Path) -> int:
    """Возвращает длительность аудио в секундах. Пробует несколько парсеров mutagen,
    т.к. файлы из SoundCloud-конвертеров часто имеют битые/отсутствующие ID3-заголовки."""
    if not _MUTAGEN_AVAILABLE:
        return 0

    # 1. Универсальный авто-парсер (определяет формат по содержимому)
    try:
        audio = _MutagenFile(str(file_path))
        if audio and audio.info and getattr(audio.info, "length", 0) > 0:
            return int(audio.info.length)
    except Exception:
        pass

    # 2. Принудительный MP3-парсер — работает даже когда нет ID3-тегов
    try:
        from mutagen.mp3 import MP3  # type: ignore
        audio = MP3(str(file_path))
        if audio and audio.info and audio.info.length > 0:
            return int(audio.info.length)
    except Exception:
        pass

    # 3. MP3-парсер без обработки ID3 (для совсем "сырых" файлов)
    try:
        from mutagen.mp3 import MP3  # type: ignore
        audio = MP3(str(file_path), ID3=None)
        if audio and audio.info and audio.info.length > 0:
            return int(audio.info.length)
    except Exception:
        pass

    # 4. MP4/M4A (некоторые "soundcloud_to_mp3" по факту .m4a)
    try:
        from mutagen.mp4 import MP4  # type: ignore
        audio = MP4(str(file_path))
        if audio and audio.info and audio.info.length > 0:
            return int(audio.info.length)
    except Exception:
        pass

    # 5. ffprobe из ffmpeg — самый универсальный (если установлен)
    ffprobe = _ffprobe_path()
    if ffprobe:
        try:
            result = subprocess.run(
                [
                    ffprobe, "-v", "error",
                    "-show_entries", "format=duration",
                    "-of", "default=noprint_wrappers=1:nokey=1",
                    str(file_path),
                ],
                capture_output=True, text=True, timeout=15,
            )
            if result.returncode == 0:
                txt = result.stdout.strip()
                if txt:
                    return int(float(txt))
        except Exception:
            pass

    return 0


_FFPROBE_CACHED: str | None = ""  # "" = ещё не искали, None = искали и не нашли

def _ffprobe_path() -> str | None:
    """Возвращает путь к ffprobe (из PATH или из стандартных winget/choco-локаций),
    или None если не нашёлся."""
    global _FFPROBE_CACHED
    if _FFPROBE_CACHED != "":
        return _FFPROBE_CACHED

    # 1. В PATH
    found = shutil.which("ffprobe")
    if found:
        _FFPROBE_CACHED = found
        return _FFPROBE_CACHED

    # 2. Стандартные пути установки на Windows
    candidates = []
    local_app = os.environ.get("LOCALAPPDATA", "")
    if local_app:
        # winget packages
        winget_root = Path(local_app) / "Microsoft" / "WinGet" / "Packages"
        if winget_root.exists():
            candidates.extend(winget_root.glob("Gyan.FFmpeg*/**/bin/ffprobe.exe"))
            candidates.extend(winget_root.glob("*FFmpeg*/**/bin/ffprobe.exe"))
    # Стандартные ручные установки
    for root in (r"C:\ffmpeg", r"C:\Program Files\ffmpeg",
                 r"C:\Program Files (x86)\ffmpeg", r"C:\ProgramData\chocolatey\bin"):
        p = Path(root) / "bin" / "ffprobe.exe"
        if p.exists():
            candidates.append(p)
        p2 = Path(root) / "ffprobe.exe"
        if p2.exists():
            candidates.append(p2)

    for c in candidates:
        if c.exists():
            _FFPROBE_CACHED = str(c)
            return _FFPROBE_CACHED

    _FFPROBE_CACHED = None
    return None


def _ffprobe_available() -> bool:
    return _ffprobe_path() is not None


def ensure_schema_ready(cur):
    """Проверяет что таблицы созданы. Если нет — даёт понятную ошибку."""
    cur.execute("""
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'Roles'
    """)
    if not cur.fetchone():
        print()
        print("=" * 70)
        print("[ОШИБКА] Таблицы в базе данных не созданы.")
        print("=" * 70)
        print("Сначала запустите приложение Rewind (хотя бы один раз) —")
        print("EF Core создаст все таблицы автоматически через EnsureCreated().")
        print()
        print("После этого можно повторно запустить:  python seed.py")
        print("=" * 70)
        sys.exit(1)


def ensure_roles(cur):
    for role_id, role_name in [(1, "Admin"), (2, "Artist"), (3, "Listener")]:
        cur.execute('SELECT 1 FROM "Roles" WHERE "RoleId" = %s', (role_id,))
        if not cur.fetchone():
            cur.execute('INSERT INTO "Roles" ("RoleId", "RoleName") VALUES (%s, %s)', (role_id, role_name))
            print(f'  [Роль] Создана: {role_name}')


def ensure_user(cur, nickname: str, email: str, password: str, role_id: int, status: str) -> int:
    cur.execute('SELECT "UserId" FROM "Users" WHERE "Nickname" = %s', (nickname,))
    row = cur.fetchone()
    if row:
        return row["UserId"]
    pw_hash = hash_password(password)
    cur.execute(
        'INSERT INTO "Users" ("Nickname", "Email", "PasswordHash", "RoleId", "Status") '
        'VALUES (%s, %s, %s, %s, %s) RETURNING "UserId"',
        (nickname, email, pw_hash, role_id, status),
    )
    return cur.fetchone()["UserId"]


def ensure_track(cur, title: str, file_path_db: str, cover_path_db: str | None,
                 duration: int, artist_id: int, genre: str | None) -> int:
    """Создаёт трек если не существует.
    Если трек уже есть — обновляет Duration/CoverPath/Genre если они изменились/были пустыми."""
    cur.execute(
        'SELECT "TrackID", "Duration", "CoverPath", "Genre" FROM "Tracks" '
        'WHERE "Title" = %s AND "ArtistID" = %s',
        (title, artist_id),
    )
    row = cur.fetchone()
    if row:
        updates = []
        params  = []
        if duration > 0 and (row["Duration"] or 0) <= 0:
            updates.append('"Duration" = %s'); params.append(duration)
        if cover_path_db and not row["CoverPath"]:
            updates.append('"CoverPath" = %s'); params.append(cover_path_db)
        if genre and (not row["Genre"] or row["Genre"] == "Разное"):
            updates.append('"Genre" = %s'); params.append(genre)
        if updates:
            params.append(row["TrackID"])
            cur.execute(
                f'UPDATE "Tracks" SET {", ".join(updates)} WHERE "TrackID" = %s',
                tuple(params),
            )
        return row["TrackID"]

    cur.execute(
        'INSERT INTO "Tracks" '
        '("Title", "FilePath", "CoverPath", "Duration", "UploadDate", "ArtistID", "Genre", "PublishStatus") '
        'VALUES (%s, %s, %s, %s, NOW(), %s, %s, %s) RETURNING "TrackID"',
        (title, file_path_db, cover_path_db, duration, artist_id, genre, "Published"),
    )
    track_id = cur.fetchone()["TrackID"]

    cur.execute(
        'INSERT INTO "Statistics" ("TrackID", "PlayCount", "LikesCount") VALUES (%s, 0, 0)',
        (track_id,),
    )
    return track_id


def ensure_album(cur, title: str, artist_id: int, cover_path_db: str | None, genre: str | None) -> int:
    cur.execute(
        'SELECT "AlbumId" FROM "Albums" WHERE "Title" = %s AND "ArtistId" = %s',
        (title, artist_id),
    )
    row = cur.fetchone()
    if row:
        return row["AlbumId"]
    cur.execute(
        'INSERT INTO "Albums" ("Title", "ArtistId", "CoverPath", "Genre", "CreatedAt") '
        'VALUES (%s, %s, %s, %s, NOW()) RETURNING "AlbumId"',
        (title, artist_id, cover_path_db, genre),
    )
    return cur.fetchone()["AlbumId"]


def link_album_track(cur, album_id: int, track_id: int):
    cur.execute('SELECT 1 FROM "AlbumTracks" WHERE "AlbumId" = %s AND "TrackId" = %s', (album_id, track_id))
    if not cur.fetchone():
        cur.execute('INSERT INTO "AlbumTracks" ("AlbumId", "TrackId") VALUES (%s, %s)', (album_id, track_id))


def ensure_playlist(cur, title: str, owner_id: int, is_private: bool, cover_path_db: str | None) -> int:
    """Создаёт плейлист если не существует.
    Если плейлист уже есть без обложки — обновляет CoverPath. Возвращает PlaylistID."""
    cur.execute(
        'SELECT "PlaylistID", "CoverPath" FROM "Playlists" WHERE "Title" = %s AND "OwnerID" = %s',
        (title, owner_id),
    )
    row = cur.fetchone()
    if row:
        if cover_path_db and not row["CoverPath"]:
            cur.execute(
                'UPDATE "Playlists" SET "CoverPath" = %s WHERE "PlaylistID" = %s',
                (cover_path_db, row["PlaylistID"]),
            )
        return row["PlaylistID"]
    cur.execute(
        'INSERT INTO "Playlists" ("Title", "OwnerID", "IsPrivate", "CoverPath") '
        'VALUES (%s, %s, %s, %s) RETURNING "PlaylistID"',
        (title, owner_id, is_private, cover_path_db),
    )
    return cur.fetchone()["PlaylistID"]


def link_playlist_track(cur, playlist_id: int, track_id: int):
    cur.execute(
        'SELECT 1 FROM "PlaylistTracks" WHERE "PlaylistID" = %s AND "TrackID" = %s',
        (playlist_id, track_id),
    )
    if not cur.fetchone():
        cur.execute(
            'INSERT INTO "PlaylistTracks" ("PlaylistID", "TrackID") VALUES (%s, %s)',
            (playlist_id, track_id),
        )



_SCRIPT_DIR      = Path(__file__).resolve().parent
_DEFAULT_MUSIC   = Path(r"C:\Users\untermensh\Downloads\music")

def _find_app_dir() -> Path | None:
    """Ищет папку bin приложения рядом со скриптом (Debug → Release → любой, любой .NET)."""
    for pattern in ("*/bin/Debug/net*-windows", "*/bin/Release/net*-windows",
                    "*/bin/*/net*-windows"):
        found = list(_SCRIPT_DIR.glob(pattern))
        if found:
            return found[0]
    return None


def refresh_durations_mode(cur, app_dir: Path) -> None:
    """Читает все треки с Duration=0, пытается прочитать длительность из MusicLibrary/, обновляет БД."""
    music_lib = app_dir / "MusicLibrary"
    if not music_lib.exists():
        print(f"[ОШИБКА] Папка MusicLibrary не найдена: {music_lib}")
        sys.exit(1)

    cur.execute(
        'SELECT "TrackID", "Title", "FilePath", "Duration" FROM "Tracks" '
        'WHERE "Duration" IS NULL OR "Duration" <= 0 '
        'ORDER BY "TrackID"'
    )
    rows = cur.fetchall()
    if not rows:
        print("[OK] Все треки уже имеют длительность. Нечего обновлять.")
        return

    print(f"=== Найдено {len(rows)} треков с Duration=0 ===")
    print(f"   mutagen:  {'установлен' if _MUTAGEN_AVAILABLE else 'НЕ установлен'}")
    ffp = _ffprobe_path()
    print(f"   ffprobe:  {ffp if ffp else 'НЕ найден'}\n")

    if not _MUTAGEN_AVAILABLE and not _ffprobe_available():
        print("[ОШИБКА] Ни mutagen, ни ffprobe не доступны. Установите:")
        print("  pip install mutagen")
        print("  и/или установите ffmpeg (https://ffmpeg.org/download.html)")
        sys.exit(1)

    fixed = failed = 0
    failed_tracks: list[tuple[str, int]] = []  # (title, file_size)
    for r in rows:
        track_id  = r["TrackID"]
        title     = r["Title"]
        file_name = r["FilePath"]
        audio     = music_lib / file_name

        if not audio.exists():
            print(f"  [ПРОПУСК] Файл не найден: {audio.name}  (трек '{title}')")
            failed += 1
            continue

        dur = get_audio_duration(audio)
        if dur > 0:
            cur.execute(
                'UPDATE "Tracks" SET "Duration" = %s WHERE "TrackID" = %s',
                (dur, track_id),
            )
            mm, ss = dur // 60, dur % 60
            print(f"  [OK] '{title}' → {mm}:{ss:02d}  ({dur}s)")
            fixed += 1
        else:
            size_kb = audio.stat().st_size // 1024
            print(f"  [НЕ ПРОЧИТАЛОСЬ] '{title}'  ({size_kb} KB)")
            failed_tracks.append((title, size_kb))
            failed += 1

    print(f"\n=== Итого: обновлено {fixed}, не удалось {failed} ===")

    if failed_tracks and not _ffprobe_available():
        print()
        print("ПОДСКАЗКА: эти треки могут быть в нестандартном формате (m4a/opus под видом mp3).")
        print("Установите ffmpeg и перезапустите скрипт — это самый универсальный парсер:")
        print("  Windows:  winget install -e --id Gyan.FFmpeg")
        print("            (или скачайте с https://ffmpeg.org/download.html)")


def main():
    parser = argparse.ArgumentParser(
        description="Rewind DB Seeder — наполняет базу данных пользователями, треками, альбомами и плейлистами.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Примеры (все аргументы необязательны — есть умные умолчания):
  python seed.py
  python seed.py --dry-run
  python seed.py --music-dir D:/MyMusic --app-dir C:/Rewind/bin/Release/net8.0-windows
        """,
    )
    parser.add_argument(
        "--music-dir",
        default=str(_DEFAULT_MUSIC),
        help=f"Директория с аудиофайлами (default: {_DEFAULT_MUSIC})",
    )
    parser.add_argument(
        "--app-dir",
        default=None,
        help="Директория запуска приложения (MusicLibrary/, Images/ ...). "
             "По умолчанию ищется автоматически в подпапках рядом со скриптом.",
    )
    parser.add_argument(
        "--dry-run", action="store_true",
        help="Только выводит что будет сделано, не изменяет БД и не копирует файлы",
    )
    parser.add_argument(
        "--refresh-durations", action="store_true",
        help="НЕ запускает основной сидер. Только обновляет Duration у треков с Duration=0, "
             "читая файлы из app-dir/MusicLibrary/.",
    )
    parser.add_argument(
        "--db-host",     default=DB_HOST,
        help=f"PostgreSQL хост (default: {DB_HOST})",
    )
    parser.add_argument(
        "--db-port",     default=DB_PORT, type=int,
        help=f"PostgreSQL порт (default: {DB_PORT})",
    )
    parser.add_argument(
        "--db-name",     default=DB_NAME,
        help=f"Имя базы данных (default: {DB_NAME})",
    )
    parser.add_argument(
        "--db-user",     default=DB_USER,
        help=f"Пользователь PostgreSQL (default: {DB_USER})",
    )
    parser.add_argument(
        "--db-password", default=DB_PASSWORD,
        help="Пароль PostgreSQL",
    )
    args = parser.parse_args()

    music_dir = Path(args.music_dir).resolve()
    dry_run   = args.dry_run

    # ── Определяем app-dir ────────────────────────────────────────────────────
    if args.app_dir:
        app_dir = Path(args.app_dir).resolve()
    else:
        app_dir = _find_app_dir()
        if app_dir is None:
            print("[ОШИБКА] Не удалось найти папку bin приложения автоматически.")
            print("Укажите её вручную: --app-dir путь/к/Rewind/bin/Debug/net8.0-windows")
            sys.exit(1)
        print(f"[auto] app-dir: {app_dir}")

    # ── Ищем tracks.json ──────────────────────────────────────────────────────
    # Сначала рядом со скриптом, потом в music-dir
    tracks_json_path: Path | None = None
    for candidate in (_SCRIPT_DIR / "tracks.json", music_dir / "tracks.json"):
        if candidate.exists():
            tracks_json_path = candidate
            break
    if tracks_json_path is None:
        print(f"[ОШИБКА] Файл tracks.json не найден.")
        print(f"  Искал в: {_SCRIPT_DIR}")
        print(f"           {music_dir}")
        print("Переименуйте tracks_example.json в tracks.json и положите в одно из мест выше.")
        sys.exit(1)

    with open(tracks_json_path, encoding="utf-8") as f:
        raw = f.read()
    # Поддержка // комментариев (не входят в стандарт JSON, но удобны для примеров)
    raw = re.sub(r'//[^\n]*', '', raw)
    data = json.loads(raw)

    tracks_meta = data.get("tracks", [])
    if not tracks_meta:
        print("[ПРЕДУПРЕЖДЕНИЕ] Список треков пуст — нечего добавлять.")

    # Явные обложки альбомов из секции "albums" (необязательно)
    album_covers_override: dict[str, str] = {}  # album_title -> cover filename
    for al in data.get("albums", []):
        if al.get("title") and al.get("cover"):
            album_covers_override[al["title"]] = al["cover"]

    if not _MUTAGEN_AVAILABLE:
        print("[!] ВНИМАНИЕ: mutagen не установлен → длительность треков будет 0.")
        print("    Установите: pip install mutagen   и перезапустите скрипт.\n")

    if dry_run:
        print("=== РЕЖИМ DRY-RUN: никаких изменений не производится ===\n")

    # ── Подключаемся к БД ─────────────────────────────────────────────────────
    if not dry_run:
        try:
            conn = psycopg2.connect(
                host=args.db_host, port=args.db_port,
                dbname=args.db_name, user=args.db_user, password=args.db_password,
            )
            conn.autocommit = False
            cur = conn.cursor(cursor_factory=RealDictCursor)
            print(f"Подключено к {args.db_host}:{args.db_port}/{args.db_name}\n")
        except Exception as e:
            print(f"[ОШИБКА] Не удалось подключиться к базе данных: {e}")
            sys.exit(1)
    else:
        conn = cur = None

    try:
        # ── Проверка что таблицы созданы (EF Core должен это сделать) ───────
        if not dry_run:
            ensure_schema_ready(cur)

        # ── Режим обновления длительностей ───────────────────────────────────
        if args.refresh_durations:
            if dry_run:
                print("[ОШИБКА] --refresh-durations несовместим с --dry-run")
                sys.exit(1)
            refresh_durations_mode(cur, app_dir)
            conn.commit()
            return

        # ── Роли ─────────────────────────────────────────────────────────────
        print("=== Проверяем роли ===")
        if not dry_run:
            ensure_roles(cur)
        else:
            print("  [DRY] Проверка/создание ролей (Admin, Artist, Listener)")

        # ── Собираем исполнителей из JSON + DEFAULT_ARTISTS ──────────────────
        artists_nicknames_from_json: set[str] = {
            t.get("artist", "").strip()
            for t in tracks_meta
            if t.get("artist", "").strip()
        }

        # Строим итоговый список исполнителей.
        # Для артистов НЕ из DEFAULT_ARTISTS email/пароль деривируются из никнейма.
        artists_to_create: list[dict] = list(DEFAULT_ARTISTS)
        default_nicks = {a["nickname"] for a in DEFAULT_ARTISTS}
        for nick in sorted(artists_nicknames_from_json - default_nicks):
            # Безопасный email: lowercase, пробелы → '_', кириллица транслитом не нужна
            # (PostgreSQL хранит любую строку, главное уникальность)
            nick_slug = re.sub(r"[^\w]", "_", nick.lower()).strip("_") or "artist"
            artists_to_create.append({
                "nickname": nick,
                "email":    f"{nick_slug}@rewind.local",
                "password": f"{nick_slug}_2024",
                "status":   "Верифицирован",
            })

        # ── Создаём исполнителей ─────────────────────────────────────────────
        print("\n=== Создаём исполнителей ===")
        artists_map: dict[str, int] = {}  # nickname -> UserId
        for a in artists_to_create:
            if dry_run:
                print(f"  [DRY] Исполнитель: {a['nickname']} <{a['email']}> пароль={a['password']}")
                artists_map[a["nickname"]] = -1
            else:
                uid = ensure_user(cur, a["nickname"], a["email"], a["password"], ROLE_ARTIST, a["status"])
                artists_map[a["nickname"]] = uid
                print(f"  Исполнитель: {a['nickname']} (UserId={uid})")

        # ── Создаём слушателей ───────────────────────────────────────────────
        print("\n=== Создаём слушателей ===")
        listener_ids: list[int] = []
        for l in LISTENERS:
            if dry_run:
                print(f"  [DRY] Слушатель: {l['nickname']} <{l['email']}> пароль={l['password']}")
                listener_ids.append(-1)
            else:
                uid = ensure_user(cur, l["nickname"], l["email"], l["password"], ROLE_LISTENER, l["status"])
                listener_ids.append(uid)
                print(f"  Слушатель: {l['nickname']} (UserId={uid})")

        # ── Копируем и регистрируем треки ────────────────────────────────────
        print("\n=== Загружаем треки ===")
        music_lib_dir = app_dir / "MusicLibrary"

        all_track_ids: list[int] = []
        # album_name -> {artist_nick, genre, cover_db, tracks: [id, ...]}
        albums_data: dict[str, dict] = {}
        # artist_nick -> [{"track_id", "genre", "cover_db"}, ...]  (треки без альбома)
        unassigned_tracks: dict[str, list] = {}
        # track_id -> cover_path_db  (для назначения обложек плейлистам)
        track_cover_map: dict[int, str] = {}

        for t in tracks_meta:
            title       = t.get("title", "Без названия").strip()
            file_name   = t.get("file", "").strip()
            cover_name  = t.get("cover", "").strip()
            artist_nick = t.get("artist", "").strip()
            genre       = t.get("genre", "").strip() or "Разное"   # дефолт если не задан
            album_name  = t.get("album", "").strip()

            artist_id = artists_map.get(artist_nick)
            if artist_id is None:
                print(f"  [ПРОПУСК] Неизвестный исполнитель '{artist_nick}' для трека '{title}'")
                continue

            if not file_name:
                print(f"  [ПРОПУСК] Не указан файл для трека '{title}'")
                continue

            audio_src = music_dir / file_name
            if not audio_src.exists():
                print(f"  [ПРОПУСК] Аудиофайл не найден: {audio_src}")
                continue

            # --- Копируем аудио ---
            if dry_run:
                audio_db = sanitize_filename(title) + audio_src.suffix
                duration = get_audio_duration(audio_src)
            else:
                audio_db = copy_to_music_library(audio_src, title, music_lib_dir)
                duration = get_audio_duration(audio_src)

            # --- Копируем обложку трека ---
            cover_path_db: str | None = None
            if cover_name:
                cover_src = music_dir / cover_name
                if cover_src.exists():
                    if dry_run:
                        cover_path_db = f"Images/TrackCovers/{cover_src.name}"
                    else:
                        cover_path_db = copy_to_image_folder(cover_src, "TrackCovers", app_dir)
                else:
                    print(f"  [ПРЕДУПРЕЖДЕНИЕ] Обложка не найдена: {cover_src}")

            # --- Вставляем трек в БД ---
            if dry_run:
                track_id = -len(all_track_ids) - 1
                print(
                    f"  [DRY] Трек: '{title}' | файл={audio_db} "
                    f"| исполнитель={artist_nick} | жанр={genre} | длит={duration}s "
                    f"| альбом={album_name or '—'}"
                )
            else:
                track_id = ensure_track(cur, title, audio_db, cover_path_db, duration, artist_id, genre)
                print(
                    f"  Трек: '{title}' (TrackID={track_id}"
                    f", исполнитель={artist_nick}, длит={duration}s)"
                )

            all_track_ids.append(track_id)
            if cover_path_db:
                track_cover_map[track_id] = cover_path_db

            if album_name:
                if album_name not in albums_data:
                    albums_data[album_name] = {
                        "artist_nick": artist_nick,
                        "genre":       genre,
                        "cover_db":    cover_path_db,  # обложка первого трека как fallback
                        "tracks":      [],
                    }
                albums_data[album_name]["tracks"].append(track_id)
            else:
                # Трек без альбома — запоминаем для возможного авто-альбома
                unassigned_tracks.setdefault(artist_nick, []).append({
                    "track_id": track_id,
                    "genre":    genre,
                    "cover_db": cover_path_db,
                })

        # ── Создаём альбомы ──────────────────────────────────────────────────
        if albums_data:
            print("\n=== Создаём альбомы ===")
        for album_name, info in albums_data.items():
            artist_id = artists_map.get(info["artist_nick"])
            if artist_id is None:
                continue

            # Определяем обложку альбома: сначала явный override из секции "albums",
            # затем используем обложку первого трека альбома.
            album_cover_db: str | None = info["cover_db"]

            override_cover_name = album_covers_override.get(album_name)
            if override_cover_name:
                cover_src = music_dir / override_cover_name
                if cover_src.exists():
                    if dry_run:
                        album_cover_db = f"Images/AlbumCovers/{cover_src.name}"
                    else:
                        album_cover_db = copy_to_image_folder(cover_src, "AlbumCovers", app_dir)
            elif album_cover_db:
                # Копируем ту же картинку в Images/AlbumCovers
                # (у треков она в TrackCovers, альбому нужна своя копия)
                if not dry_run:
                    src_path = app_dir / album_cover_db.replace("/", os.sep)
                    if src_path.exists():
                        album_cover_db = copy_to_image_folder(src_path, "AlbumCovers", app_dir)

            if dry_run:
                print(
                    f"  [DRY] Альбом: '{album_name}' | исполнитель={info['artist_nick']} "
                    f"| жанр={info['genre']} | треков={len(info['tracks'])}"
                )
            else:
                album_id = ensure_album(cur, album_name, artist_id, album_cover_db, info["genre"])
                print(
                    f"  Альбом: '{album_name}' (AlbumId={album_id}"
                    f", треков={len(info['tracks'])})"
                )
                for tid in info["tracks"]:
                    link_album_track(cur, album_id, tid)

        # ── Авто-альбомы: артисты с >1 треком без указанного альбома ────────────
        auto_album_candidates = {
            nick: entries
            for nick, entries in unassigned_tracks.items()
            if len(entries) > 1
        }
        if auto_album_candidates:
            print("\n=== Создаём авто-альбомы (треки без альбома) ===")
        for artist_nick, entries in auto_album_candidates.items():
            artist_id = artists_map.get(artist_nick)
            if artist_id is None:
                continue
            auto_album_name = f"{artist_nick} — Коллекция"
            # Жанр и обложка — берём из первого трека где они есть
            auto_genre   = next((e["genre"]    for e in entries if e["genre"]    and e["genre"] != "Разное"), None) \
                        or next((e["genre"]    for e in entries if e["genre"]),    "Разное")
            auto_cover   = next((e["cover_db"] for e in entries if e["cover_db"]), None)
            # Если обложка из TrackCovers — скопируем в AlbumCovers
            if auto_cover and not dry_run:
                src_path = app_dir / auto_cover.replace("/", os.sep)
                if src_path.exists():
                    auto_cover = copy_to_image_folder(src_path, "AlbumCovers", app_dir)
            if dry_run:
                print(
                    f"  [DRY] Авто-альбом: '{auto_album_name}' "
                    f"| жанр={auto_genre} | треков={len(entries)}"
                )
            else:
                album_id = ensure_album(cur, auto_album_name, artist_id, auto_cover, auto_genre)
                for entry in entries:
                    link_album_track(cur, album_id, entry["track_id"])
                print(
                    f"  Авто-альбом: '{auto_album_name}' "
                    f"(AlbumId={album_id}, треков={len(entries)})"
                )

        # ── Создаём плейлисты для слушателей ─────────────────────────────────
        if listener_ids and all_track_ids:
            print("\n=== Создаём плейлисты для слушателей ===")
        for i, (uid, l) in enumerate(zip(listener_ids, LISTENERS)):
            for j in range(PLAYLISTS_PER_USER):
                tpl_idx  = (i * PLAYLISTS_PER_USER + j) % len(PLAYLIST_TEMPLATES)
                pl_title = PLAYLIST_TEMPLATES[tpl_idx][0]
                is_priv  = PLAYLIST_TEMPLATES[tpl_idx][1]

                # Добавляем nickname слушателя чтобы названия были уникальными
                pl_full_title = f"{pl_title} — {l['nickname']}"

                n_tracks      = min(TRACKS_PER_PLAYLIST, len(all_track_ids))
                chosen_tracks = random.sample(all_track_ids, n_tracks)

                # Обложка плейлиста = обложка первого трека из выборки у которого она есть
                pl_cover = next(
                    (track_cover_map[tid] for tid in chosen_tracks if tid in track_cover_map),
                    None,
                )

                if dry_run:
                    print(
                        f"  [DRY] Плейлист: '{pl_full_title}' "
                        f"| приватный={is_priv} | треков={n_tracks} | обложка={'да' if pl_cover else 'нет'}"
                    )
                else:
                    pl_id = ensure_playlist(cur, pl_full_title, uid, is_priv, pl_cover)
                    for tid in chosen_tracks:
                        link_playlist_track(cur, pl_id, tid)
                    print(
                        f"  Плейлист: '{pl_full_title}' "
                        f"(PlaylistID={pl_id}, треков={n_tracks})"
                    )

        # ── Фиксируем транзакцию ─────────────────────────────────────────────
        if not dry_run:
            conn.commit()
            print("\n[OK] Транзакция зафиксирована. База данных успешно наполнена!")
        else:
            print("\n[OK] Dry-run завершён. Никаких изменений не сделано.")

    except Exception as e:
        if not dry_run and conn:
            conn.rollback()
        print(f"\n[ОШИБКА] {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
    finally:
        if not dry_run and conn:
            cur.close()
            conn.close()


if __name__ == "__main__":
    main()
