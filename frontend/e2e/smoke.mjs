/**
 * End-to-end smoke test: the whole ProCargo business through the real portal, API and database.
 *
 *   customer registers and books → owner and driver register → admin approves them
 *   → customer pays → owner takes the load → driver runs the trip with the two codes and a POD photo
 *   → admin approves the POD → admin records the payout → owner sees it paid, customer sees the invoice
 *
 * Plus: role guard, session kept across a reload, sign-out, paging and search, blocking a user.
 *
 * Needs a running API (Development, so sign-in codes are returned) and the built portal:
 *   PORTAL_URL=http://localhost:4173 API_URL=http://localhost:5080/api/v1 node e2e/smoke.mjs
 * Requires the `playwright` package (installed by CI only, it is not a project dependency).
 */
import { chromium } from 'playwright';

const PORTAL = process.env.PORTAL_URL ?? 'http://localhost:4173';
const API = process.env.API_URL ?? 'http://localhost:5080/api/v1';
const ADMIN_MOBILE = process.env.ADMIN_MOBILE ?? '9999999999';
const TIMEOUT_MS = 15_000;

/** A 1×1 PNG, used for the KYC document and the delivery receipt. */
const PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=',
  'base64',
);

const run = Date.now().toString().slice(-6);
const mobile = (first) => `${first}${run}${String(Math.floor(Math.random() * 1000)).padStart(3, '0')}`;
const people = {
  customer: { mobile: mobile('9'), name: `E2E Customer ${run}` },
  owner: { mobile: mobile('8'), name: `E2E Owner ${run}` },
  driver: { mobile: mobile('7'), name: `E2E Driver ${run}` },
};
const plate = `KA${String(10 + (Number(run) % 89))}E${String(1000 + (Number(run) % 8999))}`;

let step = 'start';
const problems = [];
/** Recent failed requests and console errors, shown if a step fails. */
const recent = [];
const pages = [];

function log(message) {
  step = message;
  console.log(`▶ ${message}`);
}

async function newRolePage(browser) {
  const context = await browser.newContext({
    geolocation: { latitude: 12.9716, longitude: 77.5946 },
    permissions: ['geolocation'],
  });
  context.setDefaultTimeout(TIMEOUT_MS);
  const page = await context.newPage();
  page.on('pageerror', (error) => problems.push(`Page error on ${page.url()}: ${error.message}`));
  page.on('response', (response) => {
    if (response.status() >= 500) {
      problems.push(`${response.status()} from ${response.request().method()} ${response.url()}`);
    } else if (response.status() >= 400) {
      recent.push(`${response.status()} ${response.request().method()} ${response.url()}`);
    }
  });
  page.on('requestfailed', (request) => recent.push(`failed ${request.method()} ${request.url()}: ${request.failure()?.errorText}`));
  page.on('console', (message) => {
    if (message.type() === 'error') recent.push(`console: ${message.text()}`);
  });
  pages.push(page);
  return page;
}

/** Fills the mobile, sends the code and types the test-mode code the development API returns. */
async function enterMobileAndCode(page, mobileNumber) {
  await page.getByLabel('Mobile number').fill(mobileNumber);
  await page.getByRole('button', { name: /^(Send code|Resend)$/ }).click();
  const code = (await page.locator('.hint b.mono').textContent()).trim();
  await page.getByLabel('6-digit code').fill(code);
}

async function toast(page, text) {
  await page.locator('.toast', { hasText: text }).first().waitFor();
}

async function expectUrl(page, pattern) {
  await page.waitForURL(pattern);
}

async function register(page, kind, person, fill) {
  await page.goto(`${PORTAL}/register`);
  await page.getByRole('radio', { name: kind }).click();
  await enterMobileAndCode(page, person.mobile);
  await page.getByLabel('Full name').fill(person.name);
  await fill?.();
  await page.getByRole('button', { name: 'Create account' }).click();
}

/** Approves one applicant on the Approvals page and waits for the row to leave the list. */
async function approve(page, rowText) {
  const row = page.locator('tr', { hasText: rowText });
  await row.getByRole('button', { name: 'Approve' }).click();
  await row.waitFor({ state: 'detached' });
}

async function signIn(page, mobileNumber) {
  await page.goto(`${PORTAL}/login`);
  await enterMobileAndCode(page, mobileNumber);
  await page.getByRole('button', { name: 'Sign in' }).click();
}

async function main() {
  const reference = await (await fetch(`${API}/reference-data`)).json();
  const fourteenFeet = reference.vehicleTypes.find((type) => type.code === '14FT');
  if (!fourteenFeet) throw new Error('Reference data has no 14FT vehicle type.');

  const browser = await chromium.launch();
  try {
    const customer = await newRolePage(browser);
    const owner = await newRolePage(browser);
    const driver = await newRolePage(browser);
    const admin = await newRolePage(browser);

    // ------------------------------------------------------------ customer books a truck
    log('Customer registers');
    await register(customer, 'I need transport', people.customer);
    await expectUrl(customer, /\/customer\/book$/);

    log('Customer gets a live price and books');
    await customer.locator('.pill.green', { hasText: 'Live' }).waitFor();
    await customer.getByPlaceholder('Warehouse 12, Peenya Industrial Area').fill('Plot 12, Peenya Industrial Area');
    await customer.getByPlaceholder('Gokul Road, Hubballi').fill('Gokul Road');
    await customer.getByPlaceholder('120 cartons of biscuits').fill('40 cartons of biscuits');
    await customer.getByRole('button', { name: 'Continue to payment' }).click();
    await expectUrl(customer, /\/customer\/bookings\/\d+$/);
    const bookingTitle = await customer.getByRole('heading', { level: 1 }).textContent();
    const bookingNumber = bookingTitle.replace('Booking ', '').trim();
    console.log(`  booking ${bookingNumber}`);

    log('Customer is kept out of admin pages');
    await customer.goto(`${PORTAL}/admin/users`);
    await expectUrl(customer, /\/customer\/bookings$/);
    await customer.getByText(bookingNumber).waitFor();

    log('Session survives a page reload (refresh token)');
    await customer.reload();
    await customer.getByText(bookingNumber).waitFor();
    await customer.locator('.pager', { hasText: 'Showing 1–1 of 1' }).waitFor();

    // ------------------------------------------------------------ owner, vehicle, driver
    log('Owner registers');
    await register(owner, 'I own lorries', people.owner, async () => {
      await owner.getByPlaceholder('ABCDE1234F').fill(`ABCDE${run.slice(-4)}F`);
      await owner.getByLabel('Aadhaar, last 4 digits').fill('4321');
      await owner.getByLabel('Account holder name').fill(people.owner.name);
      await owner.getByLabel('Account number').fill(`50100${run}789`);
      await owner.getByPlaceholder('HDFC0001234').fill('HDFC0001234');
    });
    await expectUrl(owner, /\/owner\/documents$/);

    log('Owner uploads a KYC document');
    await owner.getByRole('button', { name: 'Upload document' }).click();
    const upload = owner.getByRole('dialog');
    await upload.locator('select').first().selectOption({ label: 'PAN card' });
    await upload.locator('input[type=file]').setInputFiles({ name: 'pan.png', mimeType: 'image/png', buffer: PNG });
    await upload.getByRole('button', { name: 'Upload' }).click();
    await toast(owner, 'Uploaded');
    await owner.locator('td', { hasText: 'PAN card' }).waitFor();

    log('Owner adds a 14 ft truck');
    await owner.goto(`${PORTAL}/owner/vehicles`);
    await owner.getByRole('button', { name: 'Add vehicle' }).click();
    const addVehicle = owner.getByRole('dialog');
    await addVehicle.getByLabel('Registration number').fill(plate);
    await addVehicle.getByLabel('Vehicle type').selectOption({ label: fourteenFeet.name });
    await addVehicle.getByRole('button', { name: 'Add vehicle' }).click();
    await toast(owner, 'Vehicle added');
    await owner.locator('.plate', { hasText: plate }).waitFor();

    log('Driver registers under the owner');
    await register(driver, 'I drive', people.driver, async () => {
      await driver.getByPlaceholder('KA01 20110045678').fill(`KA01 2015${run}`);
      await driver.getByLabel('Licence valid until').fill('2031-12-31');
      await driver.getByLabel("Your lorry owner's mobile").fill(people.owner.mobile);
    });
    await expectUrl(driver, /\/driver\/documents$/);

    // ------------------------------------------------------------ admin approves
    log('Admin signs in');
    await signIn(admin, ADMIN_MOBILE);
    await expectUrl(admin, /\/admin$/);
    await admin.getByRole('heading', { name: 'Operations dashboard' }).waitFor();

    log('Admin approves the owner, driver and vehicle');
    await admin.goto(`${PORTAL}/admin/approvals`);
    await approve(admin, people.owner.name);
    await admin.getByRole('tab', { name: /Drivers/ }).click();
    await approve(admin, people.driver.name);
    await admin.getByRole('tab', { name: /Vehicles/ }).click();
    await approve(admin, plate);

    log('Admin finds the customer by mobile');
    await admin.goto(`${PORTAL}/admin/users`);
    await admin.getByLabel('Search users').fill(people.customer.mobile);
    await admin.getByLabel('Search users').press('Enter');
    await admin.locator('.pager', { hasText: 'Showing 1–1 of 1' }).waitFor();
    await admin.locator('tr', { hasText: people.customer.name }).waitFor();

    // ------------------------------------------------------------ payment and the load
    log('Customer pays');
    await customer.goto(`${PORTAL}/customer/bookings`);
    await customer.getByRole('link', { name: bookingNumber }).click();
    await customer.getByRole('button', { name: /^Pay ₹/ }).click();
    await customer.getByRole('button', { name: 'Pay securely' }).click();
    await toast(customer, 'Payment received');
    await customer.getByText('Finding a truck').first().waitFor();

    log('Owner takes the load');
    await owner.goto(`${PORTAL}/owner`);
    const load = owner.locator('.load-card', { hasText: bookingNumber });
    await load.getByRole('button', { name: 'Take this load' }).click();
    await owner.getByRole('button', { name: 'Confirm and assign' }).click();
    await toast(owner, 'Load taken');

    log('Customer sees the truck and the pickup code');
    await customer.reload();
    const pickupCode = (await customer.locator('.code-box', { hasText: 'Pickup code' }).locator('b').textContent()).trim();

    // ------------------------------------------------------------ the driver runs the trip
    log('Driver runs the trip to pickup');
    await driver.goto(`${PORTAL}/driver`);
    await driver.getByRole('button', { name: "I'm on the way" }).click();
    await toast(driver, 'on the way');
    await driver.getByRole('button', { name: 'I reached pickup' }).click();
    await driver.getByLabel('Pickup code from the sender').fill(pickupCode);
    await driver.getByRole('button', { name: 'Confirm goods loaded' }).click();
    await toast(driver, 'Goods loaded');

    log('Customer sees the delivery code');
    await customer.reload();
    const deliveryCode = (await customer.locator('.code-box', { hasText: 'Delivery code' }).locator('b').textContent()).trim();

    log('Driver delivers with POD and code');
    await driver.getByRole('button', { name: 'Start trip' }).click();
    await driver.getByRole('button', { name: 'I reached the destination' }).click();
    await driver
      .getByLabel('Photo of the signed delivery receipt (POD)')
      .setInputFiles({ name: 'pod.png', mimeType: 'image/png', buffer: PNG });
    await driver.getByRole('button', { name: 'Upload POD' }).click();
    await toast(driver, 'Delivery receipt uploaded');
    await driver.getByLabel('Delivery code from the receiver').fill(deliveryCode);
    await driver.getByRole('button', { name: 'Complete delivery' }).click();
    await driver.getByText('Trip delivered').waitFor();

    // ------------------------------------------------------------ money
    log('Admin approves the POD');
    await admin.goto(`${PORTAL}/admin/trips`);
    await admin.getByRole('button', { name: 'POD to approve' }).click();
    await admin.locator('tr', { hasText: bookingNumber }).getByRole('button', { name: 'Approve POD' }).click();
    await toast(admin, 'POD approved');

    log('Admin records the owner payout');
    await admin.goto(`${PORTAL}/admin/settlements`);
    await admin.locator('tr', { hasText: people.owner.name }).getByRole('button', { name: 'Mark paid' }).click();
    await admin.getByLabel('Bank transfer reference (UTR)').fill(`UTR${run}0001`);
    await admin.getByRole('button', { name: 'Record payout' }).click();
    await toast(admin, 'recorded as paid');

    log('Owner sees the payout as paid');
    await owner.goto(`${PORTAL}/owner/payouts`);
    await owner.locator('td', { hasText: `UTR${run}0001` }).waitFor();

    log('Customer sees the completed booking and its invoice');
    await customer.reload();
    await customer.getByRole('heading', { name: 'Tax invoice' }).waitFor();

    // ------------------------------------------------------------ blocking and signing out
    log('A blocked customer is signed out on their next request');
    await admin.goto(`${PORTAL}/admin/users`);
    await admin.getByLabel('Search users').fill(people.customer.mobile);
    await admin.getByLabel('Search users').press('Enter');
    await admin.locator('tr', { hasText: people.customer.name }).getByRole('button', { name: 'Block' }).click();
    await toast(admin, 'blocked');
    await customer.goto(`${PORTAL}/customer/bookings`);
    await expectUrl(customer, /\/login$/);
    await admin.locator('tr', { hasText: people.customer.name }).getByRole('button', { name: 'Unblock' }).click();
    await toast(admin, 'unblocked');

    log('Signing out ends the session');
    await owner.getByRole('button', { name: 'Sign out' }).click();
    await expectUrl(owner, /\/login$/);
    await owner.goto(`${PORTAL}/owner/vehicles`);
    await expectUrl(owner, /\/login$/);

    log('A wrong sign-in code shows an error');
    await owner.getByLabel('Mobile number').fill(people.owner.mobile);
    await owner.getByRole('button', { name: 'Send code' }).click();
    await owner.locator('.hint b.mono').waitFor();
    await owner.getByLabel('6-digit code').fill('000000');
    await owner.getByRole('button', { name: 'Sign in' }).click();
    await owner.getByRole('alert').waitFor();
  } catch (error) {
    error.pagesSeen = await describePages();
    throw error;
  } finally {
    await browser.close();
  }

  if (problems.length > 0) {
    throw new Error(`The journey passed but the browser saw problems:\n${problems.join('\n')}`);
  }
  console.log('✔ Portal smoke test passed');
}

/** What each open page shows: its address and any alert or error text. */
async function describePages() {
  // Called before the browser closes.
  const descriptions = [];
  for (const page of pages) {
    try {
      const alerts = await page.locator('.alert, .err').allInnerTexts();
      descriptions.push(`${page.url()} ${alerts.length ? `alerts: ${alerts.join(' / ')}` : ''}`);
    } catch {
      /* page already closed */
    }
  }
  return descriptions;
}

main().catch(async (error) => {
  const detail = [error.message.split('\n')[0], ...problems, ...recent.slice(-8), ...(error.pagesSeen ?? [])].join(' | ');
  console.error(error);
  console.log(`::error title=Portal smoke test failed at "${step}"::${detail.replace(/\r?\n/g, ' ')}`);
  process.exit(1);
});
