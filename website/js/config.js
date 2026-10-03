/* ============ CONFIG ============ */
// Where the ProCargo portal (the React app in /frontend) is running.
// Change this one line when the portal moves, e.g. 'https://portal.procargo.in'.
const PORTAL_URL = 'http://localhost:5173';

/** Points every link marked data-portal="/path" at the portal. */
function linkPortal() {
  document.querySelectorAll('[data-portal]').forEach((link) => {
    link.href = PORTAL_URL + link.dataset.portal;
  });
}

function openPortal(path) {
  location.href = PORTAL_URL + path;
}
