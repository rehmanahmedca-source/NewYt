# YT Downloader X Pro — Multi-Framework Edition

A self-hosted YouTube download manager available in **7 different framework implementations**, all with identical functionality and UI.

## 🗂️ Framework Folders

| Folder | Framework | Runtime | Description |
|---|---|---|---|
| `/` (root) | **Python / Flask** | Python 3.8+ | Original implementation |
| `nodejs/` | **Node.js / Express** | Node.js 18+ | JavaScript backend with EJS templates |
| `php/` | **PHP** | PHP 8.0+ | Single-file router, PDO + SQLite |
| `aspnet-core/` | **ASP.NET Core** | .NET 8.0 | Modern MVC with Razor views |
| `aspnet10/` | **.NET 10 / ASP.NET** | .NET 10 | Latest .NET preview |
| `aspnet-mvc/` | **ASP.NET MVC** | .NET Framework 4.8 | Classic MVC with Web.config |
| `classic-asp/` | **Classic ASP** | IIS + VBScript | Legacy ASP with ADO + SQLite ODBC |
| `maui-android/` | **.NET MAUI** | .NET 8 + Android | Native Android app (Mono) |

## ✨ Features (All Versions)

- **Dashboard** — Statistics, charts (7-day downloads, disk usage)
- **Downloads & Queue** — Fetch videos/playlists/channels, select formats, manage queue
- **Queue Management** — Pause, resume, cancel, retry, re-priority downloads
- **History** — Permanent record of completed downloads, CSV export
- **Settings** — Download folder, concurrency, proxy, cookies, ffmpeg, metadata embedding
- **Direct Downloads** — Stream from CDN directly to browser (no server disk write)
- **Dark Neon Theme** — Glass-morphism UI with Bootstrap 5

## 🔧 Common Prerequisites

All versions require:
- **yt-dlp** installed and available in system PATH
- **ffmpeg** (optional, for video+audio muxing)

## 🚀 Quick Start

### Python (Original)
```bash
pip install -r requirements.txt
python app.py
```

### Node.js
```bash
cd nodejs && npm install && npm start
```

### PHP
```bash
cd php && php -S 0.0.0.0:5000 index.php
```

### ASP.NET Core
```bash
cd aspnet-core && dotnet run
```

### .NET 10
```bash
cd aspnet10 && dotnet run
```

### Classic ASP
Deploy to IIS with Classic ASP enabled + SQLite ODBC driver installed.

### .NET MAUI (Android)
```bash
cd maui-android && dotnet build -f net8.0-android
```

## 📁 Project Structure (per framework)

```
├── Models/         # Database models (Download, History, Settings)
├── Services/       # Business logic (Downloader, QueueManager, History, Settings)
├── Routes/         # API + page routes (Controllers, endpoints)
├── Views/          # HTML templates (EJS, Razor, ASP, PHP includes)
├── Static/wwwroot/ # CSS + JavaScript (shared across all web frameworks)
├── Utils/          # Validators, helpers, logger
└── Config          # Application configuration
```

## 🌐 API Endpoints (Consistent Across All)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/fetch` | Fetch video/playlist metadata |
| POST | `/api/fetch_formats` | Get available formats for a video |
| POST | `/api/queue/add` | Add items to download queue |
| GET | `/api/tasks` | List current session's tasks |
| POST | `/api/tasks/{id}/{action}` | Pause/resume/cancel/retry/remove |
| GET | `/api/tasks/{id}/file` | Download completed file |
| POST | `/api/direct/prepare` | Resolve CDN URL for direct download |
| GET | `/api/direct/download` | Stream file from CDN to browser |
| GET | `/api/history` | List download history |
| DELETE | `/api/history/{id}` | Delete history entry |
| GET | `/api/history/export.csv` | Export history as CSV |
| GET/POST | `/api/settings` | Get/update settings |
| GET | `/api/stats` | Dashboard statistics |

## 📝 License

MIT License — free to use and modify.
