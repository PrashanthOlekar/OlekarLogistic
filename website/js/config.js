/* ============ CONFIG ============ */
// Where the ProCargo portal (the React app in /frontend) and the API are running.
// Change these lines when they go live, e.g. 'https://portal.procargo.in' and 'https://api.procargo.in/api/v1'.
const PORTAL_URL = 'http://localhost:5173';
const API_URL = 'https://localhost:7001/api/v1';

/** Points every link marked data-portal="/path" at the portal. */
function linkPortal() {
  document.querySelectorAll('[data-portal]').forEach((link) => {
    link.href = PORTAL_URL + link.dataset.portal;
  });
}
