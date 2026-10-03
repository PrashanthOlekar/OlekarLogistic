// Draws the dotted line that connects the eight "how it works" steps.
/* how-it-works connector */
function drawHowLine() {
  const wrap = $('#howWrap'),
    svg = $('#howSvg');
  if (getComputedStyle($('.how-line')).display === 'none') return null;
  const wb = wrap.getBoundingClientRect();
  const pts = $$('.step .node').map((n) => {
    const b = n.getBoundingClientRect();
    return [b.left - wb.left + b.width / 2, b.top - wb.top + b.height / 2];
  });
  svg.setAttribute('viewBox', `0 0 ${wb.width} ${wb.height}`);
  let d = `M${pts[0][0]} ${pts[0][1]}`;
  for (let i = 1; i < pts.length; i++) {
    const [x0, y0] = pts[i - 1],
      [x, y] = pts[i];
    if (Math.abs(y - y0) < 5) d += ` L${x} ${y}`;
    else {
      const my = (y0 + y) / 2 + 10;
      d += ` C${x0 + 120} ${y0}, ${x0 + 120} ${my}, ${x0} ${my} L${x + 0} ${my} C${x - 120} ${my}, ${x - 120} ${y}, ${x} ${y}`;
    }
  }
  $('#howBase').setAttribute('d', d);
  const hp = $('#howPath');
  hp.setAttribute('d', d);
  const L = hp.getTotalLength();
  hp.style.strokeDasharray = L;
  hp.style.strokeDashoffset = L;
  // fraction for each node
  const fr = pts.map(([x, y]) => {
    let best = 0,
      bd = 1e9;
    for (let k = 0; k <= 300; k++) {
      const p = hp.getPointAtLength((L * k) / 300);
      const dd = (p.x - x) ** 2 + (p.y - y) ** 2;
      if (dd < bd) {
        bd = dd;
        best = k / 300;
      }
    }
    return best;
  });
  return { L, fr };
}
let HOW = null;
function howProgress(p) {
  if (!HOW) return;
  $('#howPath').style.strokeDashoffset = HOW.L * (1 - p);
  $$('.step').forEach((s, i) => s.classList.toggle('on', p >= HOW.fr[i] - 0.005));
}
