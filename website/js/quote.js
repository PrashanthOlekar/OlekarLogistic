/* ============ PRICE CHECK ============ */
// Asks the ProCargo API for the price (POST /price-estimates). If the API can't be reached,
// it estimates with the same rate card and says so.

const API_TIMEOUT_MS = 4000;
let apiReference = null; // GET /reference-data, fetched once

function el(id) {
  return document.getElementById(id);
}

async function fetchJson(url, options = {}) {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), API_TIMEOUT_MS);
  try {
    const response = await fetch(url, { ...options, signal: controller.signal });
    const body = await response.json().catch(() => null);
    if (!response.ok) {
      const error = new Error(body?.detail || body?.title || `HTTP ${response.status}`);
      error.status = response.status;
      throw error;
    }
    return body;
  } finally {
    clearTimeout(timer);
  }
}

/** Live price from the API, or null when the API can't be reached. */
async function livePrice(from, to, vehicle, weightKg) {
  try {
    apiReference ??= await fetchJson(`${API_URL}/reference-data`);
    const cityId = (city) => apiReference.cities.find((c) => c.name === city.name)?.id;
    const typeId = apiReference.vehicleTypes.find((t) => t.code === vehicle.code)?.id;
    if (!cityId(from) || !cityId(to) || !typeId) return null;
    return await fetchJson(`${API_URL}/price-estimates`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ pickupCityId: cityId(from), dropCityId: cityId(to), vehicleTypeId: typeId, weightKg }),
    });
  } catch (error) {
    if (error.status && error.status < 500) throw error; // the API answered: show its message
    return null; // offline, blocked or timed out: estimate instead
  }
}

function fillSelect(select, items, label, selected) {
  select.innerHTML = '';
  items.forEach((item, index) => {
    const option = new Option(label(item), String(index), false, index === selected);
    select.add(option);
  });
}

function selectedQuote() {
  return {
    from: CITIES[+el('qFrom').value],
    to: CITIES[+el('qTo').value],
    vehicle: VEHICLES[+el('qVehicle').value],
    weightKg: Math.round(+el('qWeight').value),
  };
}

function showRoute() {
  const { from, to } = selectedQuote();
  el('quoteRoute').textContent = `${from.name} to ${to.name}`;
  el('quoteResult').hidden = true;
}

function showError(message) {
  el('quoteError').textContent = message;
  el('quoteError').hidden = !message;
}

/** Chooses a vehicle in the price form (used by the fleet line-up too). */
function chooseVehicle(code, weightKg) {
  const index = VEHICLES.findIndex((vehicle) => vehicle.code === code);
  if (index >= 0) el('qVehicle').value = String(index);
  if (weightKg) el('qWeight').value = String(weightKg);
  showRoute();
}

async function submitQuote(event) {
  event.preventDefault();
  showError('');
  const { from, to, vehicle, weightKg } = selectedQuote();

  if (!weightKg || weightKg < 1) {
    showError('Enter the approximate weight in kg.');
    return;
  }
  if (weightKg > vehicle.maxKg) {
    const bigger = VEHICLES.filter((v) => v.maxKg >= weightKg).sort((a, b) => a.maxKg - b.maxKg)[0];
    showError(
      bigger
        ? `${vehicle.name} carries up to ${weightLabel(vehicle.maxKg)}. Try the ${bigger.name}.`
        : 'That is more than one lorry can carry. Book two trucks or call us.',
    );
    if (bigger) chooseVehicle(bigger.code);
    return;
  }

  const button = event.submitter ?? el('quote').querySelector('[type=submit]');
  button.disabled = true;
  let price;
  let live = false;
  try {
    price = await livePrice(from, to, vehicle, weightKg);
    live = !!price;
  } catch (error) {
    showError(error.message);
    button.disabled = false;
    return;
  }
  price ??= estimatePrice(from, to, vehicle);
  button.disabled = false;

  el('qTotal').textContent = inr(price.totalAmount);
  el('qKm').textContent = `${Math.round(price.distanceKm)} km`;
  el('qVehicleCost').textContent = inr(price.vehicleCost);
  el('qDriverLabel').textContent = `Driver (${price.days} ${price.days === 1 ? 'day' : 'days'})`;
  el('qDriverCost').textContent = inr(price.driverCost);
  el('qGstLabel').textContent = `GST (${price.gstPercent}%)`;
  el('qGst').textContent = inr(price.taxAmount);
  el('qNote').textContent = live
    ? `Live price from ProCargo. Tolls at actuals. Held for ${QUOTE_HELD_MINUTES} minutes once you book.`
    : 'Estimate from the ProCargo rate card. Your exact price is shown when you book; tolls at actuals.';
  el('quoteResult').hidden = false;
}

function initQuote() {
  const city = (c) => c.name;
  fillSelect(el('qFrom'), CITIES, city, CITIES.findIndex((c) => c.name === 'Bengaluru'));
  fillSelect(el('qTo'), CITIES, city, CITIES.findIndex((c) => c.name === 'Hubballi'));
  fillSelect(el('qVehicle'), VEHICLES, (v) => `${v.name} · up to ${weightLabel(v.maxKg)}`, 3);

  ['qFrom', 'qTo', 'qVehicle', 'qWeight'].forEach((id) => el(id).addEventListener('change', showRoute));
  el('qSwap').addEventListener('click', () => {
    const from = el('qFrom').value;
    el('qFrom').value = el('qTo').value;
    el('qTo').value = from;
    showRoute();
  });
  el('quote').addEventListener('submit', submitQuote);
  showRoute();
}
