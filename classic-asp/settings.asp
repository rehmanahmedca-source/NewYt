<%@ Language="VBScript" %>
<!-- #include file="includes/config.asp" -->
<!-- #include file="includes/database.asp" -->
<!-- #include file="includes/utils.asp" -->
<%
Dim conn, rs, settings
Set conn = GetDbConnection()
Set rs = conn.Execute("SELECT * FROM settings WHERE id = 1")
If Not rs.EOF Then
    Set settings = rs
End If
%>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>YT Downloader X Pro - Settings</title>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/animate.css/4.1.1/animate.min.css">
<link rel="stylesheet" href="wwwroot/css/style.css">
</head>
<body>
<div class="app-shell">
  <nav class="sidebar">
    <div class="sidebar-brand"><i class="fa-solid fa-bolt-lightning"></i><span>YT Downloader <b>X Pro</b></span></div>
    <ul class="sidebar-nav">
      <li><a href="default.asp"><i class="fa-solid fa-gauge-high"></i> Dashboard</a></li>
      <li><a href="downloads.asp"><i class="fa-solid fa-download"></i> Downloads &amp; Queue</a></li>
      <li><a href="history.asp"><i class="fa-solid fa-clock-rotate-left"></i> History</a></li>
      <li class="active"><a href="settings.asp"><i class="fa-solid fa-sliders"></i> Settings</a></li>
      <li><a href="about.asp"><i class="fa-solid fa-circle-info"></i> About</a></li>
    </ul>
  </nav>
  <div class="main-area">
    <div class="topbar">
      <button class="btn-topbar-toggle d-lg-none" id="sidebarToggle"><i class="fa-solid fa-bars"></i></button>
      <div class="topbar-title">Settings</div>
      <div class="topbar-status"><span class="pulse-dot"></span> Live</div>
    </div>
    <div class="content-area">
      <div class="glass-panel">
        <form id="settingsForm">
          <div class="row g-3">
            <div class="col-12 col-md-6"><label class="form-label">Download Folder</label><input type="text" class="form-control" name="download_folder" value="<%=settings("download_folder")%>"></div>
            <div class="col-12 col-md-6"><label class="form-label">Temp Folder</label><input type="text" class="form-control" name="temp_folder" value="<%=settings("temp_folder")%>"></div>
            <div class="col-6 col-md-3"><label class="form-label">Max Concurrent</label><input type="number" min="1" max="10" class="form-control" name="max_concurrent" value="<%=settings("max_concurrent")%>"></div>
            <div class="col-6 col-md-3"><label class="form-label">Concurrent Fragments</label><input type="number" min="1" max="32" class="form-control" name="concurrent_fragments" value="<%=settings("concurrent_fragments")%>"></div>
            <div class="col-6 col-md-3"><label class="form-label">Max Retries</label><input type="number" min="0" max="10" class="form-control" name="max_retries" value="<%=settings("max_retries")%>"></div>
            <div class="col-6 col-md-3"><label class="form-label">Speed Limit (KB/s)</label><input type="number" min="0" class="form-control" name="speed_limit_kbps" value="<%=settings("speed_limit_kbps")%>"></div>
            <div class="col-12 col-md-6"><label class="form-label">Proxy</label><input type="text" class="form-control" name="proxy" value="<%=settings("proxy")%>"></div>
            <div class="col-12 col-md-6"><label class="form-label">FFmpeg Path</label><input type="text" class="form-control" name="ffmpeg_path" value="<%=settings("ffmpeg_path")%>"></div>
          </div>
          <button type="submit" class="btn btn-neon mt-3"><i class="fa-solid fa-floppy-disk"></i> Save Settings</button>
        </form>
      </div>
    </div>
  </div>
</div>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4"></script>
<script src="wwwroot/js/app.js"></script>
<script src="wwwroot/js/settings.js"></script>
</body>
</html>
