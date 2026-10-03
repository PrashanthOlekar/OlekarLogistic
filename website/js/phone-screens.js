// The four phone mock-ups in the "app" section and the tabs that rotate them.
/* phone screens */
const PHONE_SCREENS = [
  {
    name: 'Booking',
    html: `<div class="ps-head"><small class="faded">Good morning</small><br><b>Where to today?</b></div><div class="ps-body">
    <div class="ps-card"><small class="muted">Pickup</small><b>Peenya, Bengaluru</b></div><div class="ps-card"><small class="muted">Drop</small><b>Gokul Road, Hubballi</b></div>
    <div class="ps-grid-2"><div class="ps-card ps-card-active"><b>14 FT</b><small class="muted">4,000 kg</small></div><div class="ps-card"><b>17 FT</b><small class="muted">5,000 kg</small></div></div>
    <div class="ps-card"><small class="muted">Estimated fare</small><b class="ps-text-18">₹ 19,240</b></div><div class="ps-btn">Book now</div></div>`,
  },
  {
    name: 'Tracking',
    html: `<div class="ps-map"><svg class="ps-map-art" viewBox="0 0 216 230"><path d="M30 210 C 60 160, 120 150, 140 100 S 170 40, 190 20" fill="none" stroke="#0A1A3A" stroke-width="5" stroke-linecap="round"/><path d="M30 210 C 60 160, 120 150, 140 100" fill="none" stroke="#FF7A1A" stroke-width="5" stroke-linecap="round"/><circle cx="140" cy="100" r="9" fill="#FF7A1A" stroke="#fff" stroke-width="3"/></svg></div>
    <div class="ps-body"><div class="ps-card"><b>In transit · Chitradurga</b><small class="muted">ETA 4 h 20 m · 210 km left</small><div class="ps-progress"><div class="ps-progress-fill"></div></div></div><div class="ps-card"><b>Ramesh Gowda</b><small class="muted">KA 01 AB 4521 · 17 FT</small></div></div>`,
  },
  {
    name: 'Driver',
    html: `<div class="ps-head"><b>Trip PC-24790</b><br><small class="faded">Bengaluru → Hubballi</small></div><div class="ps-body">
    <div class="ps-bigbtn ps-bigbtn-orange">Reached pickup</div><div class="ps-bigbtn ps-bigbtn-navy">Enter pickup OTP</div><div class="ps-bigbtn ps-bigbtn-green">Start trip</div><div class="ps-card"><small class="muted">Today's earnings</small><b class="ps-text-16">₹ 2,450</b></div></div>`,
  },
  {
    name: 'Payments',
    html: `<div class="ps-head"><small class="faded">Pay for PC-24831</small><br><b class="ps-text-20">₹ 19,240</b></div><div class="ps-body">
    <div class="ps-card ps-card-active"><b>UPI</b><small class="muted">GPay, PhonePe, Paytm</small></div><div class="ps-card"><b>Card</b><small class="muted">Credit / debit</small></div><div class="ps-card"><b>Net banking</b></div><div class="ps-btn">Pay securely</div><small class="muted text-center">🔒 Encrypted checkout</small></div>`,
  },
  {
    name: 'Trip history',
    html: `<div class="ps-head"><b>Trip history</b><br><small class="faded">September 2026</small></div><div class="ps-body">
    ${[
      ['Bengaluru → Mysuru', '₹ 6,420', 'Delivered'],
      ['Hubballi → Pune', '₹ 21,400', 'Delivered'],
      ['Bengaluru → Chennai', '₹ 17,900', 'Delivered'],
      ['Tumakuru → Bengaluru', '₹ 3,150', 'Delivered'],
    ]
      .map(
        (r) =>
          `<div class="ps-card"><b>${r[0]}</b><div class="spread"><small class="muted">${r[2]}</small><b>${r[1]}</b></div></div>`,
      )
      .join('')}</div>`,
  },
];
function renderPhones(c) {
  const n = PHONE_SCREENS.length,
    idx = [(c + n - 1) % n, c, (c + 1) % n];
  $('#phones').innerHTML = idx
    .map(
      (i) =>
        `<div class="phone"><div class="scr"><span class="notch"></span>${PHONE_SCREENS[i].html}</div></div>`,
    )
    .join('');
}
