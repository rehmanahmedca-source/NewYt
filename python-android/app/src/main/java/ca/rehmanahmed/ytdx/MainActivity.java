package ca.rehmanahmed.ytdx;

import android.Manifest;
import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.view.View;
import android.webkit.DownloadListener;
import android.webkit.URLUtil;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import java.io.HttpURLConnection;
import java.net.URL;
import java.util.ArrayList;
import java.util.List;

/**
 * Full-screen WebView over the embedded Flask server (127.0.0.1:5000).
 * Shows a splash until the Python engine answers, then loads the UI.
 * File downloads are handed to the system browser / download manager.
 */
public class MainActivity extends Activity {

    private static final String SERVER = App.serverUrl();
    private static final int PERM_REQ = 41;

    private WebView webView;
    private LinearLayout splash;
    private TextView splashText;
    private volatile boolean loaded = false;

    @SuppressLint("SetJavaScriptEnabled")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Pre-Android-10: ask for legacy storage permission so Exporter can
        // place files in the public Download folder via plain File APIs.
        if (Build.VERSION.SDK_INT < 29) {
            requestLegacyStorage();
        }

        setContentView(R.layout.activity_main);
        webView = findViewById(R.id.webview);
        splash = findViewById(R.id.splash);
        splashText = findViewById(R.id.splash_text);

        WebSettings ws = webView.getSettings();
        ws.setJavaScriptEnabled(true);
        ws.setDomStorageEnabled(true);
        ws.setMediaPlaybackRequiresUserGesture(false);
        ws.setAllowFileAccess(false);
        ws.setAllowContentAccess(false);
        ws.setCacheMode(WebSettings.LOAD_DEFAULT);

        webView.setWebViewClient(new WebViewClient() {
            @Override
            public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                Uri uri = request.getUrl();
                String host = uri.getHost() == null ? "" : uri.getHost();
                // Keep our own server inside the app; open everything else
                // (YouTube links etc.) in the default browser.
                if (host.equals("127.0.0.1") || host.equals("localhost")) {
                    return false;
                }
                try {
                    startActivity(new Intent(Intent.ACTION_VIEW, uri));
                } catch (Exception ignored) {
                }
                return true;
            }
        });

        webView.setWebChromeClient(new WebChromeClient());

        // The server streams finished files with Content-Disposition; let the
        // system handle the download (ends up in the phone's Download folder).
        webView.setDownloadListener((url, userAgent, contentDisposition, mimeType, size) -> {
            try {
                Intent i = new Intent(Intent.ACTION_VIEW, Uri.parse(url));
                i.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
                startActivity(i);
            } catch (Exception e) {
                Toast.makeText(this, "Cannot open download handler", Toast.LENGTH_SHORT).show();
            }
        });

        if (savedInstanceState != null) {
            webView.restoreState(savedInstanceState);
            loaded = true;
            splash.setVisibility(View.GONE);
        }
        waitForServerThenLoad();
    }

    private void waitForServerThenLoad() {
        Thread t = new Thread(() -> {
            long deadline = System.currentTimeMillis() + 60_000;
            boolean up = false;
            while (System.currentTimeMillis() < deadline) {
                if (isServerUp()) {
                    up = true;
                    break;
                }
                try {
                    Thread.sleep(250);
                } catch (InterruptedException ignored) {
                }
            }
            final boolean ok = up;
            runOnUiThread(() -> {
                if (isFinishing() || loaded) {
                    return;
                }
                if (ok) {
                    splashText.setText(R.string.loading_ui);
                    webView.loadUrl(SERVER);
                } else {
                    splashText.setText(R.string.server_failed);
                    Toast.makeText(this, R.string.server_failed, Toast.LENGTH_LONG).show();
                }
            });
        }, "server-wait");
        t.setDaemon(true);
        t.start();
    }

    private boolean isServerUp() {
        try {
            HttpURLConnection c = (HttpURLConnection) new URL(SERVER + "/api/stats").openConnection();
            c.setConnectTimeout(1500);
            c.setReadTimeout(1500);
            int code = c.getResponseCode();
            c.disconnect();
            return code < 500;
        } catch (Exception e) {
            return false;
        }
    }

    private void requestLegacyStorage() {
        if (checkSelfPermission(Manifest.permission.WRITE_EXTERNAL_STORAGE)
                != PackageManager.PERMISSION_GRANTED) {
            requestPermissions(new String[]{Manifest.permission.WRITE_EXTERNAL_STORAGE}, PERM_REQ);
        }
    }

    @Override
    protected void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);
        webView.saveState(outState);
    }

    @Override
    public void onBackPressed() {
        if (webView != null && webView.canGoBack()) {
            webView.goBack();
        } else {
            super.onBackPressed();
        }
    }

    @Override
    protected void onDestroy() {
        if (webView != null) {
            webView.destroy();
        }
        super.onDestroy();
    }
}
