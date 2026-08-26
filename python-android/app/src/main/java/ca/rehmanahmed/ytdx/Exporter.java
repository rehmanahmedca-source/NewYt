package ca.rehmanahmed.ytdx;

import android.content.ContentValues;
import android.content.Context;
import android.net.Uri;
import android.os.Build;
import android.os.Environment;
import android.provider.MediaStore;
import android.webkit.MimeTypeMap;

import java.io.File;
import java.io.FileInputStream;
import java.io.InputStream;
import java.io.OutputStream;

/**
 * Copies a completed download from the app-private directory into the shared
 * "Download/YTDownloaderX" folder so files show up in file managers, gallery
 * apps and USB MTP. Uses MediaStore on Android 10+, plain File IO before that.
 * Called from Python (services.queue_manager) through Chaquopy.
 */
public final class Exporter {

    public static final String PUBLIC_SUBDIR = "YTDownloaderX";

    /** Returns the public destination path, or throws on failure. */
    public static String export(String srcPath) throws Exception {
        File src = new File(srcPath);
        if (!src.exists()) {
            throw new IllegalArgumentException("File not found: " + srcPath);
        }
        Context ctx = AppContextHolder.get();
        if (Build.VERSION.SDK_INT >= 29) {
            return exportMediaStore(ctx, src);
        }
        return exportLegacy(src);
    }

    private static String exportMediaStore(Context ctx, File src) throws Exception {
        String name = src.getName();
        String ext = name.contains(".") ? name.substring(name.lastIndexOf('.') + 1) : "";
        String mime = MimeTypeMap.getSingleton().getMimeTypeFromExtension(ext.toLowerCase());
        if (mime == null) {
            mime = guessMime(ext.toLowerCase());
        }

        ContentValues cv = new ContentValues();
        cv.put(MediaStore.MediaColumns.DISPLAY_NAME, name);
        cv.put(MediaStore.MediaColumns.MIME_TYPE, mime);
        cv.put(MediaStore.MediaColumns.RELATIVE_PATH,
                Environment.DIRECTORY_DOWNLOADS + "/" + PUBLIC_SUBDIR);
        cv.put(MediaStore.MediaColumns.IS_PENDING, 1);

        Uri uri = ctx.getContentResolver().insert(
                MediaStore.Downloads.EXTERNAL_CONTENT_URI, cv);
        if (uri == null) {
            throw new IllegalStateException("MediaStore insert failed");
        }
        try (InputStream in = new FileInputStream(src);
             OutputStream out = ctx.getContentResolver().openOutputStream(uri)) {
            if (out == null) {
                throw new IllegalStateException("MediaStream open failed");
            }
            copy(in, out);
        } catch (Exception e) {
            try {
                ctx.getContentResolver().delete(uri, null, null);
            } catch (Exception ignored) {
            }
            throw e;
        }
        ContentValues done = new ContentValues();
        done.put(MediaStore.MediaColumns.IS_PENDING, 0);
        ctx.getContentResolver().update(uri, done, null, null);
        return Environment.DIRECTORY_DOWNLOADS + "/" + PUBLIC_SUBDIR + "/" + name;
    }

    @SuppressWarnings({"ResultOfMethodCallIgnored", "deprecation"})
    private static String exportLegacy(File src) throws Exception {
        File dir = new File(
                Environment.getExternalStoragePublicDirectory(Environment.DIRECTORY_DOWNLOADS),
                PUBLIC_SUBDIR);
        if (!dir.exists()) {
            dir.mkdirs();
        }
        File dst = new File(dir, src.getName());
        try (InputStream in = new FileInputStream(src);
             OutputStream out = new java.io.FileOutputStream(dst)) {
            copy(in, out);
        }
        return dst.getAbsolutePath();
    }

    private static void copy(InputStream in, OutputStream out) throws Exception {
        byte[] buf = new byte[64 * 1024];
        int n;
        while ((n = in.read(buf)) > 0) {
            out.write(buf, 0, n);
        }
        out.flush();
    }

    private static String guessMime(String ext) {
        switch (ext) {
            case "mp4": case "m4v": return "video/mp4";
            case "webm": return "video/webm";
            case "mkv": return "video/x-matroska";
            case "mp3": return "audio/mpeg";
            case "m4a": return "audio/mp4";
            case "opus": case "ogg": return "audio/ogg";
            case "wav": return "audio/x-wav";
            case "jpg": case "jpeg": return "image/jpeg";
            case "png": return "image/png";
            case "webp": return "image/webp";
            default: return "application/octet-stream";
        }
    }

    private Exporter() {
    }
}
