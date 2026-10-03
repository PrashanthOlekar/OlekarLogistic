/* ============ START ============ */
// Every other script only defines things; this file runs them once the page has loaded.
renderHome();
icons();
initNav();
linkPortal();
initReveal();
if (document.readyState === 'complete') initGSAP();
else addEventListener('load', initGSAP);
addEventListener('resize', () => {
  if (HOW && !window.ScrollTrigger) {
    HOW = drawHowLine();
  }
});
