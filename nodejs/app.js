/**
 * YT Downloader X Pro -- Node.js / Express entry point.
 *
 * Run with:
 *     node app.js
 *
 * Then open http://127.0.0.1:5000 in a browser on the same device.
 */
const path = require('path');
const express = require('express');
const session = require('express-session');
const cookieParser = require('cookie-parser');

const config = require('./config');
const { initDatabase } = require('./models/database');
const { ensureDefaultSettings } = require('./services/settingsService');
const { queueManager } = require('./services/queueManager');
const { setupLogger } = require('./utils/logger');

const homeRoutes = require('./routes/home');
const apiRoutes = require('./routes/api');

function createApp() {
  const app = express();

  // Ensure directories
  const fs = require('fs');
  [config.DEFAULT_DOWNLOAD_FOLDER, config.DEFAULT_TEMP_FOLDER, config.LOG_DIR].forEach(d => {
    fs.mkdirSync(d, { recursive: true });
  });

  setupLogger(config.LOG_DIR);

  // View engine
  app.set('view engine', 'ejs');
  app.set('views', path.join(__dirname, 'views'));

  // Middleware
  app.use(express.json());
  app.use(express.urlencoded({ extended: true }));
  app.use(cookieParser());
  app.use(session({
    secret: config.SECRET_KEY,
    resave: false,
    saveUninitialized: false,
    cookie: { maxAge: null } // session cookie - expires on browser close
  }));
  app.use(express.static(path.join(__dirname, 'public')));

  // Initialize database
  initDatabase();
  ensureDefaultSettings();

  // Routes
  app.use('/', homeRoutes);
  app.use('/api', apiRoutes);

  // Start queue manager
  queueManager.recoverIncompleteTasks();
  queueManager.start();

  return app;
}

const app = createApp();

app.listen(config.PORT, config.HOST, () => {
  console.log(`YT Downloader X Pro running on http://${config.HOST}:${config.PORT}`);
});
