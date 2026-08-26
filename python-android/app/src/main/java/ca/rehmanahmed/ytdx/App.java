package ca.rehmanahmed.ytdx;

import android.app.Application;
import android.content.Context;
import android.os.Build;
import java.io.File;

import com.chaquo.python.PyObject;
import com.chaquo.python.Python;
import com.chaquo.python.android.AndroidPyPlatform;

/**
 * Boots the embedded Python (Flask + yt-dlp) server exactly once per process,
 * before any Activity runs. The UI then talks to http://127.0.0.1:5000.
 */
public class App extends Application {

    private static volatile boolean started = false;

    public static String serverUrl() {
        return "http://127.0.0.1:5000";
    }

    @Override
    public void onCreate() {
        super.onCreate();
        AppContextHolder.init(this);
        if (started) {
            return;
        }
        synchronized (App.class) {
            if (started) {
                return;
            }
            Python.start(new AndroidPyPlatform(getApplicationContext()));
            try {
                File extDir = getExternalFilesDir(null);
                if (extDir == null) {
                    extDir = getFilesDir();
                }
                String ffmpeg = new File(
                        getApplicationInfo().nativeLibraryDir, "libffmpeg.so").getAbsolutePath();

                Python py = Python.getInstance();
                PyObject bootstrap = py.getModule("newyt_bootstrap");
                bootstrap.callAttr("start",
                        getFilesDir().getAbsolutePath(),
                        extDir.getAbsolutePath(),
                        ffmpeg,
                        getPackageName());
                started = true;
            } catch (Throwable t) {
                // Surface it in logcat; MainActivity will show a timeout error too.
                android.util.Log.e("NewYT", "Python bootstrap failed", t);
            }
        }
    }

    public static boolean isServerStarted() {
        return started;
    }
}
