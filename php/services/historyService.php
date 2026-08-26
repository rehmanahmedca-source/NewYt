<?php
require_once __DIR__ . '/../models/history.php';

function add_history_entry($data) { return History::create($data); }
function list_history($search = '') { return History::listAll($search); }
function delete_history_entry($id) { return History::remove($id); }
function export_history_csv() {
    $entries = History::listAll();
    $lines = ['Title,Uploader,Date,Quality,Size (bytes),Status,Path'];
    foreach ($entries as $e) {
        $lines[] = '"' . str_replace('"', '""', $e['title']) . '","' . str_replace('"', '""', $e['uploader']) . '",' . $e['date_completed'] . ',"' . $e['quality_label'] . '",' . $e['size_bytes'] . ',"' . $e['status'] . '","' . $e['output_path'] . '"';
    }
    return implode("\n", $lines);
}
