// The India map in the "network" section, with lanes from Bengaluru and a state filter.
/* network map */
function renderNetwork() {
  const P = (lat, lon) => [(lon - 68) * 40, (23 - lat) * 40];
  const coast = [
    [23, 68.6],
    [22.3, 69.2],
    [21.6, 70.2],
    [20.8, 71.2],
    [20.7, 72.8],
    [21.2, 72.8],
    [20.4, 72.85],
    [19.0, 72.8],
    [18.0, 73.1],
    [17.0, 73.3],
    [15.5, 73.8],
    [14.8, 74.1],
    [13.8, 74.6],
    [12.9, 74.8],
    [11.25, 75.77],
    [9.9, 76.2],
    [8.5, 76.9],
    [8.08, 77.55],
    [8.8, 78.15],
    [9.28, 79.3],
    [10.3, 79.85],
    [10.77, 79.84],
    [11.8, 79.8],
    [13.08, 80.29],
    [14.4, 80.1],
    [15.8, 80.3],
    [16.2, 81.2],
    [16.9, 82.25],
    [17.7, 83.3],
    [18.3, 84.1],
    [19.3, 85.1],
    [19.8, 85.8],
    [20.8, 86.9],
    [21.5, 87.0],
    [22.5, 88.3],
    [23, 88.5],
  ];
  const poly = coast
    .map(([a, b]) =>
      P(a, b)
        .map((v) => v.toFixed(1))
        .join(','),
    )
    .join(' ');
  const hub = P(...CITY.Bengaluru);
  let s = `<defs><pattern id="dots" width="9" height="9" patternUnits="userSpaceOnUse"><circle cx="2" cy="2" r="1.5" fill="#2E4C8D"/></pattern>
    <linearGradient id="fade" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#0A1A3A"/><stop offset=".18" stop-color="#0A1A3A" stop-opacity="0"/></linearGradient></defs>`;
  s += `<polygon points="${poly}" fill="url(#dots)" stroke="#2E4C8D" stroke-width="1.2" stroke-linejoin="round"/>`;
  s += `<rect width="840" height="600" fill="url(#fade)" pointer-events="none"/>`;
  Object.entries(NET_STATES).forEach(([st, o]) => {
    const [x, y] = P(o.lat, o.lon);
    s += `<text class="stl" data-st="${st}" x="${x}" y="${y}" text-anchor="middle" font-family="JetBrains Mono,monospace" font-size="${st.length > 10 ? 11 : 12}" letter-spacing="2" fill="#5E7AB5">${st.toUpperCase()}</text>`;
  });
  let arcs = '',
    nodes = '';
  Object.entries(NET_STATES).forEach(([st, o]) =>
    o.cities.forEach((c, j) => {
      const [x, y] = P(...CITY[c]);
      const mx = (hub[0] + x) / 2,
        my = (hub[1] + y) / 2,
        dx = x - hub[0],
        dy = y - hub[1],
        len = Math.hypot(dx, dy) || 1;
      const cx = mx - (dy / len) * len * 0.18,
        cyy = my + (dx / len) * len * 0.18;
      const id = `arc-${c.replace(/\W/g, '')}`;
      arcs += `<g class="lane-g" data-st="${st}"><path id="${id}" class="arc" d="M${hub[0]} ${hub[1]} Q${cx.toFixed(1)} ${cyy.toFixed(1)} ${x.toFixed(1)} ${y.toFixed(1)}" fill="none" stroke="#FF7A1A" stroke-width="1.6" opacity=".7"/>
      <circle r="3.2" fill="#FFD2AE">${RM ? '' : `<animateMotion dur="${3 + ((j * 7 + c.length) % 4)}s" repeatCount="indefinite" begin="${(j * 0.6) % 3}s"><mpath href="#${id}"/></animateMotion>`}</circle></g>`;
      nodes += `<g class="node-g" data-st="${st}"><circle cx="${x}" cy="${y}" r="5" fill="#fff"/><text x="${x + 9}" y="${y + 4}" font-family="Instrument Sans,sans-serif" font-size="12" fill="#DDE6F7">${c === 'Thiruvananthapuram' ? 'Trivandrum' : c}</text></g>`;
    }),
  );
  s += arcs + nodes;
  s += `<circle cx="${hub[0]}" cy="${hub[1]}" r="20" fill="rgba(255,122,26,.25)">${RM ? '' : '<animate attributeName="r" values="14;26;14" dur="2.4s" repeatCount="indefinite"/>'}</circle><circle cx="${hub[0]}" cy="${hub[1]}" r="9" fill="#FF7A1A" stroke="#fff" stroke-width="3"/>
    <text x="${hub[0] + 14}" y="${hub[1] + 20}" font-family="Archivo,sans-serif" font-stretch="115%" font-weight="800" font-size="15" fill="#fff">Bengaluru hub</text>`;
  $('#netSvg').innerHTML = s;
  const list = $('#stateList');
  list.innerHTML = ['All states', ...Object.keys(NET_STATES)]
    .map((s, i) => `<button type="button" aria-pressed="${i === 0}" data-st="${i ? s : ''}">${s}</button>`)
    .join('');
  $$('button', list).forEach((b) =>
    b.addEventListener('click', () => {
      $$('button', list).forEach((x) => x.setAttribute('aria-pressed', x === b));
      const st = b.dataset.st;
      $$('#netSvg .lane-g, #netSvg .node-g').forEach(
        (g) => (g.style.opacity = !st || g.dataset.st === st ? 1 : 0.12),
      );
      $$('#netSvg .stl').forEach((t) =>
        t.setAttribute('fill', st && t.dataset.st === st ? '#FF9A4A' : '#5E7AB5'),
      );
    }),
  );
}
