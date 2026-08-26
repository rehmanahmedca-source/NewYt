/**
 * Thin wrapper around yt-dlp for metadata / format discovery.
 * Uses yt-dlp via child_process since there's no native Node.js port.
 */
const { execSync, spawn } = require('child_process');

const PLAYER_CLIENTS_ARG = '--extractor-args "youtube:player_client=android,ios,web"';

function runYtDlp(args) {
  const cmd = `yt-dlp ${PLAYER_CLIENTS_ARG} --quiet --no-warnings ${args}`;
  try {
    const output = execSync(cmd, { encoding: 'utf8', maxBuffer: 50 * 1024 * 1024, timeout: 120000 });
    return JSON.parse(output);
  } catch (e) {
    throw new Error(`yt-dlp failed: ${e.message}`);
  }
}

function fetchOverview(url) {
  // First try with extract_flat for speed
  try {
    const info = runYtDlp(`--dump-json --flat-playlist --skip-download "${url}"`);
    if (Array.isArray(info)) {
      // Playlist entries
      const entries = info.map(e => ({
        id: e.id || e.url,
        url: e.url || `https://www.youtube.com/watch?v=${e.id}`,
        title: e.title || 'Untitled',
        thumbnail: e.thumbnail || '',
        uploader: e.uploader || '',
        duration: e.duration || 0
      }));
      return { type: 'playlist', title: 'Playlist', entries };
    }
    // Single video returned directly
    return {
      type: 'video',
      title: info.title || 'Untitled',
      entries: [{
        id: info.id,
        url: url,
        title: info.title || 'Untitled',
        thumbnail: info.thumbnail || '',
        uploader: info.uploader || '',
        duration: info.duration || 0,
        view_count: info.view_count,
        upload_date: info.upload_date,
        description: (info.description || '').substring(0, 500)
      }]
    };
  } catch {
    // Fallback without flat
    const info = runYtDlp(`--dump-json --skip-download "${url}"`);
    return {
      type: 'video',
      title: info.title || 'Untitled',
      entries: [{
        id: info.id,
        url: url,
        title: info.title || 'Untitled',
        thumbnail: info.thumbnail || '',
        uploader: info.uploader || '',
        duration: info.duration || 0
      }]
    };
  }
}

function fetchFormats(url) {
  const info = runYtDlp(`--dump-json --skip-download "${url}"`);
  const formats = (info.formats || []).filter(f => !(f.vcodec === 'none' && f.acodec === 'none')).map(f => ({
    format_id: f.format_id,
    ext: f.ext,
    resolution: f.resolution || (f.height ? `${f.height}p` : 'audio'),
    fps: f.fps,
    vcodec: f.vcodec,
    acodec: f.acodec,
    abr: f.abr,
    tbr: f.tbr,
    filesize: f.filesize || f.filesize_approx,
    dynamic_range: f.dynamic_range,
    is_audio_only: f.vcodec === 'none',
    is_video_only: f.acodec === 'none'
  }));

  return {
    id: info.id,
    title: info.title || 'Untitled',
    uploader: info.uploader || '',
    thumbnail: info.thumbnail || '',
    duration: info.duration || 0,
    view_count: info.view_count,
    upload_date: info.upload_date,
    description: (info.description || '').substring(0, 1000),
    subtitles: Object.keys(info.subtitles || {}),
    automatic_captions: Object.keys(info.automatic_captions || {}).slice(0, 15),
    formats
  };
}

function resolveDirect(url, formatId) {
  const info = runYtDlp(`--dump-json --skip-download -f "${formatId}" "${url}"`);
  const req = info.requested_formats;
  if (req && req.length === 2) {
    return {
      mode: 'mux',
      video_url: req[0].url,
      video_headers: req[0].http_headers || {},
      audio_url: req[1].url,
      audio_headers: req[1].http_headers || {}
    };
  }
  return {
    mode: 'proxy',
    url: info.url,
    headers: info.http_headers || {},
    ext: info.ext || 'mp4'
  };
}

function runDownload(url, formatId, outputTemplate, options = {}, progressCallback) {
  return new Promise((resolve, reject) => {
    const args = [
      '--no-warnings', '--quiet',
      '--extractor-args', 'youtube:player_client=android,ios,web',
      '-f', formatId || 'bestvideo+bestaudio/best',
      '-o', outputTemplate,
      '--no-playlist', '--continue',
      '--newline'
    ];

    if (options.concurrentFragments) {
      args.push('--concurrent-fragments', String(options.concurrentFragments));
    }
    if (options.speedLimitKbps) {
      args.push('--limit-rate', `${options.speedLimitKbps}K`);
    }
    if (options.proxy) {
      args.push('--proxy', options.proxy);
    }
    if (options.cookiesPath) {
      args.push('--cookies', options.cookiesPath);
    }
    if (options.ffmpegPath) {
      args.push('--ffmpeg-location', options.ffmpegPath);
    }

    args.push(url);

    const proc = spawn('yt-dlp', args);
    let outputFile = '';
    let cancelled = false;
    let paused = false;

    proc.stdout.on('data', (data) => {
      const line = data.toString().trim();
      // Parse progress lines like: [download]  45.2% of 100.00MiB at 5.00MiB/s ETA 00:12
      const match = line.match(/\[download\]\s+([\d.]+)%\s+of\s+~?([\d.]+\w+)\s+at\s+([\d.]+\w+\/s)\s+ETA\s+([\d:]+)/);
      if (match && progressCallback) {
        progressCallback({
          status: 'downloading',
          percent: parseFloat(match[1]),
          totalSize: match[2],
          speed: match[3],
          eta: match[4]
        });
      }
      // Detect destination file
      const destMatch = line.match(/\[(?:Merger|ExtractAudio|download)\]\s+Destination:\s+(.+)/);
      if (destMatch) outputFile = destMatch[1];
    });

    proc.stderr.on('data', (data) => {
      // yt-dlp sometimes writes progress to stderr
    });

    proc.on('close', (code) => {
      if (cancelled) {
        reject(new Error('CANCELLED'));
      } else if (code === 0) {
        resolve(outputFile);
      } else {
        reject(new Error(`yt-dlp exited with code ${code}`));
      }
    });

    // Expose cancel method
    proc.cancelDownload = () => {
      cancelled = true;
      proc.kill('SIGTERM');
    };

    // Store reference for pause/cancel
    if (progressCallback) {
      progressCallback._process = proc;
    }
  });
}

module.exports = { fetchOverview, fetchFormats, resolveDirect, runDownload };
