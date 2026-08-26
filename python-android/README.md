# YT Downloader X Pro — Android APK (Python/Flask inside)

This folder packages the **Python (Flask) version** of YT Downloader X Pro into a
native Android app, with no Termux or root required.

## How it works

- **Chaquopy** embeds a full Python 3.11 runtime (plus `flask`, `flask-sqlalchemy`,
  `yt-dlp`, `requests`) inside the APK.
- On launch the app starts the Flask server on `127.0.0.1:5000` inside the app
  process and shows the UI in a full-screen WebView. It also resumes any
  downloads that were interrupted when the app closed.
- A **static ffmpeg** executable is bundled (as `libffmpeg.so` in `jniLibs`, fetched
  at build time) so yt-dlp can merge separate video+audio streams into 1080p+ MP4.
- Finished downloads are automatically copied to the **shared**
  `Download/YTDownloaderX` folder (via MediaStore) so they appear in file
  managers and gallery apps. The originals stay in the app's private folder.

## Build

The GitHub Actions workflow (`.github/workflows/build-apk.yml`) builds two
variants on every push that touches this folder:

| Variant | ABIs | For |
|---|---|---|
| `universal` | arm64-v8a + armeabi-v7a | Any phone, including older 32-bit devices |
| `arm64` | arm64-v8a only | Smaller APK for modern phones |

To build locally instead:

```bash
# prerequisites: JDK 17, Android SDK (platform 35, build-tools 35), Python 3.11
cd python-android
mkdir -p app/src/main/jniLibs/arm64-v8a
# put a static ffmpeg build there as libffmpeg.so (see workflow for download URL)
gradle assembleRelease
```

## Signing

Release APKs are signed with `keystore/newyt-release.p12`
(PKCS12, alias `newyt`, password `newyt1234`). Keep using this key for every
future build — Android only installs an update if it is signed with the same key.

## Notes / limitations

- Keep the app in the foreground (or at least in Recents) while downloading —
  Android may stop background processes.
- "Direct download" live-muxing streams through ffmpeg over the network, which
  the bundled static build cannot resolve on Android (no system DNS access);
  use the normal queue for those downloads instead — merging of local files
  works fine.
- The server also answers at `http://127.0.0.1:5000` from the phone's Chrome
  browser, if you prefer using it that way.
