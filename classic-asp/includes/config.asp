<%
' YT Downloader X Pro - Classic ASP Configuration
Const BASE_DIR = "C:\inetpub\wwwroot\ytdownloader"
Const SECRET_KEY = "change-this-secret-in-production"
Const DATABASE_PATH = BASE_DIR & "\database\database.db"
Const DEFAULT_DOWNLOAD_FOLDER = BASE_DIR & "\downloads"
Const DEFAULT_TEMP_FOLDER = BASE_DIR & "\temp"
Const LOG_DIR = BASE_DIR & "\logs"
Const DEFAULT_MAX_CONCURRENT = 3
Const DEFAULT_CONCURRENT_FRAGMENTS = 8
Const DEFAULT_MAX_RETRIES = 3
Const APP_PORT = 5000
%>
