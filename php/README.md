# YT Downloader X Pro - PHP Edition

A self-hosted YouTube download manager built with **PHP + SQLite + yt-dlp**.

## Prerequisites
- PHP 8.0+ with SQLite3 and PDO extensions
- yt-dlp installed and available in PATH
- ffmpeg (optional, for muxing)

## Setup
```bash
php -S 0.0.0.0:5000 index.php
```

Open http://127.0.0.1:5000 in your browser.

## Features
- Dashboard with statistics and charts
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry/priority
- Download history with CSV export
- Configurable settings
- Direct browser downloads via CDN proxy
