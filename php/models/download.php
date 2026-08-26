<?php
/**
 * Download (Task) model.
 */
require_once __DIR__ . '/database.php';

class Download {
    public static function create($data) {
        $db = Database::getInstance()->getPdo();
        $id = new_id();
        $now = microtime(true);
        $stmt = $db->prepare("INSERT INTO downloads (id, url, title, thumbnail, uploader, format_id, quality_label, status, session_id, created_time) VALUES (?, ?, ?, ?, ?, ?, ?, 'queued', ?, ?)");
        $stmt->execute([
            $id, $data['url'], $data['title'] ?? 'Untitled', $data['thumbnail'] ?? '',
            $data['uploader'] ?? '', $data['format_id'] ?? 'best', $data['quality_label'] ?? 'Best',
            $data['session_id'] ?? '', $now
        ]);
        return ['id' => $id] + $data;
    }

    public static function findById($id) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT * FROM downloads WHERE id = ?");
        $stmt->execute([$id]);
        return $stmt->fetch();
    }

    public static function findBySession($sessionId) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT * FROM downloads WHERE session_id = ? ORDER BY priority ASC, created_time ASC");
        $stmt->execute([$sessionId]);
        return $stmt->fetchAll();
    }

    public static function update($id, $fields) {
        $db = Database::getInstance()->getPdo();
        $sets = [];
        $vals = [];
        foreach ($fields as $k => $v) {
            $sets[] = "$k = ?";
            $vals[] = $v;
        }
        $vals[] = $id;
        $db->prepare("UPDATE downloads SET " . implode(', ', $sets) . " WHERE id = ?")->execute($vals);
    }

    public static function remove($id) {
        $db = Database::getInstance()->getPdo();
        $db->prepare("DELETE FROM downloads WHERE id = ?")->execute([$id]);
    }

    public static function findDownloading() {
        $db = Database::getInstance()->getPdo();
        return $db->query("SELECT * FROM downloads WHERE status = 'downloading'")->fetchAll();
    }

    public static function findQueued() {
        $db = Database::getInstance()->getPdo();
        return $db->query("SELECT * FROM downloads WHERE status = 'queued' ORDER BY priority ASC, created_time ASC LIMIT 1")->fetch();
    }

    public static function countBySessionAndStatus($sid, $status) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT COUNT(*) as cnt FROM downloads WHERE session_id = ? AND status = ?");
        $stmt->execute([$sid, $status]);
        return $stmt->fetch()['cnt'];
    }

    public static function sumBytesBySessionAndStatus($sid, $status) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT COALESCE(SUM(total_bytes), 0) as total FROM downloads WHERE session_id = ? AND status = ?");
        $stmt->execute([$sid, $status]);
        return $stmt->fetch()['total'];
    }

    public static function topUploaderBySession($sid) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT uploader, COUNT(*) as c FROM downloads WHERE session_id = ? AND status = 'completed' AND uploader != '' GROUP BY uploader ORDER BY c DESC LIMIT 1");
        $stmt->execute([$sid]);
        $row = $stmt->fetch();
        return $row ? $row['uploader'] : '-';
    }

    public static function topFormatBySession($sid) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT quality_label, COUNT(*) as c FROM downloads WHERE session_id = ? AND status = 'completed' AND quality_label != '' GROUP BY quality_label ORDER BY c DESC LIMIT 1");
        $stmt->execute([$sid]);
        $row = $stmt->fetch();
        return $row ? $row['quality_label'] : '-';
    }

    public static function toDict($row) {
        if (!$row) return null;
        return [
            'id' => $row['id'], 'url' => $row['url'], 'title' => $row['title'],
            'thumbnail' => $row['thumbnail'], 'uploader' => $row['uploader'],
            'format_id' => $row['format_id'], 'quality_label' => $row['quality_label'],
            'status' => $row['status'], 'speed' => $row['speed'],
            'eta_seconds' => $row['eta_seconds'], 'downloaded_bytes' => $row['downloaded_bytes'],
            'total_bytes' => $row['total_bytes'], 'progress' => round($row['progress'], 1),
            'output_file' => $row['output_file'], 'error_message' => $row['error_message'],
            'retry_count' => $row['retry_count'], 'priority' => $row['priority'],
            'created_time' => $row['created_time'], 'finished_time' => $row['finished_time']
        ];
    }
}
