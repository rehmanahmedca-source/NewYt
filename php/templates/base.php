<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>YT Downloader X Pro</title>
<link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css" rel="stylesheet">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.5.1/css/all.min.css">
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/animate.css/4.1.1/animate.min.css">
<link rel="stylesheet" href="/static/css/style.css">
</head>
<body>
<div class="app-shell">
  <nav class="sidebar">
    <div class="sidebar-brand">
      <i class="fa-solid fa-bolt-lightning"></i>
      <span>YT Downloader <b>X Pro</b></span>
    </div>
    <ul class="sidebar-nav">
      <li class="<?= $activePage === 'dashboard' ? 'active' : '' ?>"><a href="/"><i class="fa-solid fa-gauge-high"></i> Dashboard</a></li>
      <li class="<?= $activePage === 'downloads' ? 'active' : '' ?>"><a href="/downloads"><i class="fa-solid fa-download"></i> Downloads &amp; Queue</a></li>
      <li class="<?= $activePage === 'history' ? 'active' : '' ?>"><a href="/history"><i class="fa-solid fa-clock-rotate-left"></i> History</a></li>
      <li class="<?= $activePage === 'settings' ? 'active' : '' ?>"><a href="/settings"><i class="fa-solid fa-sliders"></i> Settings</a></li>
      <li class="<?= $activePage === 'about' ? 'active' : '' ?>"><a href="/about"><i class="fa-solid fa-circle-info"></i> About</a></li>
    </ul>
  </nav>
  <div class="main-area">
    <div class="topbar">
      <button class="btn-topbar-toggle d-lg-none" id="sidebarToggle"><i class="fa-solid fa-bars"></i></button>
      <div class="topbar-title"><?= $pageTitle ?></div>
      <div class="topbar-status"><span class="pulse-dot"></span> Live</div>
    </div>
    <div class="content-area">
      <?php include __DIR__ . '/' . $page . '.php'; ?>
    </div>
  </div>
</div>
<script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/js/bootstrap.bundle.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
<script src="https://cdn.jsdelivr.net/npm/chart.js@4"></script>
<script src="/static/js/app.js"></script>
<?php
$jsFiles = ['dashboard' => 'dashboard.js', 'downloads' => 'downloads.js', 'history' => 'history.js', 'settings' => 'settings.js'];
if (isset($jsFiles[$page])) {
    echo '<script src="/static/js/' . $jsFiles[$page] . '"></script>';
}
?>
</body>
</html>
