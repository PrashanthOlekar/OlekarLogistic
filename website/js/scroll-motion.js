// Scroll effects: reveal-on-scroll, counters and GSAP pinning (skipped for reduced motion).
/* ============ MOTION ============ */
function initReveal() {
  const io = new IntersectionObserver(
    (es) =>
      es.forEach((e) => {
        if (e.isIntersecting) {
          e.target.classList.add('play');
          io.unobserve(e.target);
        }
      }),
    { threshold: 0.12, rootMargin: '0px 0px -40px 0px' },
  );
  $$('.rv').forEach((el) => {
    if (el.getBoundingClientRect().top < innerHeight) return;
    io.observe(el);
  });
  const cio = new IntersectionObserver(
    (es) =>
      es.forEach((e) => {
        if (!e.isIntersecting) return;
        cio.unobserve(e.target);
        $$('[data-count]', e.target).forEach((el) => {
          const n = +el.dataset.count;
          if (RM) {
            el.textContent = n.toLocaleString('en-IN');
            return;
          }
          const t0 = performance.now();
          const tick = (t) => {
            const p = Math.min(1, (t - t0) / 1800),
              v = Math.round(n * (1 - Math.pow(1 - p, 4)));
            el.textContent = v.toLocaleString('en-IN');
            if (p < 1) requestAnimationFrame(tick);
          };
          requestAnimationFrame(tick);
        });
      }),
    { threshold: 0.35 },
  );
  $$('[data-count]').forEach((el) => (el.textContent = (+el.dataset.count).toLocaleString('en-IN')));
  cio.observe($('#stats'));
  // mobile how steps light via reveal
  const sio = new IntersectionObserver(
    (es) =>
      es.forEach((e) => {
        if (e.isIntersecting && !HOW) e.target.classList.add('on');
      }),
    { threshold: 0.8 },
  );
  $$('.step').forEach((s) => sio.observe(s));
}
function initGSAP() {
  if (!window.gsap || !window.ScrollTrigger || RM) {
    HOW = drawHowLine();
    if (HOW) {
      const f = () => {
        const b = $('#howWrap').getBoundingClientRect();
        howProgress(Math.max(0, Math.min(1, (innerHeight * 0.75 - b.top) / b.height)));
      };
      addEventListener('scroll', f, { passive: true });
      f();
    }
    vehicleProgressNative();
    return;
  }
  gsap.registerPlugin(ScrollTrigger);
  gsap.to('#heroTruck', {
    x: 520,
    ease: 'none',
    scrollTrigger: { trigger: '.hero', start: 'top top', end: 'bottom top', scrub: 0.6 },
  });
  gsap.to('#hillsFar', {
    x: -120,
    ease: 'none',
    scrollTrigger: { trigger: '.hero', start: 'top top', end: 'bottom top', scrub: true },
  });
  gsap.to('#hillsNear', {
    x: -240,
    ease: 'none',
    scrollTrigger: { trigger: '.hero', start: 'top top', end: 'bottom top', scrub: true },
  });
  gsap.to('.hero-inner', {
    y: 80,
    opacity: 0.25,
    ease: 'none',
    scrollTrigger: { trigger: '.hero', start: 'top top', end: 'bottom top', scrub: true },
  });
  gsap.from('.shield-art svg', {
    rotate: -8,
    scale: 0.9,
    scrollTrigger: { trigger: '#safety', start: 'top 80%', end: 'center center', scrub: 1 },
  });

  const mm = gsap.matchMedia();
  mm.add('(min-width: 901px)', () => {
    const track = $('#vehTrack');
    track.classList.add('gsap-on');
    const dist = () => track.scrollWidth - innerWidth + 32;
    const tw = gsap.to(track, {
      x: () => -dist(),
      ease: 'none',
      scrollTrigger: {
        trigger: '#vehPin',
        start: 'center center',
        end: () => '+=' + dist(),
        pin: true,
        scrub: 0.8,
        invalidateOnRefresh: true,
        anticipatePin: 1,
        onUpdate: (s) => ($('#vehBar').style.width = s.progress * 100 + '%'),
      },
    });
    HOW = drawHowLine();
    const st = ScrollTrigger.create({
      trigger: '#howWrap',
      start: 'top 75%',
      end: 'bottom 55%',
      scrub: 0.5,
      onUpdate: (s) => howProgress(s.progress),
      onRefresh: () => {
        HOW = drawHowLine();
      },
    });
    return () => {
      track.classList.remove('gsap-on');
      gsap.set(track, { x: 0 });
      HOW = null;
    };
  });
  mm.add('(max-width: 900px)', () => {
    vehicleProgressNative();
  });
}
function vehicleProgressNative() {
  const t = $('#vehTrack');
  const f = () => {
    $('#vehBar').style.width = (t.scrollLeft / Math.max(1, t.scrollWidth - t.clientWidth)) * 100 + '%';
  };
  t.addEventListener('scroll', f, { passive: true });
  f();
}
