"""Android bootstrap module -- the Chaquopy runtime calls start() from the
Java Application class before any UI appears.

Responsibilities:
  1. point all data/download paths at real Android directories
  2. import + create the Flask app (also resumes interrupted downloads)
  3. serve it on 127.0.0.1:5000 in a daemon thread (the WebView and even
     the phone's Chrome browser can then open the UI)
"""
import os
import threading
import traceback

_app = None
_server_thread = None


def start(files_dir, ext_files_dir, ffmpeg_path, package_name):
    """Called from Java: (context.getFilesDir(), getExternalFilesDir(null),
    nativeLibraryDir/libffmpeg.so, packageName)."""
    global _app, _server_thread

    os.environ["NEWYT_DATA_DIR"] = files_dir
    os.environ["NEWYT_DOWNLOAD_DIR"] = os.path.join(ext_files_dir, "downloads")
    os.environ["NEWYT_TEMP_DIR"] = os.path.join(ext_files_dir, "temp")
    os.environ["NEWYT_PACKAGE"] = package_name
    if ffmpeg_path and os.path.isfile(ffmpeg_path):
        os.environ["NEWYT_FFMPEG"] = ffmpeg_path
    os.environ["NEWYT_HOST"] = "127.0.0.1"

    from newyt.main import create_app
    from newyt.config import Config

    _app = create_app()

    def _serve():
        try:
            _app.run(host=Config.HOST, port=Config.PORT, debug=False, threaded=True)
        except Exception:
            traceback.print_exc()

    _server_thread = threading.Thread(target=_serve, name="flask", daemon=True)
    _server_thread.start()
    return True
