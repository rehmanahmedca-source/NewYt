<%
' Database connection and initialization using ADO + SQLite ODBC driver
Function GetDbConnection()
    Dim conn
    Set conn = Server.CreateObject("ADODB.Connection")
    conn.Open "Driver={SQLite3 ODBC Driver};Database=" & DATABASE_PATH & ";"
    Set GetDbConnection = conn
End Function

Sub InitializeDatabase()
    Dim conn, sql
    Set conn = GetDbConnection()

    sql = "CREATE TABLE IF NOT EXISTS downloads (" & _
          "id TEXT PRIMARY KEY, url TEXT NOT NULL, title TEXT DEFAULT 'Untitled', " & _
          "thumbnail TEXT DEFAULT '', uploader TEXT DEFAULT '', format_id TEXT DEFAULT 'best', " & _
          "quality_label TEXT DEFAULT 'Best', status TEXT DEFAULT 'queued', " & _
          "speed REAL DEFAULT 0.0, eta_seconds INTEGER DEFAULT 0, " & _
          "downloaded_bytes INTEGER DEFAULT 0, total_bytes INTEGER DEFAULT 0, " & _
          "progress REAL DEFAULT 0.0, output_file TEXT DEFAULT '', " & _
          "error_message TEXT DEFAULT '', resume_supported INTEGER DEFAULT 1, " & _
          "retry_count INTEGER DEFAULT 0, priority INTEGER DEFAULT 0, " & _
          "pause_flag INTEGER DEFAULT 0, cancel_flag INTEGER DEFAULT 0, " & _
          "session_id TEXT DEFAULT '', created_time REAL, finished_time REAL)"
    conn.Execute sql

    sql = "CREATE TABLE IF NOT EXISTS history (" & _
          "id INTEGER PRIMARY KEY AUTOINCREMENT, title TEXT DEFAULT 'Untitled', " & _
          "thumbnail TEXT DEFAULT '', uploader TEXT DEFAULT '', date_completed REAL, " & _
          "duration_seconds INTEGER DEFAULT 0, format_id TEXT DEFAULT '', " & _
          "quality_label TEXT DEFAULT '', size_bytes INTEGER DEFAULT 0, " & _
          "output_path TEXT DEFAULT '', status TEXT DEFAULT 'completed', session_id TEXT DEFAULT '')"
    conn.Execute sql

    sql = "CREATE TABLE IF NOT EXISTS settings (" & _
          "id INTEGER PRIMARY KEY DEFAULT 1, download_folder TEXT DEFAULT '', " & _
          "temp_folder TEXT DEFAULT '', filename_template TEXT DEFAULT '%(title)s.%(ext)s', " & _
          "max_concurrent INTEGER DEFAULT 3, concurrent_fragments INTEGER DEFAULT 8, " & _
          "max_retries INTEGER DEFAULT 3, auto_resume INTEGER DEFAULT 1, " & _
          "speed_limit_kbps INTEGER DEFAULT 0, default_quality TEXT DEFAULT 'best', " & _
          "theme TEXT DEFAULT 'dark', language TEXT DEFAULT 'en', " & _
          "proxy TEXT DEFAULT '', cookies_path TEXT DEFAULT '', ffmpeg_path TEXT DEFAULT '', " & _
          "embed_metadata INTEGER DEFAULT 0, embed_thumbnail INTEGER DEFAULT 0, " & _
          "embed_subtitles INTEGER DEFAULT 0, sponsorblock INTEGER DEFAULT 0)"
    conn.Execute sql

    conn.Execute "CREATE INDEX IF NOT EXISTS idx_downloads_session ON downloads(session_id)"
    conn.Execute "CREATE INDEX IF NOT EXISTS idx_history_session ON history(session_id)"

    conn.Close
    Set conn = Nothing
End Sub
%>
