<?php
/**
 * YT Downloader X Pro -- PHP entry point (single-file router).
 *
 * Run with:
 *     php -S 0.0.0.0:5000 index.php
 *
 * Then open http://127.0.0.1:5000 in a browser on the same device.
 */
session_start();

require_once __DIR__ . '/config/config.php';
require_once __DIR__ . '/models/database.php';
require_once __DIR__ . '/utils/validators.php';
require_once __DIR__ . '/utils/helpers.php';
require_once __DIR__ . '/utils/logger.php';
require_once __DIR__ . '/services/settingsService.php';
require_once __DIR__ . '/services/historyService.php';
require_once __DIR__ . '/services/downloader.php';
require_once __DIR__ . '/models/download.php';

// Ensure directories
foreach ([DEFAULT_DOWNLOAD_FOLDER, DEFAULT_TEMP_FOLDER, LOG_DIR] as $d) {
    if (!is_dir($d)) mkdir($d, 0755, true);
}

setup_logger(LOG_DIR);

// Initialize database
$db = Database::getInstance();
$db->initTables();
ensure_default_settings();

// Recover incomplete tasks
foreach (Download::findDownloading() as $task) {
    Download::update($task['id'], ['status' => 'queued']);
}

// Get session ID
function get_session_id() {
    if (empty($_SESSION['sid'])) {
        $_SESSION['sid'] = substr(bin2hex(random_bytes(8)), 0, 16);
    }
    return $_SESSION['sid'];
}

// Route handling
$uri = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$method = $_SERVER['REQUEST_METHOD'];

// Serve static files
if (preg_match('#\.(css|js|png|jpg|gif|ico|svg|woff2?)$#', $uri)) {
    $file = __DIR__ . '/static' . $uri;
    if (file_exists($file)) {
        $ext = pathinfo($file, PATHINFO_EXTENSION);
        $types = ['css' => 'text/css', 'js' => 'application/javascript', 'png' => 'image/png', 'jpg' => 'image/jpeg', 'svg' => 'image/svg+xml'];
        header('Content-Type: ' . ($types[$ext] ?? 'application/octet-stream'));
        readfile($file);
        exit;
    }
    http_response_code(404);
    exit;
}

// API routes
if (strpos($uri, '/api/') === 0) {
    header('Content-Type: application/json');
    $apiPath = substr($uri, 4); // remove /api

    // POST /api/fetch
    if ($apiPath === '/fetch' && $method === 'POST') {
        $body = json_decode(file_get_contents('php://input'), true) ?? [];
        $url = trim($body['url'] ?? '');
        if (!is_valid_youtube_url($url)) {
            http_response_code(400);
            echo json_encode(['error' => 'Please paste a valid YouTube video, playlist, or channel link.']);
            exit;
        }
        try {
            $overview = Downloader::fetchOverview($url);
            $overview['content_type_guess'] = detect_content_type($url);
            echo json_encode($overview);
        } catch (Exception $e) {
            http_response_code(502);
            echo json_encode(['error' => $e->getMessage()]);
        }
        exit;
    }

    // POST /api/fetch_formats
    if ($apiPath === '/fetch_formats' && $method === 'POST') {
        $body = json_decode(file_get_contents('php://input'), true) ?? [];
        $url = trim($body['url'] ?? '');
        if (!is_valid_youtube_url($url)) {
            http_response_code(400);
            echo json_encode(['error' => 'Invalid URL.']);
            exit;
        }
        try {
            echo json_encode(Downloader::fetchFormats($url));
        } catch (Exception $e) {
            http_response_code(502);
            echo json_encode(['error' => $e->getMessage()]);
        }
        exit;
    }

    // POST /api/queue/add
    if ($apiPath === '/queue/add' && $method === 'POST') {
        $body = json_decode(file_get_contents('php://input'), true) ?? [];
        $items = $body['items'] ?? [$body];
        $sid = get_session_id();
        $added = [];
        foreach ($items as $item) {
            $url = trim($item['url'] ?? '');
            if (!is_valid_youtube_url($url)) continue;
            $task = Download::create([
                'url' => $url,
                'format_id' => $item['format_id'] ?? 'bestvideo+bestaudio/best',
                'quality_label' => $item['quality_label'] ?? 'Best',
                'title' => $item['title'] ?? 'Untitled',
                'thumbnail' => $item['thumbnail'] ?? '',
                'uploader' => $item['uploader'] ?? '',
                'session_id' => $sid
            ]);
            $added[] = $task['id'];
        }
        echo json_encode(['added' => $added, 'count' => count($added)]);
        exit;
    }

    // GET /api/tasks
    if ($apiPath === '/tasks' && $method === 'GET') {
        $rows = Download::findBySession(get_session_id());
        echo json_encode(array_map([Download::class, 'toDict'], $rows));
        exit;
    }

    // POST /api/tasks/{id}/{action}
    if (preg_match('#^/tasks/([^/]+)/([^/]+)$#', $apiPath, $m) && $method === 'POST') {
        $taskId = $m[1];
        $action = $m[2];
        $task = Download::findById($taskId);
        if (!$task || $task['session_id'] !== get_session_id()) {
            http_response_code(404);
            echo json_encode(['error' => 'Not found']);
            exit;
        }
        $ok = false;
        switch ($action) {
            case 'pause':
                if ($task['status'] === 'downloading') Download::update($taskId, ['pause_flag' => 1]);
                elseif ($task['status'] === 'queued') Download::update($taskId, ['status' => 'paused']);
                $ok = true; break;
            case 'resume':
                if ($task['status'] === 'paused') Download::update($taskId, ['status' => 'queued', 'pause_flag' => 0]);
                $ok = true; break;
            case 'cancel':
                if ($task['status'] === 'downloading') Download::update($taskId, ['cancel_flag' => 1]);
                else Download::update($taskId, ['status' => 'cancelled']);
                $ok = true; break;
            case 'retry':
                if (in_array($task['status'], ['failed', 'cancelled']))
                    Download::update($taskId, ['status' => 'queued', 'error_message' => '', 'pause_flag' => 0, 'cancel_flag' => 0]);
                $ok = true; break;
            case 'remove':
                if ($task['status'] !== 'downloading') { Download::remove($taskId); $ok = true; }
                break;
            case 'priority_up':
                if ($task['priority'] > 0) Download::update($taskId, ['priority' => $task['priority'] - 1]);
                $ok = true; break;
            case 'priority_down':
                Download::update($taskId, ['priority' => $task['priority'] + 1]);
                $ok = true; break;
        }
        echo json_encode(['ok' => $ok]);
        exit;
    }

    // GET /api/tasks/{id}/file
    if (preg_match('#^/tasks/([^/]+)/file$#', $apiPath, $m) && $method === 'GET') {
        $task = Download::findById($m[1]);
        if (!$task || $task['status'] !== 'completed' || !file_exists($task['output_file'])) {
            http_response_code(404);
            echo json_encode(['error' => 'Not found']);
            exit;
        }
        header('Content-Type: application/octet-stream');
        header('Content-Disposition: attachment; filename="' . basename($task['output_file']) . '"');
        readfile($task['output_file']);
        exit;
    }

    // POST /api/direct/prepare
    if ($apiPath === '/direct/prepare' && $method === 'POST') {
        $body = json_decode(file_get_contents('php://input'), true) ?? [];
        $url = trim($body['url'] ?? '');
        $formatId = trim($body['format_id'] ?? '');
        if (!is_valid_youtube_url($url) || !$formatId) {
            http_response_code(400);
            echo json_encode(['error' => 'Invalid URL or format.']);
            exit;
        }
        try {
            $target = Downloader::resolveDirect($url, $formatId);
            $token = bin2hex(random_bytes(16));
            // Store in session for simplicity
            $_SESSION['resolve_cache'][$token] = [
                'target' => $target,
                'safe_title' => sanitize_filename($body['title'] ?? 'video'),
                'ts' => time()
            ];
            echo json_encode(['token' => $token]);
        } catch (Exception $e) {
            http_response_code(502);
            echo json_encode(['error' => $e->getMessage()]);
        }
        exit;
    }

    // GET /api/direct/download
    if ($apiPath === '/direct/download' && $method === 'GET') {
        $token = $_GET['token'] ?? '';
        $cached = $_SESSION['resolve_cache'][$token] ?? null;
        if ($cached && time() - $cached['ts'] <= 90) {
            $target = $cached['target'];
            $safeTitle = $cached['safe_title'];
            unset($_SESSION['resolve_cache'][$token]);
        } else {
            http_response_code(400);
            echo json_encode(['error' => 'Token expired']);
            exit;
        }
        if ($target['mode'] === 'proxy') {
            $ext = $target['ext'] ?? 'mp4';
            header('Content-Disposition: attachment; filename="' . $safeTitle . '.' . $ext . '"');
            header('Content-Type: ' . ($ext === 'm4a' ? 'audio/mp4' : 'video/mp4'));
            $ctx = stream_context_create(['http' => ['header' => implode("\r\n", array_map(fn($k, $v) => "$k: $v", array_keys($target['headers'] ?? []), array_values($target['headers'] ?? [])))] ]);
            readfile($target['url'], false, $ctx);
        } else {
            http_response_code(501);
            echo json_encode(['error' => 'Mux mode requires ffmpeg']);
        }
        exit;
    }

    // GET /api/history
    if ($apiPath === '/history' && $method === 'GET') {
        $search = $_GET['search'] ?? '';
        $entries = list_history($search);
        echo json_encode(array_map([History::class, 'toDict'], $entries));
        exit;
    }

    // DELETE /api/history/{id}
    if (preg_match('#^/history/(\d+)$#', $apiPath, $m) && $method === 'DELETE') {
        $ok = delete_history_entry(intval($m[1]));
        echo json_encode(['ok' => $ok]);
        exit;
    }

    // POST /api/history/{id}/redownload
    if (preg_match('#^/history/(\d+)/redownload$#', $apiPath, $m) && $method === 'POST') {
        $entry = History::findById(intval($m[1]));
        if (!$entry) { http_response_code(404); echo json_encode(['error' => 'Not found']); exit; }
        $body = json_decode(file_get_contents('php://input'), true) ?? [];
        $url = trim($body['url'] ?? '');
        if (!is_valid_youtube_url($url)) { http_response_code(400); echo json_encode(['error' => 'Original URL required.']); exit; }
        $task = Download::create([
            'url' => $url, 'format_id' => $entry['format_id'] ?: 'bestvideo+bestaudio/best',
            'quality_label' => $entry['quality_label'] ?: 'Best',
            'title' => $entry['title'], 'thumbnail' => $entry['thumbnail'],
            'uploader' => $entry['uploader'], 'session_id' => get_session_id()
        ]);
        echo json_encode(['ok' => true, 'task_id' => $task['id']]);
        exit;
    }

    // GET /api/history/export.csv
    if ($apiPath === '/history/export.csv' && $method === 'GET') {
        header('Content-Type: text/csv');
        header('Content-Disposition: attachment; filename=history.csv');
        echo export_history_csv();
        exit;
    }

    // GET /api/settings
    if ($apiPath === '/settings' && $method === 'GET') {
        echo json_encode(Settings::toDict(get_settings()));
        exit;
    }

    // POST /api/settings
    if ($apiPath === '/settings' && $method === 'POST') {
        $body = json_decode(file_get_contents('php://input'), true) ?? [];
        $row = update_settings($body);
        echo json_encode(Settings::toDict($row));
        exit;
    }

    // GET /api/stats
    if ($apiPath === '/stats' && $method === 'GET') {
        $sid = get_session_id();
        $completed = Download::countBySessionAndStatus($sid, 'completed');
        $failed = Download::countBySessionAndStatus($sid, 'failed');
        $active = Download::countBySessionAndStatus($sid, 'downloading');
        $queued = Download::countBySessionAndStatus($sid, 'queued');
        $bytes = Download::sumBytesBySessionAndStatus($sid, 'completed');
        $attempts = $completed + $failed;
        $rate = $attempts ? ($completed / $attempts * 100) : 0;
        $totalAll = History::countAll();
        $totalGb = History::sumSizeBytes() / (1024 ** 3);
        $now = time();
        $labels = []; $counts = [];
        for ($i = 6; $i >= 0; $i--) {
            $dayStart = $now - $i * 86400;
            $labels[] = date('D', $dayStart);
            $counts[] = History::countByDayRange($dayStart, $dayStart + 86400);
        }
        $settings = get_settings();
        $disk = get_disk_usage($settings['download_folder']);
        echo json_encode([
            'total_downloads' => $totalAll, 'completed_downloads' => $completed,
            'failed_downloads' => $failed, 'active_downloads' => $active,
            'queued_downloads' => $queued, 'success_rate' => round($rate, 1),
            'downloaded_session_bytes' => $bytes, 'total_gb_downloaded' => round($totalGb, 2),
            'disk_used_bytes' => $disk['used'], 'disk_total_bytes' => $disk['total'],
            'disk_free_bytes' => $disk['free'], 'chart_days_labels' => $labels,
            'chart_days_counts' => $counts,
            'top_uploader' => Download::topUploaderBySession($sid),
            'top_format' => Download::topFormatBySession($sid)
        ]);
        exit;
    }

    http_response_code(404);
    echo json_encode(['error' => 'Not found']);
    exit;
}

// Page routes
function render_page($page, $activePage) {
    $pageTitle = ['dashboard' => 'Dashboard', 'downloads' => 'Downloads & Queue', 'history' => 'History', 'settings' => 'Settings', 'about' => 'About'][$activePage] ?? 'Dashboard';
    include __DIR__ . '/templates/base.php';
}

switch ($uri) {
    case '/': render_page('dashboard', 'dashboard'); break;
    case '/downloads': render_page('downloads', 'downloads'); break;
    case '/history': render_page('history', 'history'); break;
    case '/settings': render_page('settings', 'settings'); break;
    case '/about': render_page('about', 'about'); break;
    default: http_response_code(404); echo "Not found"; break;
}
