// Small shared helpers: $ / $$ (querySelector), number formats, icons(), toast().
/* ============ HELPERS ============ */
const $ = (s, r = document) => r.querySelector(s),
  $$ = (s, r = document) => [...r.querySelectorAll(s)];
const RM = matchMedia('(prefers-reduced-motion: reduce)').matches;
const inr = (n) => '₹ ' + Math.round(n).toLocaleString('en-IN');
const kg = (n) => n.toLocaleString('en-IN') + ' kg';
const icons = () => {
  try {
    window.lucide && lucide.createIcons();
  } catch (e) {}
};
function toast(msg) {
  $('#toastText').textContent = msg;
  const t = $('#toast');
  t.classList.add('show');
  clearTimeout(toast._t);
  toast._t = setTimeout(() => t.classList.remove('show'), 2800);
}
document.addEventListener('click', (e) => {
  const b = e.target.closest('[data-toast]');
  if (b) {
    e.preventDefault();
    toast(b.dataset.toast);
  }
});
