// Downloads & Queue page: fetch video/playlist/channel, select items,
// add to queue, then poll and render the live queue with progress,
// pause/resume/cancel/retry/reorder controls.

let currentEntries = [];
let knownStatuses = {};   // task_id -> last seen status (for completion notifications)

const fetchBtn = document.getElementById("fetchBtn");
const fetchUrl = document.getElementById("fetchUrl");

fetchBtn.addEventListener("click", doFetch);
fetchUrl.addEventListener("keydown", (e) => { if (e.key === "Enter") doFetch(); });

async function doFetch() {
  const url = fetchUrl.value.trim();
  if (!url) { toast("warning", "Paste a link first"); return; }

  fetchBtn.disabled = true;
  fetchBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Fetching...';
  try {
    const result = await apiPost("/api/fetch", { url });
    if (result.error) {
      toast("error", result.error);
      return;
    }
    currentEntries = result.entries.map(e => ({ ...e, selected: true }));
    document.getElementById("fetchResultTitle").textContent =
      `${result.title} (${result.entries.length} item${result.entries.length > 1 ? "s" : ""})`;
    document.getElementById("fetchTypeBadge").innerHTML =
      `<span class="status-badge status-downloading">${result.type}</span>`;
    document.getElementById("fetchResults").classList.remove("d-none");
    renderEntries();
  } catch (e) {
    toast("error", "Network error while fetching");
  } finally {
    fetchBtn.disabled = false;
    fetchBtn.innerHTML = '<i class="fa-solid fa-magnifying-glass"></i> Fetch';
  }
}

function renderEntries(filter = "") {
  const list = document.getElementById("entryList");
  const f = filter.toLowerCase();
  const visible = currentEntries.filter(e => !f || e.title.toLowerCase().includes(f));

  if (!visible.length) {
    list.innerHTML = '<div class="text-muted small">No matching items.</div>';
  } else {
    list.innerHTML = visible.map((e) => `
      <div class="entry-row">
        <input type="checkbox" class="form-check-input entry-checkbox" data-id="${e.id}" ${e.selected ? "checked" : ""}>
        <img src="${e.thumbnail || ''}" onerror="this.style.visibility='hidden'">
        <div class="flex-fill">
          <div class="entry-title">${e.title}</div>
          <div class="entry-meta">${e.uploader || ''} ${e.duration ? '&middot; ' + formatEta(e.duration) : ''}</div>
        </div>
      </div>
    `).join("");
  }

  list.querySelectorAll(".entry-checkbox").forEach(cb => {
    cb.addEventListener("change", () => {
      const entry = currentEntries.find(x => String(x.id) === cb.dataset.id);
      if (entry) entry.selected = cb.checked;
      updateEstimate();
    });
  });
  updateEstimate();
}

function updateEstimate() {
  const selectedCount = currentEntries.filter(e => e.selected).length;
  document.getElementById("estimateLine").textContent =
    `${selectedCount} of ${currentEntries.length} selected for download.`;
}

document.getElementById("entrySearch").addEventListener("input", (e) => renderEntries(e.target.value));
document.getElementById("selectAllBtn").addEventListener("click", () => { currentEntries.forEach(e => e.selected = true); renderEntries(document.getElementById("entrySearch").value); });
document.getElementById("selectNoneBtn").addEventListener("click", () => { currentEntries.forEach(e => e.selected = false); renderEntries(document.getElementById("entrySearch").value); });
document.getElementById("invertBtn").addEventListener("click", () => { currentEntries.forEach(e => e.selected = !e.selected); renderEntries(document.getElementById("entrySearch").value); });

// ---------------------------------------------------------- direct-to-browser download
// Single button, driven by the page-level quality dropdown. The actual
// file transfer is handled entirely by the browser's own native download
// manager (a hidden iframe navigated to /api/direct/download) -- real
// download bar, real notification, real pause/resume where supported.
// The only thing we add is an honest "resolving..." indicator for the
// server-side wait (yt-dlp finding the real CDN link) before that starts:
// we call /api/direct/prepare first, which pays that cost and hands back
// a token; once it responds we flip the card to a green check and
// immediately fire the iframe at the cached, already-resolved token --
// so the real browser download starts with no extra delay.
const dlPanel = document.getElementById("dlProgressPanel");

function makeCard(title, thumbnail) {
  const card = document.createElement("div");
  card.className = "dl-card";
  card.innerHTML = `
    <div class="dl-card-top">
      <img src="${thumbnail || ''}" onerror="this.style.visibility='hidden'">
      <div class="dl-card-body">
        <div class="dl-card-title">${title}</div>
        <div class="dl-card-status"><i class="fa-solid fa-spinner fa-spin"></i> Resolving link&hellip;</div>
      </div>
      <div class="dl-card-icon"></div>
    </div>
    <div class="dl-progress"><div class="dl-progress-bar indeterminate"></div></div>
  `;
  dlPanel.appendChild(card);
  return {
    el: card,
    status: card.querySelector(".dl-card-status"),
    icon: card.querySelector(".dl-card-icon"),
    bar: card.querySelector(".dl-progress-bar"),
  };
}

function setCardReady(card) {
  card.bar.classList.remove("indeterminate");
  card.bar.classList.add("dl-progress-done");
  card.bar.style.width = "100%";
  card.status.innerHTML = `<i class="fa-solid fa-circle-check"></i> Starting in your browser's downloader&hellip;`;
  card.icon.innerHTML = '<i class="fa-solid fa-circle-check dl-check"></i>';
  setTimeout(() => {
    card.el.classList.add("dl-card-fadeout");
    setTimeout(() => card.el.remove(), 500);
  }, 2200);
}

function setCardFailed(card, message) {
  card.bar.classList.remove("indeterminate");
  card.bar.classList.add("dl-progress-failed");
  card.bar.style.width = "100%";
  card.status.innerHTML = `<i class="fa-solid fa-circle-xmark"></i> Failed -- ${message || "could not resolve link"}`;
  card.icon.innerHTML = '<i class="fa-solid fa-circle-xmark dl-fail"></i>';
  setTimeout(() => {
    card.el.classList.add("dl-card-fadeout");
    setTimeout(() => card.el.remove(), 500);
  }, 5000);
}

function fireBrowserDownload(token) {
  const iframe = document.createElement("iframe");
  iframe.style.display = "none";
  iframe.src = `/api/direct/download?token=${encodeURIComponent(token)}`;
  document.body.appendChild(iframe);
  setTimeout(() => iframe.remove(), 60000);
}

async function downloadOne(entry, formatId) {
  const card = makeCard(entry.title, entry.thumbnail);

  let data;
  try {
    const resp = await apiPost("/api/direct/prepare", { url: entry.url, format_id: formatId, title: entry.title });
    data = resp;
  } catch (e) {
    setCardFailed(card, "network error");
    return;
  }

  if (!data || data.error || !data.token) {
    setCardFailed(card, data && data.error);
    return;
  }

  setCardReady(card);
  fireBrowserDownload(data.token);
}

const downloadBtn = document.getElementById("downloadSelectedBtn");

downloadBtn.addEventListener("click", async () => {
  const quality = document.getElementById("qualitySelect");
  const selected = currentEntries.filter(e => e.selected);
  if (!selected.length) { toast("warning", "Select at least one item"); return; }

  downloadBtn.disabled = true;
  const originalLabel = downloadBtn.innerHTML;
  downloadBtn.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Working\u2026';

  for (const entry of selected) {
    await downloadOne(entry, quality.value);
  }

  downloadBtn.disabled = false;
  downloadBtn.innerHTML = originalLabel;
});

// ---------------------------------------------------------------- queue
async function refreshQueue() {
  let tasks;
  try { tasks = await apiGet("/api/tasks"); } catch (e) { return; }

  const list = document.getElementById("queueList");
  const summary = document.getElementById("queueSummary");
  const activeCount = tasks.filter(t => t.status === "downloading").length;
  const queuedCount = tasks.filter(t => t.status === "queued").length;
  summary.textContent = `${activeCount} active, ${queuedCount} queued, ${tasks.length} total`;

  if (!tasks.length) {
    list.innerHTML = '<div class="text-muted small">No downloads yet.</div>';
    return;
  }

  list.innerHTML = tasks.map(renderTaskCard).join("");

  tasks.forEach(t => {
    const prevStatus = knownStatuses[t.id];
    if (prevStatus && prevStatus !== t.status) {
      if (t.status === "completed") {
        toast("success", `Ready on server: ${t.title} -- click Download to save it to your device`);
        notifyBrowser("Download ready on server", `${t.title} -- click to save to your device`, () => downloadTaskFile(t.id));
      } else if (t.status === "failed") {
        toast("error", `Failed: ${t.title}`);
        notifyBrowser("Download failed", t.title);
      }
    }
    knownStatuses[t.id] = t.status;
  });

  bindQueueActions();
}

function renderTaskCard(t) {
  const pct = Math.round(t.progress || 0);
  const canPause = t.status === "downloading" || t.status === "queued";
  const canResume = t.status === "paused";
  const canCancel = ["downloading", "queued", "paused"].includes(t.status);
  const canRetry = ["failed", "cancelled"].includes(t.status);
  const canRemove = ["completed", "failed", "cancelled"].includes(t.status);
  const canDownload = t.status === "completed";

  return `
  <div class="queue-card" data-id="${t.id}">
    <div class="queue-top">
      <img src="${t.thumbnail || ''}" onerror="this.style.visibility='hidden'">
      <div class="queue-body">
        <div class="queue-title">${t.title}</div>
        <div class="queue-meta">${t.quality_label || ''} &middot; <span class="status-badge status-${t.status}">${t.status}</span></div>
      </div>
    </div>
    <div class="progress mt-2"><div class="progress-bar" style="width:${pct}%"></div></div>
    <div class="queue-meta mt-1">
      ${pct}% &middot; ${formatSpeed(t.speed)} &middot; ETA ${formatEta(t.eta_seconds)}
      &middot; ${formatBytes(t.downloaded_bytes)} / ${formatBytes(t.total_bytes)}
      ${t.error_message ? `<br><span class="text-danger">${t.error_message}</span>` : ""}
    </div>
    <div class="queue-actions">
      ${canDownload ? `<button data-download-id="${t.id}"><i class="fa-solid fa-download"></i> Download to your device</button>` : ""}
      ${canPause ? `<button data-action="pause" data-id="${t.id}"><i class="fa-solid fa-pause"></i> Pause</button>` : ""}
      ${canResume ? `<button data-action="resume" data-id="${t.id}"><i class="fa-solid fa-play"></i> Resume</button>` : ""}
      ${canCancel ? `<button data-action="cancel" data-id="${t.id}"><i class="fa-solid fa-xmark"></i> Cancel</button>` : ""}
      ${canRetry ? `<button data-action="retry" data-id="${t.id}"><i class="fa-solid fa-rotate-right"></i> Retry</button>` : ""}
      ${t.status === "queued" ? `<button data-action="priority_up" data-id="${t.id}"><i class="fa-solid fa-arrow-up"></i></button>` : ""}
      ${t.status === "queued" ? `<button data-action="priority_down" data-id="${t.id}"><i class="fa-solid fa-arrow-down"></i></button>` : ""}
      ${canRemove ? `<button data-action="remove" data-id="${t.id}"><i class="fa-solid fa-trash"></i> Remove</button>` : ""}
    </div>
  </div>`;
}

function bindQueueActions() {
  document.querySelectorAll("#queueList [data-action]").forEach(btn => {
    btn.addEventListener("click", async () => {
      await apiPost(`/api/tasks/${btn.dataset.id}/${btn.dataset.action}`, {});
      refreshQueue();
    });
  });
  document.querySelectorAll("#queueList [data-download-id]").forEach(btn => {
    btn.addEventListener("click", () => downloadTaskFile(btn.dataset.downloadId));
  });
}

refreshQueue();
setInterval(refreshQueue, 1500);
