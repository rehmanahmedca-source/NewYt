# YT Downloader X Pro

A self-hosted YouTube download manager (IDM-style) built with Flask + yt-dlp.
Runs locally and is controlled from a browser -- works great inside Pydroid 3
on Android, or on a normal desktop.

## Install (Pydroid 3 -> Pip menu, or `pip` on desktop)

```
pip install -U yt-dlp
pip install Flask Flask-SQLAlchemy APScheduler
```

FFmpeg is optional but recommended (needed to merge separate video+audio
streams, embed thumbnails/metadata, and convert to mp3). Install it via
Pydroid's repo/plugin manager, or `pip install imageio-ffmpeg`.

## Run

```
python app.py
```

Then open **http://127.0.0.1:5000** in your browser (Chrome, etc. -- on the
same device). The database (`database.db`) and folders (`downloads/`,
`temp/`, `logs/`) are created automatically on first run.

## Android storage note

For downloads to land in your real Downloads / SD card folder (not just the
app sandbox), grant Pydroid 3 full storage access:
Android Settings -> Apps -> Pydroid 3 -> Permissions -> Files/Storage ->
Allow access to manage all files. Then set the download folder in
Settings inside the app to e.g. `/storage/emulated/0/Download`.

## Real-time updates

This build uses AJAX polling (the dashboard/queue refresh themselves every
~1.5s) instead of WebSockets/Flask-SocketIO. This was a deliberate choice:
Flask-SocketIO requires eventlet/gevent monkey-patching, which is unreliable
on Android Python builds. Polling is simpler, has zero extra dependencies,
and is robust everywhere -- the visual result (live progress bars, speed,
ETA) is effectively identical to a websocket-driven UI.

## Project layout

```
app.py              Flask app factory / entry point
config.py           App configuration
extensions.py       Shared SQLAlchemy instance (avoids circular imports)
models/             Download (task), History, Settings ORM models
services/           Queue manager, yt-dlp wrapper, progress tracking, etc.
utils/              Small helpers: filesize formatting, validators, logger
routes/             Flask blueprints: page routes + JSON API routes
templates/          Jinja2 HTML (Bootstrap 5, dark neon theme)
static/             CSS / JS / images
```
