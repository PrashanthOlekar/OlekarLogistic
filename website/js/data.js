/* ============ DATA ============ */
// The same cities, lorry classes and rates as the ProCargo database (database/ProCargo.sql).
// Used to fill the forms, and to estimate a price when the API can't be reached.

const CITIES = [
  { name: 'Bengaluru', kn: 'ಬೆಂಗಳೂರು', lat: 12.9716, lng: 77.5946 },
  { name: 'Mysuru', kn: 'ಮೈಸೂರು', lat: 12.2958, lng: 76.6394 },
  { name: 'Tumakuru', kn: 'ತುಮಕೂರು', lat: 13.3409, lng: 77.101 },
  { name: 'Chitradurga', kn: 'ಚಿತ್ರದುರ್ಗ', lat: 14.2306, lng: 76.398 },
  { name: 'Davanagere', kn: 'ದಾವಣಗೆರೆ', lat: 14.4644, lng: 75.9218 },
  { name: 'Hubballi', kn: 'ಹುಬ್ಬಳ್ಳಿ', lat: 15.3647, lng: 75.124 },
  { name: 'Belagavi', kn: 'ಬೆಳಗಾವಿ', lat: 15.8497, lng: 74.4977 },
  { name: 'Mangaluru', kn: 'ಮಂಗಳೂರು', lat: 12.9141, lng: 74.856 },
  { name: 'Kalaburagi', kn: 'ಕಲಬುರಗಿ', lat: 17.3297, lng: 76.8343 },
  { name: 'Pune', lat: 18.5204, lng: 73.8567 },
  { name: 'Mumbai', lat: 19.076, lng: 72.8777 },
  { name: 'Chennai', lat: 13.0827, lng: 80.2707 },
  { name: 'Hyderabad', lat: 17.385, lng: 78.4867 },
  { name: 'Kochi', lat: 9.9312, lng: 76.2673 },
  { name: 'Panaji', lat: 15.4909, lng: 73.8278 },
];

// code, name, body, max load (kg), length × width × height (ft), best for, ₹/km, minimum fare, driver ₹/day
const VEHICLES = [
  ['ACE', 'Tata Ace', 'Closed', 750, 7, 4.8, 6, 'Parcels, FMCG, single-room moves', 16, 450, 500],
  ['PICKUP', 'Pickup Truck', 'Open', 1500, 8, 5.5, null, 'Appliances, hardware, farm produce', 20, 650, 500],
  ['8FT', '8 FT Truck', 'Closed', 2000, 8, 5.5, 6, '1 BHK moves, retail stock', 22, 900, 600],
  ['14FT', '14 FT Truck', 'Closed', 4000, 14, 6, 6.5, '2-3 BHK moves, e-commerce, garments', 32, 1800, 700],
  ['17FT', '17 FT Truck', 'Closed', 5000, 17, 6.5, 7, 'Office moves, packaged goods', 38, 2400, 800],
  ['20FT', '20 FT Truck', 'Closed', 7000, 20, 7, 7, 'Electronics, FMCG distribution', 45, 3000, 900],
  ['22FT', '22 FT Truck', 'Closed', 10000, 22, 7.5, 7, 'Cement, tiles, bulk cartons', 52, 3800, 900],
  ['32FT', '32 FT Truck', 'Closed', 16000, 32, 8, 8, 'Factory loads, inter-state freight', 72, 6500, 1000],
  ['CONT24', 'Container', 'Container', 12000, 24, 8, 8, 'High-value, weather-sensitive cargo', 60, 5500, 1000],
  ['OPEN10W', 'Open Body Truck', 'Open', 21000, 24, 8, null, 'Steel, sand, agri produce, machinery', 65, 6000, 1000],
  ['TRAILER40', 'Trailer', 'Flatbed', 28000, 40, 8, null, 'Heavy machinery, coils, ODC', 95, 12000, 1200],
].map(([code, name, body, maxKg, len, wid, hgt, goods, ratePerKm, minFare, battaPerDay]) => ({
  code, name, body, maxKg, len, wid, hgt, goods, ratePerKm, minFare, battaPerDay,
}));

// Settings table: GST on freight and the owner commission
const GST_PERCENT = 5;
const QUOTE_HELD_MINUTES = 30;

/** "₹1,23,456" */
function inr(amount) {
  return '₹' + Math.round(amount).toLocaleString('en-IN');
}

/** "3,000 kg" or "12 t" */
function weightLabel(kg) {
  return kg >= 10000 ? `${(kg / 1000).toLocaleString('en-IN')} t` : `${kg.toLocaleString('en-IN')} kg`;
}

/** The same estimate as the API's PriceCalculator, for when the API can't be reached. */
function estimatePrice(from, to, vehicle) {
  let km = 18;
  if (from !== to) {
    const rad = Math.PI / 180;
    const dLat = (to.lat - from.lat) * rad;
    const dLng = (to.lng - from.lng) * rad;
    const a = Math.sin(dLat / 2) ** 2 + Math.cos(from.lat * rad) * Math.cos(to.lat * rad) * Math.sin(dLng / 2) ** 2;
    km = Math.round(2 * 6371 * Math.asin(Math.sqrt(a)) * 1.24 * 10) / 10;
  }
  const vehicleCost = Math.round(Math.max(vehicle.minFare, vehicle.ratePerKm * km));
  const days = Math.max(1, Math.ceil(km / 400));
  const driverCost = days * vehicle.battaPerDay;
  const freight = vehicleCost + driverCost;
  const taxAmount = Math.round(freight * GST_PERCENT) / 100;
  return { distanceKm: km, days, vehicleCost, driverCost, taxAmount, totalAmount: freight + taxAmount, gstPercent: GST_PERCENT };
}
