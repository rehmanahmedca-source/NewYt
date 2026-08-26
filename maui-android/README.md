# YT Downloader X Pro - .NET MAUI (Android)

A native Android YouTube download manager built with **.NET MAUI + SQLite + yt-dlp**.

## Prerequisites
- .NET 8 SDK with Android workload
- Android SDK (API 21+)
- yt-dlp binary available on the Android device (via Termux or similar)
- ffmpeg (optional, for muxing)

## Setup
```bash
dotnet workload install android
dotnet build -f net8.0-android
dotnet run -f net8.0-android
```

Or open in Visual Studio 2022 and deploy to an Android device/emulator.

## Features
- Native Android app with Flyout navigation
- Dashboard with statistics
- Video/playlist/channel fetching with format selection
- Download queue with pause/resume/cancel/retry
- Download history
- Configurable settings
- Saves directly to Android device storage
- Dark theme matching the web version

## Notes
- yt-dlp must be installed on the Android device (e.g., via Termux)
- The app needs "All files access" permission for saving downloads
- Downloads are saved to the app's data directory by default
