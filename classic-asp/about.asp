<%@ Language="VBScript" %>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>YT Downloader X Pro - About</title>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css">
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
      <li><a href="settings.asp"><i class="fa-solid fa-sliders"></i> Settings</a></li>
      <li class="active"><a href="about.asp"><i class="fa-solid fa-circle-info"></i> About</a></li>
    </ul>
  </nav>
  <div class="main-area">
    <div class="topbar">
      <div class="topbar-title">About</div>
      <div class="topbar-status"><span class="pulse-dot"></span> Live</div>
    </div>
    <div class="content-area">
      <div class="glass-panel">
        <h5><i class="fa-solid fa-bolt-lightning"></i> YT Downloader X Pro</h5>
        <p class="text-muted">A self-hosted YouTube download manager built with Classic ASP (VBScript) + SQLite + yt-dlp.</p>
        <ul class="text-muted">
          <li>Fetch single videos, playlists, and channels</li>
          <li>Full format list per video</li>
          <li>Parallel queue with pause / resume / cancel / retry</li>
          <li>Dashboard statistics, history with CSV export</li>
        </ul>
        <p class="small text-muted">Built with Classic ASP, ADO + SQLite ODBC, yt-dlp, Bootstrap 5, Chart.js, SweetAlert2.</p>
      </div>
    </div>
  </div>
</div>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
</body>
</html>
