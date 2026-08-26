/**
 * Small shared helpers used across services/routes.
 */
const { v4: uuidv4 } = require('uuid');
const os = require('os');
const fs = require('fs');

function newId() {
  return uuidv4().replace(/-/g, '').substring(0, 12);
}

function getDiskUsage(dirPath) {
  try {
    // Node doesn't have a direct disk usage API; use a simple estimate
    const stats = fs.statfsSync(dirPath || '.');
    const total = stats.bsize * stats.blocks;
    const free = stats.bsize * stats.bfree;
    const used = total - free;
    return { used, total, free };
  } catch {
    return { used: 0, total: 0, free: 0 };
  }
}

function detectContentType(url) {
  const u = url.toLowerCase();
  if (u.includes('playlist') || u.includes('list=')) return 'playlist';
  if (u.includes('/shorts/')) return 'shorts';
  if (u.includes('/channel/') || u.includes('/@') || u.includes('/c/')) return 'channel';
  if (u.includes('live')) return 'live';
  return 'video';
}

module.exports = { newId, getDiskUsage, detectContentType };
