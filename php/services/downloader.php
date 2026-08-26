<?php
/**
 * Thin wrapper around yt-dlp for metadata / format discovery.
 */
class Downloader {
    private static function runYtDlp($args) {
        $cmd = 'yt-dlp --extractor-args "youtube:player_client=android,ios,web" --quiet --no-warnings ' . $args . ' 2>/dev/null';
        $output = shell_exec($cmd);
        if (!$output) throw new RuntimeException("yt-dlp failed");
        return json_decode($output, true);
    }

    public static function fetchOverview($url) {
        try {
            $info = self::runYtDlp('--dump-json --flat-playlist --skip-download ' . escapeshellarg($url));
            if (isset($info['id'])) {
                // Single video
                return [
                    'type' => 'video',
                    'title' => $info['title'] ?? 'Untitled',
                    'entries' => [[
                        'id' => $info['id'], 'url' => $url,
                        'title' => $info['title'] ?? 'Untitled',
                        'thumbnail' => $info['thumbnail'] ?? '',
                        'uploader' => $info['uploader'] ?? '',
                        'duration' => $info['duration'] ?? 0,
                        'view_count' => $info['view_count'] ?? null,
                        'description' => substr($info['description'] ?? '', 0, 500)
                    ]]
                ];
            }
            // Playlist
            $entries = [];
            foreach (($info['entries'] ?? []) as $e) {
                $entries[] = [
                    'id' => $e['id'] ?? '', 'url' => $e['url'] ?? "https://www.youtube.com/watch?v=" . ($e['id'] ?? ''),
                    'title' => $e['title'] ?? 'Untitled', 'thumbnail' => $e['thumbnail'] ?? '',
                    'uploader' => $e['uploader'] ?? '', 'duration' => $e['duration'] ?? 0
                ];
            }
            return ['type' => 'playlist', 'title' => $info['title'] ?? 'Playlist', 'entries' => $entries];
        } catch (\Exception $e) {
            throw new RuntimeException("Could not fetch info: " . $e->getMessage());
        }
    }

    public static function fetchFormats($url) {
        $info = self::runYtDlp('--dump-json --skip-download ' . escapeshellarg($url));
        $formats = [];
        foreach (($info['formats'] ?? []) as $f) {
            if ($f['vcodec'] === 'none' && $f['acodec'] === 'none') continue;
            $formats[] = [
                'format_id' => $f['format_id'], 'ext' => $f['ext'],
                'resolution' => $f['resolution'] ?? ($f['height'] ? "{$f['height']}p" : 'audio'),
                'fps' => $f['fps'] ?? null, 'vcodec' => $f['vcodec'] ?? '',
                'acodec' => $f['acodec'] ?? '', 'abr' => $f['abr'] ?? null,
                'tbr' => $f['tbr'] ?? null, 'filesize' => $f['filesize'] ?? $f['filesize_approx'] ?? null,
                'dynamic_range' => $f['dynamic_range'] ?? null,
                'is_audio_only' => ($f['vcodec'] ?? '') === 'none',
                'is_video_only' => ($f['acodec'] ?? '') === 'none'
            ];
        }
        return [
            'id' => $info['id'], 'title' => $info['title'] ?? 'Untitled',
            'uploader' => $info['uploader'] ?? '', 'thumbnail' => $info['thumbnail'] ?? '',
            'duration' => $info['duration'] ?? 0, 'formats' => $formats
        ];
    }

    public static function resolveDirect($url, $formatId) {
        $info = self::runYtDlp('--dump-json --skip-download -f ' . escapeshellarg($formatId) . ' ' . escapeshellarg($url));
        $req = $info['requested_formats'] ?? null;
        if ($req && count($req) === 2) {
            return [
                'mode' => 'mux',
                'video_url' => $req[0]['url'], 'video_headers' => $req[0]['http_headers'] ?? [],
                'audio_url' => $req[1]['url'], 'audio_headers' => $req[1]['http_headers'] ?? []
            ];
        }
        return [
            'mode' => 'proxy', 'url' => $info['url'],
            'headers' => $info['http_headers'] ?? [], 'ext' => $info['ext'] ?? 'mp4'
        ];
    }
}
