<?php
require_once __DIR__ . '/database.php';

class Settings {
    public static function get() {
        return Database::getInstance()->getPdo()->query("SELECT * FROM settings WHERE id = 1")->fetch();
    }

    public static function create($data) {
        $db = Database::getInstance()->getPdo();
        $stmt = $db->prepare("INSERT INTO settings (id, download_folder, temp_folder, filename_template, max_concurrent, concurrent_fragments, max_retries) VALUES (1, ?, ?, ?, ?, ?, ?)");
        $stmt->execute([
            $data['download_folder'] ?? '', $data['temp_folder'] ?? '',
            $data['filename_template'] ?? '%(title)s.%(ext)s',
            $data['max_concurrent'] ?? 3, $data['concurrent_fragments'] ?? 8,
            $data['max_retries'] ?? 3
        ]);
    }

    public static function update($data) {
        $db = Database::getInstance()->getPdo();
        $boolFields = ['auto_resume', 'embed_metadata', 'embed_thumbnail', 'embed_subtitles', 'sponsorblock'];
        $intFields = ['max_concurrent', 'concurrent_fragments', 'max_retries', 'speed_limit_kbps'];
        $sets = [];
        $vals = [];
        foreach ($data as $key => $value) {
            if (in_array($key, $boolFields)) {
                $value = ($value === true || $value === 'true' || $value === '1') ? 1 : 0;
            } elseif (in_array($key, $intFields)) {
                $value = intval($value);
            }
            $sets[] = "$key = ?";
            $vals[] = $value;
        }
        if ($sets) {
            $db->prepare("UPDATE settings SET " . implode(', ', $sets) . " WHERE id = 1")->execute($vals);
        }
        return self::get();
    }

    public static function toDict($row) {
        if (!$row) return [];
        $bools = ['auto_resume', 'embed_metadata', 'embed_thumbnail', 'embed_subtitles', 'sponsorblock'];
        $dict = [];
        foreach ($row as $k => $v) {
            $dict[$k] = in_array($k, $bools) ? (bool)$v : $v;
        }
        return $dict;
    }
}
