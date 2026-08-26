<?php
/**
 * Small shared helpers.
 */
function new_id() {
    return substr(str_replace('-', '', bin2hex(random_bytes(6))), 0, 12);
}

function get_disk_usage($path) {
    $checkPath = is_dir($path) ? $path : '.';
    $total = disk_total_space($checkPath);
    $free = disk_free_space($checkPath);
    $used = $total - $free;
    return ['used' => $used, 'total' => $total, 'free' => $free];
}

function detect_content_type($url) {
    $u = strtolower($url);
    if (strpos($u, 'playlist') !== false || strpos($u, 'list=') !== false) return 'playlist';
    if (strpos($u, '/shorts/') !== false) return 'shorts';
    if (strpos($u, '/channel/') !== false || strpos($u, '/@') !== false) return 'channel';
    if (strpos($u, 'live') !== false) return 'live';
    return 'video';
}
