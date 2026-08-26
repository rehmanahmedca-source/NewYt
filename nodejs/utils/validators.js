/**
 * Input validation / sanitization -- security-relevant helpers.
 */
const { URL } = require('url');

const ALLOWED_HOST_FRAGMENTS = ['youtube.com', 'youtu.be', 'music.youtube.com'];

function isValidYoutubeUrl(url) {
  if (!url || typeof url !== 'string') return false;
  try {
    const parsed = new URL(url.trim());
    if (!['http:', 'https:'].includes(parsed.protocol)) return false;
    const host = parsed.hostname.toLowerCase();
    return ALLOWED_HOST_FRAGMENTS.some(frag => host.includes(frag));
  } catch {
    return false;
  }
}

function sanitizeFilename(name) {
  if (!name) return 'file';
  let n = name.replace(/\//g, '_').replace(/\\/g, '_');
  n = n.replace(/[<>:"|?*\x00-\x1f]/g, '_');
  n = n.trim().replace(/^[. ]+|[. ]+$/g, '');
  return n.substring(0, 200) || 'file';
}

module.exports = { isValidYoutubeUrl, sanitizeFilename };
