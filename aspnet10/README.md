# YT Downloader X Pro - .NET 10 / ASP.NET Edition

A self-hosted YouTube download manager built with **.NET 10 + ASP.NET + SQLite + yt-dlp**.

## Prerequisites
- .NET 10 SDK (Preview or later)
- yt-dlp installed and available in PATH
- ffmpeg (optional, for muxing)

## Setup
```bash
dotnet run
```

Open http://127.0.0.1:5000 in your browser.

## Features
- Dashboard with statistics and charts
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry/priority
- Download history with CSV export
- Configurable settings
- Direct browser downloads via CDN proxy
