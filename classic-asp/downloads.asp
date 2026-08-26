<%@ Language="VBScript" %>
<!-- #include file="includes/config.asp" -->
<!-- #include file="includes/utils.asp" -->
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>YT Downloader X Pro - Downloads</title>
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
      <li class="active"><a href="downloads.asp"><i class="fa-solid fa-download"></i> Downloads &amp; Queue</a></li>
      <li><a href="history.asp"><i class="fa-solid fa-clock-rotate-left"></i> History</a></li>
      <li><a href="settings.asp"><i class="fa-solid fa-sliders"></i> Settings</a></li>
      <li><a href="about.asp"><i class="fa-solid fa-circle-info"></i> About</a></li>
    </ul>
  </nav>
  <div class="main-area">
    <div class="topbar">
      <button class="btn-topbar-toggle d-lg-none" id="sidebarToggle"><i class="fa-solid fa-bars"></i></button>
      <div class="topbar-title">Downloads &amp; Queue</div>
      <div class="topbar-status"><span class="pulse-dot"></span> Live</div>
    </div>
    <div class="content-area">
      <div class="glass-panel mb-3">
        <h6>Fetch a Video, Playlist, or Channel</h6>
        <div class="input-group">
          <input type="text" id="fetchUrl" class="form-control" placeholder="Paste a YouTube link">
          <button class="btn btn-neon" id="fetchBtn"><i class="fa-solid fa-magnifying-glass"></i> Fetch</button>
        </div>
      </div>
      <div id="fetchResults" class="glass-panel mb-3 d-none">
        <div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-2">
          <h6 class="mb-0" id="fetchResultTitle">Results</h6>
          <div class="d-flex gap-2 flex-wrap">
            <select id="qualitySelect" class="form-select form-select-sm" style="width:150px">
              <option value="bestvideo+bestaudio/best">Best Quality</option>
              <option value="bestvideo[height<=1080]+bestaudio/best[height<=1080]">1080p</option>
              <option value="bestvideo[height<=720]+bestaudio/best[height<=720]">720p</option>
              <option value="bestvideo[height<=480]+bestaudio/best[height<=480]">480p</option>
              <option value="bestaudio/best">Audio Only (MP3)</option>
            </select>
            <button class="btn btn-sm btn-outline-light" id="selectAllBtn">Select All</button>
          </div>
        </div>
        <div id="entryList" class="entry-list"></div>
        <div class="mt-3 text-end"><button class="btn btn-neon" id="downloadSelectedBtn"><i class="fa-solid fa-download"></i> Download</button></div>
      </div>
      <div class="glass-panel">
        <h6 class="mb-0">Queue</h6>
        <div id="queueList" class="queue-list"><div class="text-muted small">No downloads yet.</div></div>
      </div>
      <div id="dlProgressPanel" class="dl-progress-panel"></div>
    </div>
  </div>
</div>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4"></script>
<script src="wwwroot/js/app.js"></script>
<script src="wwwroot/js/downloads.js"></script>
</body>
</html>
