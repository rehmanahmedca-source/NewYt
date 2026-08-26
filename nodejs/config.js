const path = require('path');

const BASE_DIR = __dirname;

module.exports = {
  SECRET_KEY: 'change-this-secret-in-production',
  DATABASE_PATH: path.join(BASE_DIR, 'database.db'),

  DEFAULT_DOWNLOAD_FOLDER: path.join(BASE_DIR, 'downloads'),
  DEFAULT_TEMP_FOLDER: path.join(BASE_DIR, 'temp'),
  LOG_DIR: path.join(BASE_DIR, 'logs'),

  DEFAULT_MAX_CONCURRENT: 3,
  DEFAULT_CONCURRENT_FRAGMENTS: 8,
  DEFAULT_MAX_RETRIES: 3,

  HOST: '0.0.0.0',
  PORT: 5000
};
