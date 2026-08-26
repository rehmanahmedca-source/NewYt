# YT Downloader X Pro - Node.js Edition

A self-hosted YouTube download manager built with **Node.js + Express + yt-dlp**.

## Prerequisites
- Node.js 18+
- yt-dlp installed and available in PATH
- ffmpeg (optional, for muxing)

## Setup
```bash
npm install
npm start
```

Open http://127.0.0.1:5000 in your browser.

## Features
- Dashboard with statistics and charts
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry/priority
- Download history with CSV export
- Configurable settings (download folder, concurrency, proxy, etc.)
- Direct browser downloads via CDN proxy
