// Fills the home page from data.js: quote form, services, vehicles, steps, safety, testimonials.
/* ============ HOME RENDER ============ */
function renderHome() {
  // quick form
  $('#cities').innerHTML = Object.keys(CITY)
    .map((c) => `<option value="${c}">`)
    .join('');
  $('#qGoods').innerHTML = GOODS.map((g) => `<option>${g}</option>`).join('');
  $('#qVeh').innerHTML = VEH.map(
    (v, i) => `<option value="${i}" ${i === 3 ? 'selected' : ''}>${v.name} · ${kg(v.cap)}</option>`,
  ).join('');
  const d = new Date();
  d.setDate(d.getDate() + 1);
  $('#qDate').value = d.toISOString().slice(0, 10);
  $('#quickForm').addEventListener('submit', (e) => {
    e.preventDefault();
    openPortal('/customer/book');
  });
  setInterval(() => {
    const el = $('#trucksNear');
    el.textContent = 138 + Math.floor(Math.random() * 12);
  }, 3500);

  // services
  $('#svcGrid').innerHTML = SERVICES.map(
    (s, i) => `<button type="button" class="svc rv" aria-expanded="false" data-delay="${i % 4}">
      <span class="ic"><i data-lucide="${s.ic}"></i></span><h3>${s.t}</h3><p>${s.d}</p>
      <div class="detail"><div><ul>${s.li.map((l) => `<li>${l}</li>`).join('')}</ul></div></div>
      <span class="more">Learn more <i data-lucide="chevron-right"></i></span></button>`,
  ).join('');
  $$('.svc').forEach((b) =>
    b.addEventListener('click', () =>
      b.setAttribute('aria-expanded', b.getAttribute('aria-expanded') !== 'true'),
    ),
  );

  // vehicles
  $('#vehTrack').innerHTML = VEH.map(
    (v, i) => `<article class="veh">
      <div class="veh-art">${vehSVG(v, i)}</div>
      <div class="veh-top"><h3>${v.name}</h3><span class="tag">${v.tag}</span></div>
      <div class="veh-specs"><div><small>Max load</small><b>${kg(v.cap)}</b></div><div><small>Dimensions</small><b>${v.dim}</b></div></div>
      <p class="goods"><b class="strong">Best for:</b> ${v.goods}</p>
      <a class="btn btn-dark btn-block" href="${PORTAL_URL}/customer/book">Book This Vehicle <i class="arr" data-lucide="arrow-right"></i></a>
    </article>`,
  ).join('');

  // how
  $('#howGrid').innerHTML = STEPS.map(
    (s, i) =>
      `<div class="step rv" data-delay="${i % 4}"><span class="node"><i data-lucide="${s.ic}"></i><span class="num">${i + 1}</span></span><h3>${s.t}</h3><p>${s.d}</p></div>`,
  ).join('');

  // safety
  $('#featList').innerHTML = SAFETY.map(
    ([ic, t], i) =>
      `<div class="feat rv" data-delay="${i % 2}"><span class="ic"><i data-lucide="${ic}"></i></span><b>${t}</b></div>`,
  ).join('');

  // testimonials
  const card = (t) =>
    `<figure class="tst flush"><div class="stars">${'<i data-lucide="star"></i>'.repeat(5)}</div><q>${t.q}</q><figcaption class="who"><span class="avatar avatar-sm">${t.n
      .split(' ')
      .map((x) => x[0])
      .join('')}</span><div><b>${t.n}</b><small>${t.r}</small></div></figcaption></figure>`;
  $('#tstRow').innerHTML = TST.map(card).join('') + TST.map(card).join('');

  // phones
  renderPhones(0);
  $('#screenTabs').innerHTML = PHONE_SCREENS.map(
    (p, i) => `<button type="button" aria-pressed="${i === 0}" data-scr="${i}">${p.name}</button>`,
  ).join('');
  $$('#screenTabs button').forEach((b) =>
    b.addEventListener('click', () => {
      $$('#screenTabs button').forEach((x) => x.setAttribute('aria-pressed', x === b));
      renderPhones(+b.dataset.scr);
      icons();
    }),
  );

  renderNetwork();
  initRouteLoop();
  initTrackLoop();
  initPayFlow();
}
