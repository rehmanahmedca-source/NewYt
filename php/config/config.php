<?php
/**
 * Application configuration.
 */
define('BASE_DIR', dirname(__DIR__));
define('SECRET_KEY', 'change-this-secret-in-production');
define('DATABASE_PATH', BASE_DIR . '/database.db');

define('DEFAULT_DOWNLOAD_FOLDER', BASE_DIR . '/downloads');
define('DEFAULT_TEMP_FOLDER', BASE_DIR . '/temp');
define('LOG_DIR', BASE_DIR . '/logs');

define('DEFAULT_MAX_CONCURRENT', 3);
define('DEFAULT_CONCURRENT_FRAGMENTS', 8);
define('DEFAULT_MAX_RETRIES', 3);

define('APP_HOST', '0.0.0.0');
define('APP_PORT', 5000);
