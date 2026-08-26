<?php
require_once __DIR__ . '/database.php';

class History {
    public static function create($data) {
        $db = Database::getInstance()->getPdo();
        $now = microtime(true);
        $stmt = $db->prepare("INSERT INTO history (title, thumbnail, uploader, date_completed, duration_seconds, format_id, quality_label, size_bytes, output_path, status, session_id) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
        $stmt->execute([
            $data['title'] ?? 'Untitled', $data['thumbnail'] ?? '', $data['uploader'] ?? '',
            $data['date_completed'] ?? $now, $data['duration_seconds'] ?? 0,
            $data['format_id'] ?? '', $data['quality_label'] ?? '',
            $data['size_bytes'] ?? 0, $data['output_path'] ?? '',
            $data['status'] ?? 'completed', $data['session_id'] ?? ''
        ]);
        return ['id' => $db->lastInsertId()] + $data;
    }

    public static function listAll($search = '') {
        $db = Database::getInstance()->getPdo();
        if ($search) {
            $like = "%$search%";
            $stmt = $db->prepare("SELECT * FROM history WHERE title LIKE ? OR uploader LIKE ? ORDER BY date_completed DESC");
            $stmt->execute([$like, $like]);
            return $stmt->fetchAll();
        }
        return $db->query("SELECT * FROM history ORDER BY date_completed DESC")->fetchAll();
    }

    public static function findById($id) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT * FROM history WHERE id = ?");
        $stmt->execute([$id]);
        return $stmt->fetch();
    }

    public static function remove($id) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("DELETE FROM history WHERE id = ?");
        $stmt->execute([$id]);
        return $stmt->rowCount() > 0;
    }

    public static function countAll() {
        return Database::getInstance()->getPdo()->query("SELECT COUNT(*) as cnt FROM history")->fetch()['cnt'];
    }

    public static function sumSizeBytes() {
        return Database::getInstance()->getPdo()->query("SELECT COALESCE(SUM(size_bytes), 0) as total FROM history")->fetch()['total'];
    }

    public static function countByDayRange($start, $end) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("SELECT COUNT(*) as cnt FROM history WHERE date_completed >= ? AND date_completed < ?");
        $stmt->execute([$start, $end]);
        return $stmt->fetch()['cnt'];
    }

    public static function toDict($row) {
        if (!$row) return null;
        return [
            'id' => $row['id'], 'title' => $row['title'], 'thumbnail' => $row['thumbnail'],
            'uploader' => $row['uploader'], 'date_completed' => $row['date_completed'],
            'duration_seconds' => $row['duration_seconds'], 'format_id' => $row['format_id'],
            'quality_label' => $row['quality_label'], 'size_bytes' => $row['size_bytes'],
            'output_path' => $row['output_path'], 'status' => $row['status']
        ];
    }
}
