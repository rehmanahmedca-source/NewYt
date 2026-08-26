/**
 * Download (Task) model -- represents one item in the download queue.
 */
const { v4: uuidv4 } = require('uuid');
const { getDb } = require('./database');

function genId() {
  return uuidv4().replace(/-/g, '').substring(0, 12);
}

function toDict(row) {
  if (!row) return null;
  return {
    id: row.id,
    url: row.url,
    title: row.title,
    thumbnail: row.thumbnail,
    uploader: row.uploader,
    format_id: row.format_id,
    quality_label: row.quality_label,
    status: row.status,
    speed: row.speed,
    eta_seconds: row.eta_seconds,
    downloaded_bytes: row.downloaded_bytes,
    total_bytes: row.total_bytes,
    progress: Math.round((row.progress || 0) * 10) / 10,
    output_file: row.output_file,
    error_message: row.error_message,
    retry_count: row.retry_count,
    priority: row.priority,
    created_time: row.created_time,
    finished_time: row.finished_time
  };
}

function create(data) {
  const db = getDb();
  const id = genId();
  const now = Date.now() / 1000;
  db.prepare(`
    INSERT INTO downloads (id, url, title, thumbnail, uploader, format_id, quality_label,
      status, session_id, created_time)
    VALUES (?, ?, ?, ?, ?, ?, ?, 'queued', ?, ?)
  `).run(
    id, data.url, data.title || 'Untitled', data.thumbnail || '',
    data.uploader || '', data.format_id || 'best', data.quality_label || 'Best',
    data.session_id || '', now
  );
  return { id, ...data, status: 'queued', created_time: now };
}

function findById(id) {
  return getDb().prepare('SELECT * FROM downloads WHERE id = ?').get(id);
}

function findBySession(sessionId) {
  return getDb().prepare(
    'SELECT * FROM downloads WHERE session_id = ? ORDER BY priority ASC, created_time ASC'
  ).all(sessionId);
}

function update(id, fields) {
  const db = getDb();
  const sets = [];
  const vals = [];
  for (const [k, v] of Object.entries(fields)) {
    sets.push(`${k} = ?`);
    vals.push(v);
  }
  vals.push(id);
  db.prepare(`UPDATE downloads SET ${sets.join(', ')} WHERE id = ?`).run(...vals);
}

function remove(id) {
  getDb().prepare('DELETE FROM downloads WHERE id = ?').run(id);
}

function findDownloading() {
  return getDb().prepare("SELECT * FROM downloads WHERE status = 'downloading'").all();
}

function findQueued() {
  return getDb().prepare(
    "SELECT * FROM downloads WHERE status = 'queued' ORDER BY priority ASC, created_time ASC LIMIT 1"
  ).get();
}

function countBySessionAndStatus(sessionId, status) {
  return getDb().prepare(
    'SELECT COUNT(*) as cnt FROM downloads WHERE session_id = ? AND status = ?'
  ).get(sessionId, status).cnt;
}

function sumBytesBySessionAndStatus(sessionId, status) {
  return getDb().prepare(
    'SELECT COALESCE(SUM(total_bytes), 0) as total FROM downloads WHERE session_id = ? AND status = ?'
  ).get(sessionId, status).total;
}

function topUploaderBySession(sessionId) {
  const row = getDb().prepare(
    "SELECT uploader, COUNT(*) as c FROM downloads WHERE session_id = ? AND status = 'completed' AND uploader != '' GROUP BY uploader ORDER BY c DESC LIMIT 1"
  ).get(sessionId);
  return row ? row.uploader : '-';
}

function topFormatBySession(sessionId) {
  const row = getDb().prepare(
    "SELECT quality_label, COUNT(*) as c FROM downloads WHERE session_id = ? AND status = 'completed' AND quality_label != '' GROUP BY quality_label ORDER BY c DESC LIMIT 1"
  ).get(sessionId);
  return row ? row.quality_label : '-';
}

module.exports = {
  genId, toDict, create, findById, findBySession, update, remove,
  findDownloading, findQueued, countBySessionAndStatus,
  sumBytesBySessionAndStatus, topUploaderBySession, topFormatBySession
};
