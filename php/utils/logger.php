<?php
/**
 * Rotating file logger.
 */
function setup_logger($logDir) {
    if (!is_dir($logDir)) mkdir($logDir, 0755, true);
}

function get_logger() {
    return new class {
        public function info($msg) { $this->log('INFO', $msg); }
        public function warning($msg) { $this->log('WARNING', $msg); }
        public function error($msg) { $this->log('ERROR', $msg); }
        private function log($level, $msg) {
            $line = date('Y-m-d H:i:s') . " [$level] $msg\n";
            error_log(trim($line));
            @file_put_contents(LOG_DIR . '/app.log', $line, FILE_APPEND);
        }
    };
}
