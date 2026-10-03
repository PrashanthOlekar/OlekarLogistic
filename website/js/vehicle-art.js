// Draws each vehicle class as an SVG (cab, body, wheels) for the vehicle carousel.
/* vehicle illustration */
function vehSVG(v, uid) {
  const G = 100,
    cabW = v.cab === 'mini' ? 44 : v.cab === 'lcv' ? 50 : 58,
    cabH = v.cab === 'mini' ? 40 : v.cab === 'lcv' ? 50 : 60;
  const r = v.cab === 'mini' ? 10 : v.cab === 'lcv' ? 12 : 14,
    chY = G - r - 8;
  const cabX = 292 - cabW,
    bodyX = cabX - 4 - v.bw,
    top = chY - v.bh;
  let s = `<svg viewBox="0 0 300 108" aria-hidden="true">`;
  s += `<ellipse cx="150" cy="${G + 3}" rx="140" ry="4" fill="#0A1A3A" opacity=".12"/>`;
  if (v.kind === 'trailer') {
    const bx = 18,
      bw = 196;
    s += `<rect x="${bx}" y="${chY - 6}" width="${bw}" height="8" fill="#26324F"/>`;
    s += `<rect x="${bx + 8}" y="${chY - 52}" width="${bw - 16}" height="46" rx="2" fill="#3C5A8F"/>`;
    s += `<g stroke="#2E4A7C" stroke-width="2">${Array.from({ length: 13 }, (_, i) => `<path d="M${bx + 20 + i * 13} ${chY - 48}v38"/>`).join('')}</g>`;
    s += `<text x="${bx + 18}" y="${chY - 24}" font-family="JetBrains Mono,monospace" font-size="9" font-weight="700" fill="#DDE6F7">PCGU 204118</text>`;
  } else if (v.kind === 'container') {
    s += `<rect x="${bodyX}" y="${top}" width="${v.bw}" height="${v.bh}" rx="3" fill="#3C5A8F"/>`;
    s += `<g stroke="#2E4A7C" stroke-width="2.4">${Array.from({ length: Math.floor(v.bw / 12) }, (_, i) => `<path d="M${bodyX + 8 + i * 12} ${top + 4}v${v.bh - 8}"/>`).join('')}</g>`;
    s += `<rect x="${bodyX}" y="${top + v.bh - 9}" width="${v.bw}" height="5" fill="#FF7A1A"/>`;
  } else if (v.kind === 'open') {
    s += `<rect x="${bodyX}" y="${top}" width="${v.bw}" height="${v.bh}" rx="2" fill="#FF7A1A"/>`;
    s += `<g stroke="#E0600A" stroke-width="2">${Array.from({ length: Math.floor(v.bh / 9) }, (_, i) => `<path d="M${bodyX + 3} ${top + 7 + i * 9}h${v.bw - 6}"/>`).join('')}</g>`;
    s += `<g stroke="#0A1A3A" stroke-width="2.5">${Array.from({ length: Math.floor(v.bw / 40) + 1 }, (_, i) => `<path d="M${bodyX + 4 + i * ((v.bw - 8) / Math.floor(v.bw / 40))} ${top}v${v.bh}"/>`).join('')}</g>`;
  } else {
    s += `<rect x="${bodyX}" y="${top}" width="${v.bw}" height="${v.bh}" rx="3" fill="#13295A"/>`;
    s += `<g stroke="#1D3B78" stroke-width="2">${Array.from({ length: Math.floor(v.bw / 16) }, (_, i) => `<path d="M${bodyX + 10 + i * 16} ${top + 3}v${v.bh - 6}"/>`).join('')}</g>`;
    if (v.bw > 120)
      s += `<text x="${bodyX + 10}" y="${top + v.bh * 0.58}" font-family="Archivo,Arial Narrow,sans-serif" font-stretch="125%" font-weight="900" font-size="${Math.min(17, v.bw / 10)}" fill="#fff">PROCARGO</text>`;
    s += `<rect x="${bodyX}" y="${top + v.bh - 8}" width="${v.bw}" height="5" fill="#FF7A1A"/>`;
  }
  // chassis
  const chX = v.kind === 'trailer' ? 12 : bodyX - 6;
  s += `<rect x="${chX}" y="${chY}" width="${298 - chX}" height="7" rx="2" fill="#0A0F1F"/>`;
  // cab
  const cy = chY + 7 - cabH;
  s += `<path d="M${cabX} ${cy + 4} H${cabX + cabW - 18} Q${cabX + cabW - 8} ${cy + 4} ${cabX + cabW - 5} ${cy + 14} L${cabX + cabW} ${cy + cabH * 0.62} V${chY + 7} H${cabX} Z" fill="#EEF1F7" stroke="#CBD3E2"/>`;
  s += `<path d="M${cabX + cabW * 0.5} ${cy + 9} H${cabX + cabW - 16} Q${cabX + cabW - 10} ${cy + 9} ${cabX + cabW - 8} ${cy + 16} L${cabX + cabW - 3} ${cy + cabH * 0.55} H${cabX + cabW * 0.5} Z" fill="#5F87C9"/>`;
  s += `<rect x="${cabX + 4}" y="${cy + 10}" width="${cabW * 0.36}" height="${cabH * 0.3}" rx="2" fill="#5F87C9"/>`;
  s += `<rect x="${cabX}" y="${cy + cabH * 0.68}" width="${cabW}" height="4" fill="#FF7A1A"/>`;
  s += `<rect x="${cabX + cabW - 4}" y="${cy + cabH * 0.74}" width="5" height="5" rx="1" fill="#FFD27A"/>`;
  // wheels
  const wheel = (x) =>
    `<g class="wh"><circle cx="${x}" cy="${G - r}" r="${r}" fill="#0A0F1F"/><circle cx="${x}" cy="${G - r}" r="${r * 0.45}" fill="#9AA8C4"/><path d="M${x} ${G - r * 1.45}v${r * 0.9}M${x - r * 0.45} ${G - r}h${r * 0.9}" stroke="#56627E" stroke-width="1.6"/></g>`;
  s += wheel(cabX + cabW * 0.52);
  const n = v.rear[v.rear.length - 1],
    rx0 = v.kind === 'trailer' ? 40 : bodyX + Math.min(40, v.bw * 0.22);
  for (let i = 0; i < n; i++) s += wheel(rx0 + i * (r * 2 + 3));
  if (v.kind === 'trailer') {
    s += wheel(cabX - 8);
    s += wheel(cabX - 8 - (r * 2 + 3));
  }
  return s + `</svg>`;
}
