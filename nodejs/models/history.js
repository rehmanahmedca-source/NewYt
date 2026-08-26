/**
 * History model -- a permanent record of completed (or failed) downloads.
 */
const { getDb } = require('./database');

function create(data) {
  const db = getDb();
  const now = Date.now() / 1000;
  const result = db.prepare(`
    INSERT INTO history (title, thumbnail, uploader, date_completed, duration_seconds,
      format_id, quality_label, size_bytes, output_path, status, session_id)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `).run(
    data.title || 'Untitled', data.thumbnail || '', data.uploader || '',
    data.date_completed || now, data.duration_seconds || 0,
    data.format_id || '', data.quality_label || '',
    data.size_bytes || 0, data.output_path || '', data.status || 'completed',
    data.session_id || ''
  );
  return { id: result.lastInsertRowid, ...data };
}

function list(search = '') {
  const db = getDb();
  if (search) {
    const like = `%${search}%`;
    return db.prepare(
      "SELECT * FROM history WHERE title LIKE ? OR uploader LIKE ? ORDER BY date_completed DESC"
    ).all(like, like);
  }
  return db.prepare('SELECT * FROM history ORDER BY date_completed DESC').all();
}

function findById(id) {
  return getDb().prepare('SELECT * FROM history WHERE id = ?').get(id);
}

function remove(id) {
  const result = getDb().prepare('DELETE FROM history WHERE id = ?').run(id);
  return result.changes > 0;
}

function count() {
  return getDb().prepare('SELECT COUNT(*) as cnt FROM history').get().cnt;
}

function sumSizeBytes() {
  return getDb().prepare('SELECT COALESCE(SUM(size_bytes), 0) as total FROM history').get().total;
}

function countByDayRange(startTs, endTs) {
  return getDb().prepare(
    'SELECT COUNT(*) as cnt FROM history WHERE date_completed >= ? AND date_completed < ?'
  ).get(startTs, endTs).cnt;
}

function toDict(row) {
  if (!row) return null;
  return {
    id: row.id,
    title: row.title,
    thumbnail: row.thumbnail,
    uploader: row.uploader,
    date_completed: row.date_completed,
    duration_seconds: row.duration_seconds,
    format_id: row.format_id,
    quality_label: row.quality_label,
    size_bytes: row.size_bytes,
    output_path: row.output_path,
    status: row.status
  };
}

module.exports = { create, list, findById, remove, count, sumSizeBytes, countByDayRange, toDict };
