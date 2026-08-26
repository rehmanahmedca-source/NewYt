# YT Downloader X Pro - ASP.NET MVC Edition

A self-hosted YouTube download manager built with **ASP.NET MVC (.NET Framework 4.8) + SQLite + yt-dlp**.

## Prerequisites
- .NET Framework 4.8 SDK
- IIS or IIS Express
- yt-dlp installed and available in PATH
- ffmpeg (optional, for muxing)

## Setup
Open the project in Visual Studio and run with IIS Express, or:
```bash
msbuild YTDownloaderXPro.csproj
```

## Features
- Dashboard with statistics and charts
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry/priority
- Download history with CSV export
- Configurable settings
- Direct browser downloads via CDN proxy
