/**
 * Settings CRUD -- a single-row table holding all user preferences.
 */
const config = require('../config');
const Settings = require('../models/settings');

function ensureDefaultSettings() {
  let row = Settings.get();
  if (!row) {
    Settings.create({
      download_folder: config.DEFAULT_DOWNLOAD_FOLDER,
      temp_folder: config.DEFAULT_TEMP_FOLDER,
      max_concurrent: config.DEFAULT_MAX_CONCURRENT,
      concurrent_fragments: config.DEFAULT_CONCURRENT_FRAGMENTS,
      max_retries: config.DEFAULT_MAX_RETRIES
    });
    row = Settings.get();
  }
  return row;
}

function getSettings() {
  let row = Settings.get();
  if (!row) {
    row = ensureDefaultSettings();
  }
  return row;
}

function updateSettings(data) {
  return Settings.update(data);
}

module.exports = { ensureDefaultSettings, getSettings, updateSettings };
