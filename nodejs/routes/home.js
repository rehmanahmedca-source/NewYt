/**
 * Page routes -- render EJS templates. All data is loaded by JS via
 * the /api endpoints, so these views are mostly just shells.
 */
const express = require('express');
const router = express.Router();

router.get('/', (req, res) => {
  res.render('dashboard', { activePage: 'dashboard' });
});

router.get('/downloads', (req, res) => {
  res.render('downloads', { activePage: 'downloads' });
});

router.get('/history', (req, res) => {
  res.render('history', { activePage: 'history' });
});

router.get('/settings', (req, res) => {
  res.render('settings', { activePage: 'settings' });
});

router.get('/about', (req, res) => {
  res.render('about', { activePage: 'about' });
});

module.exports = router;
