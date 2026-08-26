package ca.rehmanahmed.ytdx;

import android.app.Application;
import android.content.Context;

/** Small helper so Exporter can reach a Context without extra plumbing. */
public final class AppContextHolder {

    private static volatile Context appContext;

    static void init(Application app) {
        appContext = app;
    }

    static Context get() {
        return appContext;
    }

    private AppContextHolder() {
    }
}
