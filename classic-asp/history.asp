<%@ Language="VBScript" %>
<!-- #include file="includes/config.asp" -->
<!-- #include file="includes/utils.asp" -->
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>YT Downloader X Pro - History</title>
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
      <li class="active"><a href="history.asp"><i class="fa-solid fa-clock-rotate-left"></i> History</a></li>
      <li><a href="settings.asp"><i class="fa-solid fa-sliders"></i> Settings</a></li>
      <li><a href="about.asp"><i class="fa-solid fa-circle-info"></i> About</a></li>
    </ul>
  </nav>
  <div class="main-area">
    <div class="topbar">
      <button class="btn-topbar-toggle d-lg-none" id="sidebarToggle"><i class="fa-solid fa-bars"></i></button>
      <div class="topbar-title">History</div>
      <div class="topbar-status"><span class="pulse-dot"></span> Live</div>
    </div>
    <div class="content-area">
      <div class="glass-panel mb-3">
        <div class="d-flex gap-2 flex-wrap justify-content-between">
          <input type="text" id="historySearch" class="form-control" style="max-width:300px" placeholder="Search history...">
          <a class="btn btn-outline-light" href="api_export_csv.asp"><i class="fa-solid fa-file-csv"></i> Export CSV</a>
        </div>
      </div>
      <div class="glass-panel"><div id="historyList" class="history-list"><div class="text-muted small">Loading...</div></div></div>
    </div>
  </div>
</div>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4"></script>
<script src="wwwroot/js/app.js"></script>
<script src="wwwroot/js/history.js"></script>
</body>
</html>
