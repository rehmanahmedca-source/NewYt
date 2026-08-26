<?php
/**
 * Input validation / sanitization.
 */
function is_valid_youtube_url($url) {
    if (empty($url) || !is_string($url)) return false;
    $parsed = parse_url(trim($url));
    if (!$parsed || !in_array($parsed['scheme'] ?? '', ['http', 'https'])) return false;
    $host = strtolower($parsed['host'] ?? '');
    $allowed = ['youtube.com', 'youtu.be', 'music.youtube.com'];
    foreach ($allowed as $frag) {
        if (strpos($host, $frag) !== false) return true;
    }
    return false;
}

function sanitize_filename($name) {
    if (empty($name)) return 'file';
    $name = str_replace(['/', '\\'], '_', $name);
    $name = preg_replace('/[<>:"|?*\x00-\x1f]/', '_', $name);
    $name = trim($name, ' .');
    return substr($name, 0, 200) ?: 'file';
}
