"""Path configuration.

On desktop this behaves like the original repo layout (everything next to
the package). Inside the Android APK, java passes real device paths via
environment variables before this module is imported:

    NEWYT_DATA_DIR       database / logs (app-private, survives reinstalls)
    NEWYT_DOWNLOAD_DIR   finished downloads (app external files dir)
    NEWYT_TEMP_DIR       .part files
    NEWYT_FFMPEG         absolute path to a working ffmpeg executable
"""
import os

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

# Directory for mutable data. On Android this is the app's private files dir.
_DATA_DIR = os.environ.get("NEWYT_DATA_DIR", BASE_DIR)


class Config:
    SECRET_KEY = "change-this-secret-in-production"
    SQLALCHEMY_DATABASE_URI = os.environ.get(
        "NEWYT_DB_URI", f"sqlite:///{os.path.join(_DATA_DIR, 'database.db')}"
    )
    SQLALCHEMY_TRACK_MODIFICATIONS = False

    DEFAULT_DOWNLOAD_FOLDER = os.environ.get(
        "NEWYT_DOWNLOAD_DIR", os.path.join(_DATA_DIR, "downloads")
    )
    DEFAULT_TEMP_FOLDER = os.environ.get(
        "NEWYT_TEMP_DIR", os.path.join(_DATA_DIR, "temp")
    )
    LOG_DIR = os.environ.get("NEWYT_LOG_DIR", os.path.join(_DATA_DIR, "logs"))

    DEFAULT_MAX_CONCURRENT = 3
    DEFAULT_CONCURRENT_FRAGMENTS = 8
    DEFAULT_MAX_RETRIES = 3

    HOST = os.environ.get("NEWYT_HOST", "0.0.0.0")
    PORT = int(os.environ.get("NEWYT_PORT", "5000"))


def ffmpeg_path():
    """Best-effort ffmpeg executable, or None.

    Order: env (Android: bundled static binary) -> system PATH.
    """
    env = os.environ.get("NEWYT_FFMPEG", "").strip()
    if env and os.path.isfile(env):
        return env
    return None
