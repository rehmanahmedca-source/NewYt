# YT Downloader X Pro - PHP Edition

A self-hosted YouTube download manager built with **PHP + SQLite + yt-dlp**.

## Prerequisites
- PHP 8.0+ with SQLite3 and PDO extensions
- yt-dlp installed and available in PATH
- ffmpeg (optional, for muxing)

> **Android users:** this PHP version calls the *external* `yt-dlp` binary via
> `shell_exec`, which is why it is painful to run in Termux (you must install
> Python + yt-dlp + PHP inside Termux and keep them on PATH). On a phone, use
> the Android APK built from the Python version instead — see
> [`../python-android/`](../python-android/README.md). yt-dlp is embedded, so
> nothing else is needed.

## Setup
```bash
php -S 0.0.0.0:5000 index.php
```

Open http://127.0.0.1:5000 in your browser.

### Termux (if you really want the PHP version on a phone)
```bash
pkg update && pkg install php php-pdo-sqlite python
pip install yt-dlp          # provides the yt-dlp binary this app shells out to
cd php && php -S 0.0.0.0:5000 index.php
```
Then open http://127.0.0.1:5000 in Chrome. If `yt-dlp` is not found, run
`pip install --force-reinstall yt-dlp` and make sure `$(python -m site --user-base)/bin`
is in your PATH.

## Features
- Dashboard with statistics and charts
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry/priority
- Download history with CSV export
- Configurable settings
- Direct browser downloads via CDN proxy
