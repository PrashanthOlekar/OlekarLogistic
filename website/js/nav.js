// Top navigation: solid background on scroll, login menu, mobile menu and the EN / ಕನ್ನಡ switch.
/* ============ NAV / LANG ============ */
function initNav() {
  const nav = $('#nav');
  const upd = () => nav.classList.toggle('solid', scrollY > 40);
  addEventListener('scroll', upd, { passive: true });
  upd();
  initNav.upd = upd;
  const lb = $('#loginBtn'),
    lm = $('#loginMenu');
  lb.addEventListener('click', (e) => {
    e.stopPropagation();
    lm.hidden = !lm.hidden;
    lb.setAttribute('aria-expanded', !lm.hidden);
  });
  document.addEventListener('click', (e) => {
    if (!e.target.closest('.menu-wrap')) {
      lm.hidden = true;
      lb.setAttribute('aria-expanded', 'false');
    }
  });
  lm.addEventListener('click', () => (lm.hidden = true));
  const bg = $('#burger'),
    mmn = $('#mobileMenu');
  bg.addEventListener('click', () => {
    mmn.hidden = !mmn.hidden;
  });
  mmn.addEventListener('click', () => (mmn.hidden = true));
  const orig = {};
  $$('[data-i18n]').forEach((el) => {
    orig[el.dataset.i18n] = orig[el.dataset.i18n] || el.innerHTML;
  });
  $$('[data-lang]').forEach((b) =>
    b.addEventListener('click', () => {
      const L = b.dataset.lang;
      document.documentElement.lang = L === 'kn' ? 'kn' : 'en';
      $$('[data-lang]').forEach((x) => x.setAttribute('aria-pressed', x === b));
      $$('[data-i18n]').forEach((el) => {
        const k = el.dataset.i18n;
        el.innerHTML = L === 'kn' && I18N.kn[k] ? I18N.kn[k] : orig[k];
      });
      try {
        localStorage.setItem('procargo-lang', L);
      } catch (e) {}
      if (window.ScrollTrigger) setTimeout(() => ScrollTrigger.refresh(), 50);
    }),
  );
  try {
    if (localStorage.getItem('procargo-lang') === 'kn') $('[data-lang="kn"]').click();
  } catch (e) {}
}
