"""JSON API -- fetch metadata, manage the download queue, history, settings,
and dashboard statistics. Polled by static/js/*.js every ~1.5s for
near-real-time updates without WebSockets."""
import os
import time
import uuid
import subprocess
import requests as upstream_requests
from flask import Blueprint, request, jsonify, Response, send_file, abort, stream_with_context, session

from newyt.extensions import db
from newyt.models.download import Download
from newyt.models.history import HistoryEntry
from newyt.services import downloader
from newyt.services.queue_manager import queue_manager
from newyt.services.settings_service import get_settings, update_settings
from newyt.services.history_service import list_history, delete_history_entry, export_history_csv
from urllib.parse import quote
from newyt.utils.validators import is_valid_youtube_url, sanitize_filename
from newyt.utils.helpers import get_disk_usage, detect_content_type
from newyt.utils.logger import get_logger

api_bp = Blueprint("api", __name__)

# In-memory cache for pre-resolved direct-download targets. The heavy part
# of a direct download is yt-dlp resolving the real CDN URL from YouTube --
# /api/direct/prepare pays that cost once and hands back a token; the
# actual GET /api/direct/download?token=... then streams immediately using
# the cached target instead of resolving a second time. Tokens expire
# quickly since CDN URLs themselves are short-lived and this is only meant
# to bridge the gap between "resolved" and "browser starts downloading".
_RESOLVE_CACHE = {}
_RESOLVE_TTL_SECONDS = 90


def _cache_prune():
    now = time.time()
    dead = [k for k, v in _RESOLVE_CACHE.items() if now - v["ts"] > _RESOLVE_TTL_SECONDS]
    for k in dead:
        _RESOLVE_CACHE.pop(k, None)


def _get_session_id():
    """Identifies 'this session' (this browser, until it's fully closed) via
    a non-permanent Flask session cookie -- no Expires/Max-Age is set, so
    the browser drops it when it closes, and the next visit gets a fresh
    one. Used to scope the queue and most dashboard stats to the current
    sitting rather than showing everything ever downloaded."""
    sid = session.get("sid")
    if not sid:
        sid = uuid.uuid4().hex[:16]
        session["sid"] = sid
        session.permanent = False
    return sid


# --------------------------------------------------------------- fetch
@api_bp.route("/fetch", methods=["POST"])
def fetch():
    url = (request.json or {}).get("url", "").strip()
    if not is_valid_youtube_url(url):
        return jsonify({"error": "Please paste a valid YouTube video, playlist, or channel link."}), 400
    try:
        overview = downloader.fetch_overview(url)
        overview["content_type_guess"] = detect_content_type(url)
        return jsonify(overview)
    except Exception as ex:
        get_logger().warning(f"Fetch failed for {url}: {ex}")
        return jsonify({"error": str(ex)}), 502


@api_bp.route("/fetch_formats", methods=["POST"])
def fetch_formats():
    url = (request.json or {}).get("url", "").strip()
    if not is_valid_youtube_url(url):
        return jsonify({"error": "Invalid URL."}), 400
    try:
        return jsonify(downloader.fetch_formats(url))
    except Exception as ex:
        return jsonify({"error": str(ex)}), 502


# --------------------------------------------------------------- queue
@api_bp.route("/queue/add", methods=["POST"])
def queue_add():
    body = request.json or {}
    items = body.get("items")
    if not items:
        items = [body]

    sid = _get_session_id()
    added = []
    for item in items:
        url = (item.get("url") or "").strip()
        if not is_valid_youtube_url(url):
            continue
        task = queue_manager.add_task(
            url=url,
            format_id=item.get("format_id", "bestvideo+bestaudio/best"),
            quality_label=item.get("quality_label", "Best"),
            title=item.get("title", "Untitled"),
            thumbnail=item.get("thumbnail", ""),
            uploader=item.get("uploader", ""),
            session_id=sid,
        )
        added.append(task.id)
    return jsonify({"added": added, "count": len(added)})


@api_bp.route("/tasks", methods=["GET"])
def tasks():
    sid = _get_session_id()
    rows = (
        Download.query.filter_by(session_id=sid)
        .order_by(Download.priority.asc(), Download.created_time.asc())
        .all()
    )
    return jsonify([r.to_dict() for r in rows])


@api_bp.route("/tasks/<task_id>/<action>", methods=["POST"])
def task_action(task_id, action):
    sid = _get_session_id()
    owned = Download.query.filter_by(id=task_id, session_id=sid).first()
    if not owned:
        return jsonify({"error": "Not found"}), 404

    ok = False
    if action == "pause":
        ok = queue_manager.pause(task_id)
    elif action == "resume":
        ok = queue_manager.resume(task_id)
    elif action == "cancel":
        ok = queue_manager.cancel(task_id)
    elif action == "retry":
        ok = queue_manager.retry(task_id)
    elif action == "remove":
        ok = queue_manager.remove(task_id)
    elif action == "priority_up":
        ok = queue_manager.reprioritize(task_id, "up")
    elif action == "priority_down":
        ok = queue_manager.reprioritize(task_id, "down")
    else:
        return jsonify({"error": "Unknown action"}), 400
    return jsonify({"ok": ok})


@api_bp.route("/tasks/<task_id>/file", methods=["GET"])
def task_file(task_id):
    """Streams the finished file from the server straight to the browser
    (as_attachment triggers the native 'Save file' download), so the file
    lands on the user's device instead of just sitting on the server.
    conditional=True enables Range requests, which lets the browser resume
    interrupted downloads and pull large video files faster."""
    task = Download.query.get(task_id)
    if not task or task.status != "completed" or not task.output_file:
        abort(404)
    if not os.path.isfile(task.output_file):
        abort(404)
    return send_file(
        task.output_file,
        as_attachment=True,
        download_name=os.path.basename(task.output_file),
        conditional=True,
        max_age=0,
    )


@api_bp.route("/direct/prepare", methods=["POST"])
def direct_prepare():
    """Does the slow part (yt-dlp resolving the real CDN link) up front and
    caches the result. Returns quickly once resolved so the front-end can
    swap its 'resolving...' spinner for a ready state, then hand off to
    the browser's native downloader via /direct/download?token=... with
    no further resolve delay."""
    body = request.json or {}
    url = (body.get("url") or "").strip()
    format_id = (body.get("format_id") or "").strip()
    title = body.get("title") or "video"
    if not is_valid_youtube_url(url) or not format_id:
        return jsonify({"error": "Invalid URL or format."}), 400

    try:
        target = downloader.resolve_direct(url, format_id)
    except Exception as ex:
        get_logger().warning(f"Prepare resolve failed for {url} ({format_id}): {ex}")
        return jsonify({"error": str(ex)}), 502

    _cache_prune()
    token = uuid.uuid4().hex
    _RESOLVE_CACHE[token] = {
        "target": target,
        "safe_title": sanitize_filename(title),
        "ts": time.time(),
    }
    return jsonify({"token": token})


@api_bp.route("/direct/download", methods=["GET"])
def direct_download():
    """Streams straight from YouTube's CDN into the browser -- nothing is
    ever written to disk on this server.

    Single-stream formats (progressive video, audio-only) are proxied with
    Range headers forwarded both ways, so the browser's own download
    manager gets real pause/resume. Formats that need a separate video +
    audio stream are muxed live through ffmpeg and piped straight into the
    response -- if that one is interrupted it has to restart from the top,
    since nothing is saved on either end to resume from.

    If a ?token= from /direct/prepare is present and still valid, the
    already-resolved target is reused so this request starts streaming
    immediately with no yt-dlp resolve delay. Otherwise it resolves fresh
    from url/format_id, same as before (kept for backward compatibility).
    """
    token = (request.args.get("token") or "").strip()
    cached = _RESOLVE_CACHE.pop(token, None) if token else None

    if cached and time.time() - cached["ts"] <= _RESOLVE_TTL_SECONDS:
        target = cached["target"]
        safe_title = cached["safe_title"]
    else:
        url = (request.args.get("url") or "").strip()
        format_id = (request.args.get("format_id") or "").strip()
        title = request.args.get("title") or "video"
        if not is_valid_youtube_url(url) or not format_id:
            abort(400)
        try:
            target = downloader.resolve_direct(url, format_id)
        except Exception as ex:
            get_logger().warning(f"Direct resolve failed for {url} ({format_id}): {ex}")
            return jsonify({"error": str(ex)}), 502
        safe_title = sanitize_filename(title)

    if target["mode"] == "proxy":
        return _stream_proxy(target, safe_title)
    return _stream_muxed(target, safe_title)


def _content_disposition(safe_title, ext):
    """Builds a Content-Disposition header that survives non-ASCII video
    titles. HTTP headers must be Latin-1; a raw unicode title in a plain
    filename="..." breaks header encoding and browsers then fall back to
    guessing a name from the URL/host -- which is why downloads were
    showing the server address instead of the video title. RFC 5987's
    filename*=UTF-8''... plus an ASCII-safe filename= fallback fixes it
    for every browser.
    """
    fname = f"{safe_title}.{ext}"
    ascii_fallback = fname.encode("ascii", "ignore").decode("ascii").strip() or f"video.{ext}"
    utf8_quoted = quote(fname)
    return f"attachment; filename=\"{ascii_fallback}\"; filename*=UTF-8''{utf8_quoted}"


def _stream_proxy(target, safe_title):
    upstream_headers = dict(target.get("headers") or {})
    range_header = request.headers.get("Range")
    if range_header:
        upstream_headers["Range"] = range_header

    upstream = upstream_requests.get(
        target["url"], headers=upstream_headers, stream=True, timeout=30
    )

    def generate():
        try:
            for chunk in upstream.iter_content(chunk_size=256 * 1024):
                if chunk:
                    yield chunk
        finally:
            upstream.close()

    ext = target.get("ext") or "mp4"
    resp_headers = {
        "Content-Disposition": _content_disposition(safe_title, ext),
        "Accept-Ranges": "bytes",
    }
    for h in ("Content-Length", "Content-Range", "Content-Type"):
        if h in upstream.headers:
            resp_headers[h] = upstream.headers[h]
    resp_headers.setdefault("Content-Type", "audio/mp4" if ext == "m4a" else "video/mp4")

    status = upstream.status_code if upstream.status_code in (200, 206) else 200
    return Response(stream_with_context(generate()), status=status, headers=resp_headers)


def _stream_muxed(target, safe_title):
    settings = get_settings()
    from newyt.config import ffmpeg_path as _ffp
    ffmpeg_bin = settings.ffmpeg_path or _ffp() or "ffmpeg"
    if not os.path.isfile(ffmpeg_bin) and "/" in ffmpeg_bin:
        return {"error": "ffmpeg not available for live muxing on this device"}

    def headers_arg(headers):
        if not headers:
            return []
        header_str = "".join(f"{k}: {v}\r\n" for k, v in headers.items())
        return ["-headers", header_str]

    cmd = [
        ffmpeg_bin, "-y",
        *headers_arg(target.get("video_headers")), "-i", target["video_url"],
        *headers_arg(target.get("audio_headers")), "-i", target["audio_url"],
        "-c", "copy", "-movflags", "frag_keyframe+empty_moov+default_base_moof",
        "-f", "mp4", "pipe:1",
    ]
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)

    def generate():
        try:
            while True:
                chunk = proc.stdout.read(256 * 1024)
                if not chunk:
                    break
                yield chunk
        finally:
            try:
                proc.stdout.close()
            except Exception:
                pass
            proc.terminate()

    headers = {
        "Content-Disposition": _content_disposition(safe_title, "mp4"),
        "Content-Type": "video/mp4",
        "Accept-Ranges": "none",  # live-muxed: can't be byte-resumed
    }
    return Response(stream_with_context(generate()), headers=headers)


# --------------------------------------------------------------- history
@api_bp.route("/history", methods=["GET"])
def history_list():
    search = request.args.get("search", "")
    entries = list_history(search)
    if request.args.get("scope") == "session":
        sid = _get_session_id()
        entries = [e for e in entries if e.session_id == sid]
    return jsonify([e.to_dict() for e in entries])


@api_bp.route("/history/<int:entry_id>", methods=["DELETE"])
def history_delete(entry_id):
    ok = delete_history_entry(entry_id)
    return jsonify({"ok": ok})


@api_bp.route("/history/<int:entry_id>/redownload", methods=["POST"])
def history_redownload(entry_id):
    entry = HistoryEntry.query.get(entry_id)
    if not entry:
        return jsonify({"error": "Not found"}), 404
    # We don't store the original URL in history (only output path/metadata),
    # so the client is expected to pass it back if available.
    url = (request.json or {}).get("url", "")
    if not is_valid_youtube_url(url):
        return jsonify({"error": "Original URL required to redownload."}), 400
    task = queue_manager.add_task(
        url=url, format_id=entry.format_id or "bestvideo+bestaudio/best",
        quality_label=entry.quality_label or "Best",
        title=entry.title, thumbnail=entry.thumbnail, uploader=entry.uploader,
        session_id=_get_session_id(),
    )
    return jsonify({"ok": True, "task_id": task.id})


@api_bp.route("/history/export.csv", methods=["GET"])
def history_export():
    csv_text = export_history_csv()
    return Response(
        csv_text, mimetype="text/csv",
        headers={"Content-Disposition": "attachment; filename=history.csv"},
    )


# --------------------------------------------------------------- settings
@api_bp.route("/settings", methods=["GET"])
def settings_get():
    return jsonify(get_settings().to_dict())


@api_bp.route("/settings", methods=["POST"])
def settings_post():
    row = update_settings(request.json or {})
    return jsonify(row.to_dict())


# --------------------------------------------------------------- stats
@api_bp.route("/stats", methods=["GET"])
def stats():
    sid = _get_session_id()

    # ---- this session (resets to empty next time the browser is closed) ----
    session_q = Download.query.filter_by(session_id=sid)
    completed = session_q.filter_by(status="completed").count()
    failed = session_q.filter_by(status="failed").count()
    active = session_q.filter_by(status="downloading").count()
    queued = session_q.filter_by(status="queued").count()

    downloaded_session_bytes = (
        db.session.query(db.func.coalesce(db.func.sum(Download.total_bytes), 0))
        .filter(Download.session_id == sid, Download.status == "completed")
        .scalar()
    )
    session_attempts = completed + failed
    session_success_rate = (completed / session_attempts * 100) if session_attempts else 0

    top_uploader_row = (
        db.session.query(Download.uploader, db.func.count(Download.id).label("c"))
        .filter(Download.session_id == sid, Download.status == "completed", Download.uploader != "")
        .group_by(Download.uploader).order_by(db.desc("c")).first()
    )
    top_format_row = (
        db.session.query(Download.quality_label, db.func.count(Download.id).label("c"))
        .filter(Download.session_id == sid, Download.status == "completed", Download.quality_label != "")
        .group_by(Download.quality_label).order_by(db.desc("c")).first()
    )

    # ---- lifetime, all sessions combined -- never resets ----
    total_downloads_alltime = HistoryEntry.query.count()
    total_gb_downloaded = (
        db.session.query(db.func.coalesce(db.func.sum(HistoryEntry.size_bytes), 0)).scalar()
    ) / (1024 ** 3)

    now = time.time()
    days_labels, days_counts = [], []
    for i in range(6, -1, -1):
        day_start = now - i * 86400
        day_end = day_start + 86400
        count = HistoryEntry.query.filter(
            HistoryEntry.date_completed >= day_start, HistoryEntry.date_completed < day_end
        ).count()
        days_labels.append(time.strftime("%a", time.localtime(day_start)))
        days_counts.append(count)

    settings = get_settings()
    used, disk_total, free = get_disk_usage(settings.download_folder)

    return jsonify({
        "total_downloads": total_downloads_alltime,  # lifetime, all sessions
        "completed_downloads": completed,            # this session
        "failed_downloads": failed,                  # this session
        "active_downloads": active,                  # this session
        "queued_downloads": queued,                  # this session
        "success_rate": round(session_success_rate, 1),
        "downloaded_session_bytes": int(downloaded_session_bytes or 0),
        "total_gb_downloaded": round(total_gb_downloaded, 2),  # lifetime
        "disk_used_bytes": used,
        "disk_total_bytes": disk_total,
        "disk_free_bytes": free,
        "chart_days_labels": days_labels,
        "chart_days_counts": days_counts,
        "top_uploader": top_uploader_row[0] if top_uploader_row else "-",
        "top_format": top_format_row[0] if top_format_row else "-",
    })
