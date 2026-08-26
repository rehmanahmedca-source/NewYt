<?php
require_once __DIR__ . '/../models/settings.php';

function ensure_default_settings() {
    $row = Settings::get();
    if (!$row) {
        Settings::create([
            'download_folder' => DEFAULT_DOWNLOAD_FOLDER,
            'temp_folder' => DEFAULT_TEMP_FOLDER,
            'max_concurrent' => DEFAULT_MAX_CONCURRENT,
            'concurrent_fragments' => DEFAULT_CONCURRENT_FRAGMENTS,
            'max_retries' => DEFAULT_MAX_RETRIES
        ]);
        $row = Settings::get();
    }
    return $row;
}

function get_settings() {
    $row = Settings::get();
    if (!$row) $row = ensure_default_settings();
    return $row;
}

function update_settings($data) {
    return Settings::update($data);
}
