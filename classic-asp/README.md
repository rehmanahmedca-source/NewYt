# YT Downloader X Pro - Classic ASP Edition

A self-hosted YouTube download manager built with **Classic ASP (VBScript) + SQLite ODBC + yt-dlp**.

## Prerequisites
- Windows Server with IIS and Classic ASP enabled
- SQLite3 ODBC Driver installed
- yt-dlp installed and available in PATH
- ffmpeg (optional, for muxing)

## Setup
1. Enable Classic ASP in IIS (Server Manager > Add Roles and Features > Web Server > Application Development > ASP)
2. Install SQLite3 ODBC driver
3. Copy all files to your IIS site directory
4. Configure the application pool to allow 32-bit applications if using 32-bit ODBC
5. Browse to http://localhost/

## Features
- Dashboard with statistics and charts
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry
- Download history with CSV export
- Configurable settings
