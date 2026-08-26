/**
 * Rotating file logger setup.
 */
const fs = require('fs');
const path = require('path');

let configured = false;
let logFilePath = null;

function setupLogger(logDir) {
  if (configured) return;
  fs.mkdirSync(logDir, { recursive: true });
  logFilePath = path.join(logDir, 'app.log');
  configured = true;
}

function getLogger() {
  return {
    info: (msg) => log('INFO', msg),
    warning: (msg) => log('WARNING', msg),
    error: (msg) => log('ERROR', msg)
  };
}

function log(level, message) {
  const timestamp = new Date().toISOString();
  const line = `${timestamp} [${level}] ${message}\n`;
  console.log(line.trim());
  if (logFilePath) {
    try {
      fs.appendFileSync(logFilePath, line);
    } catch (e) { /* ignore */ }
  }
}

module.exports = { setupLogger, getLogger };
