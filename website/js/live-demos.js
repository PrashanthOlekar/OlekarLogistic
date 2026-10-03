// Looping demos: the live route ticker, the tracking map and the payment flow cards.
/* path helpers */
function pathMarkers(path, group, list, dark) {
  const L = path.getTotalLength();
  let s = '';
  list.forEach(([name, f], i) => {
    const p = path.getPointAtLength(L * f);
    const right = p.x < 500;
    s += `<circle cx="${p.x}" cy="${p.y}" r="${dark ? 9 : 8}" fill="${dark ? '#0A1A3A' : '#fff'}" stroke="${dark ? '#fff' : '#0A1A3A'}" stroke-width="3" data-f="${f}"/>
       <text x="${p.x + (right ? 16 : -16)}" y="${p.y + 5}" text-anchor="${right ? 'start' : 'end'}" font-size="${dark ? 15 : 14}" font-weight="700" fill="${dark ? '#fff' : '#0A1A3A'}" paint-order="stroke" stroke="${dark ? '#0C1D44' : '#E9EEF5'}" stroke-width="4">${name}</text>`;
  });
  group.innerHTML = s;
  return L;
}
function initRouteLoop() {
  const path = $('#rPath'),
    done = $('#rDone'),
    truck = $('#rTruck');
  const L = pathMarkers(
    path,
    $('#rCities'),
    [
      ['Bengaluru', 0],
      ['Hubballi', 0.417],
      ['Pune', 0.848],
      ['Mumbai', 1],
    ],
    true,
  );
  done.style.strokeDasharray = L;
  const towns = [
    [0, 'Bengaluru'],
    [0.05, 'Tumakuru'],
    [0.18, 'Chitradurga'],
    [0.26, 'Davanagere'],
    [0.36, 'Haveri'],
    [0.417, 'Hubballi'],
    [0.5, 'Belagavi'],
    [0.6, 'Kolhapur'],
    [0.73, 'Satara'],
    [0.848, 'Pune'],
    [0.92, 'Lonavala'],
    [0.985, 'Mumbai'],
  ];
  let t0 = performance.now();
  const D = 26000;
  function frame(now) {
    let p = ((now - t0) % (D + 2500)) / D;
    const arrived = p >= 1;
    p = Math.min(1, p);
    const pt = path.getPointAtLength(L * p);
    truck.setAttribute('transform', `translate(${pt.x} ${pt.y})`);
    done.style.strokeDashoffset = L * (1 - p);
    let near = towns[0][1];
    for (const [f, n] of towns) if (p >= f - 0.02) near = n;
    $('#rLoc').textContent = arrived
      ? 'Mumbai, Bhiwandi hub'
      : p < 0.02
        ? 'Departed Bengaluru'
        : 'Near ' + near;
    const h = 18 * (1 - p);
    $('#rEta').textContent = arrived
      ? 'Delivered'
      : `${Math.floor(h)} h ${String(Math.round((h % 1) * 60)).padStart(2, '0')} m`;
    $('#rBar').style.width = (p * 100).toFixed(1) + '%';
    $('#rStatus').textContent = arrived ? 'Delivered' : 'In Transit';
    if (!RM) requestAnimationFrame(frame);
  }
  if (RM) {
    const pt = path.getPointAtLength(L * 0.42);
    truck.setAttribute('transform', `translate(${pt.x} ${pt.y})`);
    done.style.strokeDashoffset = L * 0.58;
    $('#rBar').style.width = '42%';
    $('#rLoc').textContent = 'Hubballi';
    $('#rEta').textContent = '10 h 26 m';
  } else requestAnimationFrame(frame);
  setInterval(() => ($('#rSpeed').textContent = 56 + Math.round(Math.random() * 14) + ' km/h'), 1800);
}
function initTrackLoop() {
  const path = $('#tPath'),
    done = $('#tDone'),
    truck = $('#tTruck');
  const stops = [
    ['Bengaluru', 0],
    ['Tumakuru', 0.17],
    ['Chitradurga', 0.49],
    ['Davanagere', 0.65],
    ['Hubballi', 1],
  ];
  const L = pathMarkers(path, $('#tCities'), stops, false);
  done.style.strokeDasharray = L;
  const TL = [
    ['Pickup confirmed', '06:10'],
    ['Truck assigned', '06:24'],
    ['Goods loaded', '07:05'],
    ['In transit', ''],
    ['Reached destination', ''],
    ['Delivery confirmed', ''],
  ];
  const KM = 410,
    D = 36000;
  let t0 = performance.now();
  function draw(p, phase) {
    const pt = path.getPointAtLength(L * p),
      ah = path.getPointAtLength(Math.max(0, L * p - 2));
    truck.setAttribute('transform', `translate(${pt.x} ${pt.y})`);
    done.style.strokeDashoffset = L * (1 - p);
    const rem = Math.round(KM * (1 - p));
    $('#tRem').textContent = rem + ' km';
    const h = rem / 58;
    $('#tEta').textContent =
      phase >= 4 ? 'Arrived' : `${Math.floor(h)} h ${String(Math.round((h % 1) * 60)).padStart(2, '0')} m`;
    let loc = 'Bengaluru';
    for (const [n, f] of stops) if (p >= f - 0.03) loc = n;
    $('#tLoc').textContent = phase >= 4 ? 'Hubballi' : p > 0.03 && loc !== 'Hubballi' ? `Past ${loc}` : loc;
    $('#tPct').textContent = Math.round(p * 100) + '%';
    $('#tBar').style.width = p * 100 + '%';
    if (draw.last === phase) return;
    draw.last = phase;
    $('#tTimeline').innerHTML = TL.map(([t, tm], i) => {
      const st = i < 3 || i < phase ? 'done' : i === Math.max(3, phase) ? 'now' : '';
      return `<li class="${st}"><span class="dot">${st === 'done' ? '<svg viewBox="0 0 24 24" width="13" height="13" fill="none" stroke="currentColor" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round"><path d="M20 6 9 17l-5-5"/></svg>' : ''}</span><b>${t}</b><time>${tm || (st === 'done' ? 'now' : '')}</time></li>`;
    }).join('');
  }
  let lastPhase = -1;
  function frame(now) {
    const c = (now - t0) % (D + 5000);
    let p = Math.min(1, c / D),
      phase = 3;
    if (c > D) phase = 4;
    if (c > D + 2500) phase = 6;
    draw(p, phase);
    requestAnimationFrame(frame);
  }
  if (RM) draw(0.49, 3);
  else requestAnimationFrame(frame);
  setInterval(() => ($('#tSpeed').textContent = 52 + Math.round(Math.random() * 12) + ' km/h'), 2000);
}
function initPayFlow() {
  const cards = $$('#payFlow .pf');
  let i = 0;
  if (RM) {
    cards.forEach((c) => c.classList.add('lit'));
    return;
  }
  setInterval(() => {
    cards.forEach((c, j) => c.classList.toggle('lit', j <= i));
    i = (i + 1) % (cards.length + 1);
  }, 1100);
}
