/**
 * Settings model -- a single row holding all user-configurable options.
 */
const { getDb } = require('./database');

function get() {
  return getDb().prepare('SELECT * FROM settings WHERE id = 1').get();
}

function create(data) {
  getDb().prepare(`
    INSERT INTO settings (id, download_folder, temp_folder, filename_template,
      max_concurrent, concurrent_fragments, max_retries, auto_resume,
      speed_limit_kbps, default_quality, theme, language, proxy, cookies_path,
      ffmpeg_path, embed_metadata, embed_thumbnail, embed_subtitles, sponsorblock)
    VALUES (1, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `).run(
    data.download_folder || '', data.temp_folder || '',
    data.filename_template || '%(title)s.%(ext)s',
    data.max_concurrent || 3, data.concurrent_fragments || 8, data.max_retries || 3,
    data.auto_resume ? 1 : 0, data.speed_limit_kbps || 0,
    data.default_quality || 'best', data.theme || 'dark', data.language || 'en',
    data.proxy || '', data.cookies_path || '', data.ffmpeg_path || '',
    data.embed_metadata ? 1 : 0, data.embed_thumbnail ? 1 : 0,
    data.embed_subtitles ? 1 : 0, data.sponsorblock ? 1 : 0
  );
}

function update(data) {
  const current = get();
  if (!current) return null;

  const boolFields = ['auto_resume', 'embed_metadata', 'embed_thumbnail', 'embed_subtitles', 'sponsorblock'];
  const intFields = ['max_concurrent', 'concurrent_fragments', 'max_retries', 'speed_limit_kbps'];

  const sets = [];
  const vals = [];

  for (const [key, value] of Object.entries(data)) {
    if (current[key] === undefined) continue;
    let v = value;
    if (boolFields.includes(key)) {
      v = (value === true || value === 'true' || value === '1') ? 1 : 0;
    } else if (intFields.includes(key)) {
      v = parseInt(value) || 0;
    }
    sets.push(`${key} = ?`);
    vals.push(v);
  }

  if (sets.length > 0) {
    getDb().prepare(`UPDATE settings SET ${sets.join(', ')} WHERE id = 1`).run(...vals);
  }
  return get();
}

function toDict(row) {
  if (!row) return {};
  return {
    id: row.id,
    download_folder: row.download_folder,
    temp_folder: row.temp_folder,
    filename_template: row.filename_template,
    max_concurrent: row.max_concurrent,
    concurrent_fragments: row.concurrent_fragments,
    max_retries: row.max_retries,
    auto_resume: !!row.auto_resume,
    speed_limit_kbps: row.speed_limit_kbps,
    default_quality: row.default_quality,
    theme: row.theme,
    language: row.language,
    proxy: row.proxy,
    cookies_path: row.cookies_path,
    ffmpeg_path: row.ffmpeg_path,
    embed_metadata: !!row.embed_metadata,
    embed_thumbnail: !!row.embed_thumbnail,
    embed_subtitles: !!row.embed_subtitles,
    sponsorblock: !!row.sponsorblock
  };
}

module.exports = { get, create, update, toDict };
