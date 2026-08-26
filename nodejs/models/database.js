/**
 * SQLite database initialization using better-sqlite3 (synchronous API).
 */
const Database = require('better-sqlite3');
const config = require('../config');

let db = null;

function getDb() {
  if (!db) {
    db = new Database(config.DATABASE_PATH);
    db.pragma('journal_mode = WAL');
  }
  return db;
}

function initDatabase() {
  const d = getDb();
  d.exec(`
    CREATE TABLE IF NOT EXISTS downloads (
      id TEXT PRIMARY KEY,
      url TEXT NOT NULL,
      title TEXT DEFAULT 'Untitled',
      thumbnail TEXT DEFAULT '',
      uploader TEXT DEFAULT '',
      format_id TEXT DEFAULT 'best',
      quality_label TEXT DEFAULT 'Best',
      status TEXT DEFAULT 'queued',
      speed REAL DEFAULT 0.0,
      eta_seconds INTEGER DEFAULT 0,
      downloaded_bytes INTEGER DEFAULT 0,
      total_bytes INTEGER DEFAULT 0,
      progress REAL DEFAULT 0.0,
      output_file TEXT DEFAULT '',
      error_message TEXT DEFAULT '',
      resume_supported INTEGER DEFAULT 1,
      retry_count INTEGER DEFAULT 0,
      priority INTEGER DEFAULT 0,
      pause_flag INTEGER DEFAULT 0,
      cancel_flag INTEGER DEFAULT 0,
      session_id TEXT DEFAULT '',
      created_time REAL,
      finished_time REAL
    );

    CREATE TABLE IF NOT EXISTS history (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      title TEXT DEFAULT 'Untitled',
      thumbnail TEXT DEFAULT '',
      uploader TEXT DEFAULT '',
      date_completed REAL,
      duration_seconds INTEGER DEFAULT 0,
      format_id TEXT DEFAULT '',
      quality_label TEXT DEFAULT '',
      size_bytes INTEGER DEFAULT 0,
      output_path TEXT DEFAULT '',
      status TEXT DEFAULT 'completed',
      session_id TEXT DEFAULT ''
    );

    CREATE TABLE IF NOT EXISTS settings (
      id INTEGER PRIMARY KEY DEFAULT 1,
      download_folder TEXT DEFAULT '',
      temp_folder TEXT DEFAULT '',
      filename_template TEXT DEFAULT '%(title)s.%(ext)s',
      max_concurrent INTEGER DEFAULT 3,
      concurrent_fragments INTEGER DEFAULT 8,
      max_retries INTEGER DEFAULT 3,
      auto_resume INTEGER DEFAULT 1,
      speed_limit_kbps INTEGER DEFAULT 0,
      default_quality TEXT DEFAULT 'best',
      theme TEXT DEFAULT 'dark',
      language TEXT DEFAULT 'en',
      proxy TEXT DEFAULT '',
      cookies_path TEXT DEFAULT '',
      ffmpeg_path TEXT DEFAULT '',
      embed_metadata INTEGER DEFAULT 0,
      embed_thumbnail INTEGER DEFAULT 0,
      embed_subtitles INTEGER DEFAULT 0,
      sponsorblock INTEGER DEFAULT 0
    );
  `);

  // Create indexes
  d.exec(`CREATE INDEX IF NOT EXISTS idx_downloads_session ON downloads(session_id)`);
  d.exec(`CREATE INDEX IF NOT EXISTS idx_history_session ON history(session_id)`);
}

module.exports = { getDb, initDatabase };
