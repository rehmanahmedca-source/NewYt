/**
 * JSON API -- fetch metadata, manage the download queue, history, settings,
 * and dashboard statistics.
 */
const express = require('express');
const router = express.Router();
const { v4: uuidv4 } = require('uuid');
const axios = require('axios');
const fs = require('fs');
const path = require('path');

const Download = require('../models/download');
const History = require('../models/history');
const downloader = require('../services/downloader');
const { queueManager } = require('../services/queueManager');
const { getSettings, updateSettings } = require('../services/settingsService');
const { listHistory, deleteHistoryEntry, exportHistoryCsv } = require('../services/historyService');
const { isValidYoutubeUrl, sanitizeFilename } = require('../utils/validators');
const { getDiskUsage, detectContentType } = require('../utils/helpers');
const { getLogger } = require('../utils/logger');

// In-memory cache for pre-resolved direct-download targets
const RESOLVE_CACHE = {};
const RESOLVE_TTL_SECONDS = 90;

function cachePrune() {
  const now = Date.now() / 1000;
  for (const k of Object.keys(RESOLVE_CACHE)) {
    if (now - RESOLVE_CACHE[k].ts > RESOLVE_TTL_SECONDS) {
      delete RESOLVE_CACHE[k];
    }
  }
}

function getSessionId(req) {
  if (!req.session.sid) {
    req.session.sid = uuidv4().replace(/-/g, '').substring(0, 16);
  }
  return req.session.sid;
}

// --------------------------------------------------------------- fetch
router.post('/fetch', async (req, res) => {
  const url = (req.body.url || '').trim();
  if (!isValidYoutubeUrl(url)) {
    return res.status(400).json({ error: 'Please paste a valid YouTube video, playlist, or channel link.' });
  }
  try {
    const overview = downloader.fetchOverview(url);
    overview.content_type_guess = detectContentType(url);
    res.json(overview);
  } catch (e) {
    getLogger().warning(`Fetch failed for ${url}: ${e.message}`);
    res.status(502).json({ error: e.message });
  }
});

router.post('/fetch_formats', async (req, res) => {
  const url = (req.body.url || '').trim();
  if (!isValidYoutubeUrl(url)) {
    return res.status(400).json({ error: 'Invalid URL.' });
  }
  try {
    res.json(downloader.fetchFormats(url));
  } catch (e) {
    res.status(502).json({ error: e.message });
  }
});

// --------------------------------------------------------------- queue
router.post('/queue/add', (req, res) => {
  let items = req.body.items;
  if (!items) items = [req.body];
  const sid = getSessionId(req);
  const added = [];
  for (const item of items) {
    const url = (item.url || '').trim();
    if (!isValidYoutubeUrl(url)) continue;
    const task = queueManager.addTask({
      url,
      format_id: item.format_id || 'bestvideo+bestaudio/best',
      quality_label: item.quality_label || 'Best',
      title: item.title || 'Untitled',
      thumbnail: item.thumbnail || '',
      uploader: item.uploader || '',
      session_id: sid
    });
    added.push(task.id);
  }
  res.json({ added, count: added.length });
});

router.get('/tasks', (req, res) => {
  const sid = getSessionId(req);
  const rows = Download.findBySession(sid);
  res.json(rows.map(Download.toDict));
});

router.post('/tasks/:taskId/:action', (req, res) => {
  const { taskId, action } = req.params;
  const sid = getSessionId(req);
  const task = Download.findById(taskId);
  if (!task || task.session_id !== sid) {
    return res.status(404).json({ error: 'Not found' });
  }

  let ok = false;
  switch (action) {
    case 'pause': ok = queueManager.pause(taskId); break;
    case 'resume': ok = queueManager.resume(taskId); break;
    case 'cancel': ok = queueManager.cancel(taskId); break;
    case 'retry': ok = queueManager.retry(taskId); break;
    case 'remove': ok = queueManager.remove(taskId); break;
    case 'priority_up': ok = queueManager.reprioritize(taskId, 'up'); break;
    case 'priority_down': ok = queueManager.reprioritize(taskId, 'down'); break;
    default: return res.status(400).json({ error: 'Unknown action' });
  }
  res.json({ ok });
});

router.get('/tasks/:taskId/file', (req, res) => {
  const task = Download.findById(req.params.taskId);
  if (!task || task.status !== 'completed' || !task.output_file) {
    return res.status(404).json({ error: 'Not found' });
  }
  if (!fs.existsSync(task.output_file)) {
    return res.status(404).json({ error: 'File not found' });
  }
  res.download(task.output_file, path.basename(task.output_file));
});

// --------------------------------------------------------------- direct download
router.post('/direct/prepare', (req, res) => {
  const { url, format_id, title } = req.body;
  if (!isValidYoutubeUrl(url) || !format_id) {
    return res.status(400).json({ error: 'Invalid URL or format.' });
  }
  try {
    const target = downloader.resolveDirect(url, format_id);
    cachePrune();
    const token = uuidv4().replace(/-/g, '');
    RESOLVE_CACHE[token] = {
      target,
      safe_title: sanitizeFilename(title || 'video'),
      ts: Date.now() / 1000
    };
    res.json({ token });
  } catch (e) {
    getLogger().warning(`Prepare resolve failed for ${url}: ${e.message}`);
    res.status(502).json({ error: e.message });
  }
});

router.get('/direct/download', async (req, res) => {
  const token = (req.query.token || '').trim();
  const cached = token ? RESOLVE_CACHE[token] : null;
  if (cached) delete RESOLVE_CACHE[token];

  let target, safeTitle;
  if (cached && (Date.now() / 1000 - cached.ts <= RESOLVE_TTL_SECONDS)) {
    target = cached.target;
    safeTitle = cached.safe_title;
  } else {
    const url = (req.query.url || '').trim();
    const formatId = (req.query.format_id || '').trim();
    if (!isValidYoutubeUrl(url) || !formatId) {
      return res.status(400).json({ error: 'Invalid parameters' });
    }
    try {
      target = downloader.resolveDirect(url, formatId);
    } catch (e) {
      return res.status(502).json({ error: e.message });
    }
    safeTitle = sanitizeFilename(req.query.title || 'video');
  }

  if (target.mode === 'proxy') {
    const ext = target.ext || 'mp4';
    const filename = `${safeTitle}.${ext}`;
    res.setHeader('Content-Disposition', `attachment; filename="${filename}"`);
    res.setHeader('Content-Type', ext === 'm4a' ? 'audio/mp4' : 'video/mp4');
    try {
      const response = await axios.get(target.url, {
        headers: target.headers || {},
        responseType: 'stream',
        timeout: 30000
      });
      response.data.pipe(res);
    } catch (e) {
      res.status(502).json({ error: 'Stream failed' });
    }
  } else {
    // Mux mode - would need ffmpeg piping
    const filename = `${safeTitle}.mp4`;
    res.setHeader('Content-Disposition', `attachment; filename="${filename}"`);
    res.setHeader('Content-Type', 'video/mp4');
    res.status(501).json({ error: 'Mux mode requires ffmpeg' });
  }
});

// --------------------------------------------------------------- history
router.get('/history', (req, res) => {
  const search = req.query.search || '';
  let entries = listHistory(search);
  if (req.query.scope === 'session') {
    const sid = getSessionId(req);
    entries = entries.filter(e => e.session_id === sid);
  }
  res.json(entries.map(History.toDict));
});

router.delete('/history/:id', (req, res) => {
  const ok = deleteHistoryEntry(parseInt(req.params.id));
  res.json({ ok });
});

router.post('/history/:id/redownload', (req, res) => {
  const entry = History.findById(parseInt(req.params.id));
  if (!entry) return res.status(404).json({ error: 'Not found' });
  const url = (req.body.url || '').trim();
  if (!isValidYoutubeUrl(url)) {
    return res.status(400).json({ error: 'Original URL required to redownload.' });
  }
  const task = queueManager.addTask({
    url,
    format_id: entry.format_id || 'bestvideo+bestaudio/best',
    quality_label: entry.quality_label || 'Best',
    title: entry.title,
    thumbnail: entry.thumbnail,
    uploader: entry.uploader,
    session_id: getSessionId(req)
  });
  res.json({ ok: true, task_id: task.id });
});

router.get('/history/export.csv', (req, res) => {
  const csv = exportHistoryCsv();
  res.setHeader('Content-Type', 'text/csv');
  res.setHeader('Content-Disposition', 'attachment; filename=history.csv');
  res.send(csv);
});

// --------------------------------------------------------------- settings
router.get('/settings', (req, res) => {
  const Settings = require('../models/settings');
  res.json(Settings.toDict(getSettings()));
});

router.post('/settings', (req, res) => {
  const row = updateSettings(req.body);
  const Settings = require('../models/settings');
  res.json(Settings.toDict(row));
});

// --------------------------------------------------------------- stats
router.get('/stats', (req, res) => {
  const sid = getSessionId(req);

  const completed = Download.countBySessionAndStatus(sid, 'completed');
  const failed = Download.countBySessionAndStatus(sid, 'failed');
  const active = Download.countBySessionAndStatus(sid, 'downloading');
  const queued = Download.countBySessionAndStatus(sid, 'queued');

  const downloadedSessionBytes = Download.sumBytesBySessionAndStatus(sid, 'completed');
  const sessionAttempts = completed + failed;
  const sessionSuccessRate = sessionAttempts ? (completed / sessionAttempts * 100) : 0;

  const totalDownloadsAlltime = History.count();
  const totalGbDownloaded = History.sumSizeBytes() / (1024 ** 3);

  const now = Date.now() / 1000;
  const dayLabels = [];
  const dayCounts = [];
  const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  for (let i = 6; i >= 0; i--) {
    const dayStart = now - i * 86400;
    const dayEnd = dayStart + 86400;
    const count = History.countByDayRange(dayStart, dayEnd);
    const d = new Date(dayStart * 1000);
    dayLabels.push(days[d.getDay()]);
    dayCounts.push(count);
  }

  const settings = getSettings();
  const disk = getDiskUsage(settings.download_folder);

  res.json({
    total_downloads: totalDownloadsAlltime,
    completed_downloads: completed,
    failed_downloads: failed,
    active_downloads: active,
    queued_downloads: queued,
    success_rate: Math.round(sessionSuccessRate * 10) / 10,
    downloaded_session_bytes: downloadedSessionBytes || 0,
    total_gb_downloaded: Math.round(totalGbDownloaded * 100) / 100,
    disk_used_bytes: disk.used,
    disk_total_bytes: disk.total,
    disk_free_bytes: disk.free,
    chart_days_labels: dayLabels,
    chart_days_counts: dayCounts,
    top_uploader: Download.topUploaderBySession(sid),
    top_format: Download.topFormatBySession(sid)
  });
});

module.exports = router;
