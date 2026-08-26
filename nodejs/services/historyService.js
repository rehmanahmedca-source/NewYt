/**
 * History CRUD + CSV export.
 */
const History = require('../models/history');

function addHistoryEntry(data) {
  return History.create(data);
}

function listHistory(search = '') {
  return History.list(search);
}

function deleteHistoryEntry(id) {
  return History.remove(id);
}

function exportHistoryCsv() {
  const entries = History.list();
  const lines = [['Title', 'Uploader', 'Date', 'Quality', 'Size (bytes)', 'Status', 'Path']];
  for (const e of entries) {
    lines.push([e.title, e.uploader, e.date_completed, e.quality_label, e.size_bytes, e.status, e.output_path]);
  }
  return lines.map(row => row.map(v => `"${String(v || '').replace(/"/g, '""')}"`).join(',')).join('\n');
}

module.exports = { addHistoryEntry, listHistory, deleteHistoryEntry, exportHistoryCsv };
