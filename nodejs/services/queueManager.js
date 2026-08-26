/**
 * Download engine: dispatches queued tasks, runs yt-dlp downloads in
 * background, and supports pause / resume / cancel / retry / re-priority.
 */
const path = require('path');
const fs = require('fs');
const { getSettings } = require('./settingsService');
const { addHistoryEntry } = require('./historyService');
const Download = require('../models/download');
const { getLogger } = require('../utils/logger');
const downloader = require('./downloader');

class QueueManager {
  constructor() {
    this.activeCount = 0;
    this.running = {}; // taskId -> { process, cancelled }
    this.dispatcherStarted = false;
  }

  start() {
    if (this.dispatcherStarted) return;
    this.dispatcherStarted = true;
    this._dispatcherLoop();
  }

  recoverIncompleteTasks() {
    const tasks = Download.findDownloading();
    for (const task of tasks) {
      Download.update(task.id, { status: 'queued' });
    }
  }

  addTask(data) {
    const task = Download.create(data);
    return task;
  }

  pause(taskId) {
    const task = Download.findById(taskId);
    if (!task) return false;
    if (task.status === 'downloading') {
      Download.update(taskId, { pause_flag: 1 });
      const running = this.running[taskId];
      if (running && running.process) {
        running.process.kill('SIGTERM');
      }
    } else if (task.status === 'queued') {
      Download.update(taskId, { status: 'paused' });
    }
    return true;
  }

  resume(taskId) {
    const task = Download.findById(taskId);
    if (!task || task.status !== 'paused') return false;
    Download.update(taskId, { status: 'queued', pause_flag: 0 });
    return true;
  }

  cancel(taskId) {
    const task = Download.findById(taskId);
    if (!task) return false;
    if (task.status === 'downloading') {
      Download.update(taskId, { cancel_flag: 1 });
      const running = this.running[taskId];
      if (running && running.process) {
        running.process.kill('SIGTERM');
      }
    } else {
      Download.update(taskId, { status: 'cancelled' });
    }
    return true;
  }

  retry(taskId) {
    const task = Download.findById(taskId);
    if (!task || !['failed', 'cancelled'].includes(task.status)) return false;
    Download.update(taskId, { status: 'queued', error_message: '', pause_flag: 0, cancel_flag: 0 });
    return true;
  }

  remove(taskId) {
    const task = Download.findById(taskId);
    if (!task || task.status === 'downloading') return false;
    Download.remove(taskId);
    return true;
  }

  reprioritize(taskId, direction) {
    const task = Download.findById(taskId);
    if (!task) return false;
    // Simple priority swap
    if (direction === 'up' && task.priority > 0) {
      Download.update(taskId, { priority: task.priority - 1 });
    } else if (direction === 'down') {
      Download.update(taskId, { priority: task.priority + 1 });
    }
    return true;
  }

  _dispatcherLoop() {
    const tick = () => {
      try {
        const settings = getSettings();
        while (this.activeCount < settings.max_concurrent) {
          const task = Download.findQueued();
          if (!task) break;
          this.activeCount++;
          Download.update(task.id, { status: 'downloading' });
          this._runDownload(task.id).finally(() => {
            this.activeCount = Math.max(0, this.activeCount - 1);
          });
        }
      } catch (e) {
        getLogger().error(`Dispatcher error: ${e.message}`);
      }
      setTimeout(tick, 800);
    };
    tick();
  }

  async _runDownload(taskId) {
    try {
      const task = Download.findById(taskId);
      if (!task) return;
      const settings = getSettings();
      fs.mkdirSync(settings.download_folder, { recursive: true });

      const outputTemplate = path.join(settings.download_folder, settings.filename_template || '%(title)s.%(ext)s');

      const opts = {
        concurrentFragments: settings.concurrent_fragments,
        speedLimitKbps: settings.speed_limit_kbps,
        proxy: settings.proxy,
        cookiesPath: settings.cookies_path,
        ffmpegPath: settings.ffmpeg_path
      };

      const progressCallback = (d) => {
        if (d.status === 'downloading') {
          Download.update(taskId, {
            progress: d.percent || 0,
            speed: 0,
            eta_seconds: 0
          });
        }
      };

      const outputFile = await downloader.runDownload(task.url, task.format_id, outputTemplate, opts, progressCallback);

      Download.update(taskId, {
        status: 'completed',
        progress: 100,
        output_file: outputFile,
        finished_time: Date.now() / 1000
      });

      addHistoryEntry({
        title: task.title,
        thumbnail: task.thumbnail,
        uploader: task.uploader,
        format_id: task.format_id,
        quality_label: task.quality_label,
        size_bytes: task.total_bytes,
        output_path: outputFile,
        status: 'completed',
        session_id: task.session_id
      });

      getLogger().info(`Completed: ${task.title}`);
    } catch (e) {
      const task = Download.findById(taskId);
      if (!task) return;
      const settings = getSettings();

      if (e.message === 'CANCELLED') {
        Download.update(taskId, { status: 'cancelled', cancel_flag: 0 });
      } else {
        const retryCount = task.retry_count + 1;
        if (retryCount <= settings.max_retries) {
          Download.update(taskId, { status: 'queued', retry_count: retryCount, error_message: e.message });
        } else {
          Download.update(taskId, { status: 'failed', retry_count: retryCount, error_message: e.message });
        }
        getLogger().warning(`Download failed (${retryCount}x): ${task.title} -> ${e.message}`);
      }
    }
  }
}

const queueManager = new QueueManager();
module.exports = { queueManager };
