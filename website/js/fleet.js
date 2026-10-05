/* ============ FLEET LINE-UP ============ */
// Each lorry drawn to scale by body length. The load slider (100 kg to 28 t, on a log scale)
// dims what can't carry the load and marks the smallest lorry that can.

const SLIDER_MIN_KG = 100;
const SLIDER_MAX_KG = 28000;
const LONGEST_FT = 40;

function sliderToKg(value) {
  const kg = SLIDER_MIN_KG * (SLIDER_MAX_KG / SLIDER_MIN_KG) ** (value / 1000);
  return kg < 1000 ? Math.round(kg / 10) * 10 : Math.round(kg / 100) * 100;
}

function kgToSlider(kg) {
  return Math.round((Math.log(kg / SLIDER_MIN_KG) / Math.log(SLIDER_MAX_KG / SLIDER_MIN_KG)) * 1000);
}

function renderFleet() {
  const list = document.getElementById('fleetList');
  list.innerHTML = VEHICLES.map((v) => {
    const size = [v.len, v.wid, v.hgt].filter(Boolean).join(' × ') + ' ft';
    const width = Math.round((v.len / LONGEST_FT) * 210 + 18);
    return `
      <li class="fleet-row" data-max="${v.maxKg}" data-code="${v.code}">
        <div class="body-scale" aria-hidden="true"><i style="width:${width}px"></i></div>
        <div class="fleet-name"><b>${v.name}<span class="badge">Best fit</span></b><span>${v.body} body · ${size}</span></div>
        <div class="fleet-cap">${weightLabel(v.maxKg)}</div>
        <div class="fleet-goods"><small>Good for</small>${v.goods}</div>
        <a class="fleet-book" href="#quote" data-code="${v.code}">Price this lorry</a>
      </li>`;
  }).join('');

  list.addEventListener('click', (event) => {
    const link = event.target.closest('.fleet-book');
    if (!link) return;
    chooseVehicle(link.dataset.code, sliderToKg(+document.getElementById('loadSlider').value));
  });
}

function updateFleet() {
  const kg = sliderToKg(+document.getElementById('loadSlider').value);
  document.getElementById('loadValue').textContent = weightLabel(kg);
  const rows = [...document.querySelectorAll('.fleet-row')];
  const fits = rows.filter((row) => +row.dataset.max >= kg).sort((a, b) => a.dataset.max - b.dataset.max);
  rows.forEach((row) => {
    row.classList.toggle('too-small', +row.dataset.max < kg);
    row.classList.toggle('best', row === fits[0]);
  });
}

function initFleet() {
  renderFleet();
  const slider = document.getElementById('loadSlider');
  slider.value = String(kgToSlider(3000));
  slider.addEventListener('input', updateFleet);
  updateFleet();
}
