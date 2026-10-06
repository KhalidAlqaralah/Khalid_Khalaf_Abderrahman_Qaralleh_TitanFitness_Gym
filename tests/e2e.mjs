// End-to-end UI tests for the Angular staff portal (needs a FRESHLY SEEDED database).
// Run: node e2e.mjs   (API on http://localhost:5162, Angular on http://localhost:4200)
import { chromium, expect as baseExpect } from 'playwright/test';
import { mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// Slower PCs: give each UI assertion up to 10 s.
const expect = baseExpect.configure({ timeout: 10000 });

const APP = process.env.APP_URL ?? 'http://localhost:4200';
const SHOTS = fileURLToPath(new URL('./screenshots/', import.meta.url));
mkdirSync(SHOTS, { recursive: true });

const results = [];
function ok(name, cond, detail = '') {
  results.push([!!cond, name]);
  console.log((cond ? 'PASS ' : 'FAIL ') + name + (cond ? '' : `  [${typeof detail === 'string' ? detail : JSON.stringify(detail)}]`));
}

const pad = n => String(n).padStart(2, '0');
const mdy = d => `${pad(d.getMonth() + 1)}/${pad(d.getDate())}/${d.getFullYear()}`;
const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const addDays = (d, n) => { const c = new Date(d.getFullYear(), d.getMonth(), d.getDate()); c.setDate(c.getDate() + n); return c; };

const browser = await chromium.launch();
const ctx = await browser.newContext({ viewport: { width: 1400, height: 950 }, locale: 'en-US' });
const page = await ctx.newPage();
page.setDefaultTimeout(10000);
// SLOW=4 node e2e.mjs  -> runs the browser 4x slower, to check the tests on a slow PC.
if (process.env.SLOW) await (await ctx.newCDPSession(page)).send('Emulation.setCPUThrottlingRate', { rate: Number(process.env.SLOW) });
const pageErrors = [];
page.on('pageerror', e => pageErrors.push(String(e)));

const tid = id => `[data-testid=${id}]`;

// Wait for Angular to render the current change (zoneless change detection runs on the next frame).
const settle = () => page.evaluate(() => new Promise(r => requestAnimationFrame(() => setTimeout(r, 30))));

// Fill a field and make sure the value stays: a dialog that is still rendering can overwrite it with its initial value.
async function fill(target, value) {
  const loc = typeof target === 'string' ? page.locator(target) : target;
  for (let attempt = 0; attempt < 3; attempt++) {
    await loc.waitFor();
    await settle();
    const before = await loc.inputValue();
    await loc.fill(value);
    await settle();
    const after = await loc.inputValue();
    const overwritten = value !== '' && (after === '' || (after === before && before !== value));
    if (!overwritten) return;
  }
}
async function login(user, pwd) {
  await page.goto(APP + '/login');
  await fill('#userName', user);
  await fill('#password', pwd);
  await page.click(tid('login-submit'));
}
async function pickMember(search, name) {
  await fill(page.locator(tid('member-search')), search);
  const opt = page.locator('mat-option', { hasText: name });
  await opt.waitFor();
  await opt.click();
}
async function step(name, fn) {
  try {
    await fn();
  } catch (e) {
    ok(`${name} (stopped)`, false, String(e.message ?? e).replace(/\u001b\[[0-9;]*m/g, '').split('\n').map(l => l.trim()).filter(Boolean).slice(0, 5).join(' | ').slice(0, 600));
    await page.screenshot({ path: `${SHOTS}fail-${name.replace(/[^a-z0-9]+/gi, '-')}.png` });
  }
}

// ---------- sign-in and roles ----------
await step('auth', async () => {
  await login('manager', 'nope');
  const msg = await page.locator(tid('login-error')).innerText();
  ok('wrong password shows message', msg.includes('Wrong user name or password'), msg);
  await page.goto(APP + '/members/x');
  await page.waitForURL('**/login?returnUrl=%2Fmembers%2Fx');
  ok('guard sends to sign-in with returnUrl', true);
  await login('frontdesk', 'Desk@123');
  await page.waitForURL('**/dashboard');
  const nav = await page.locator('nav.nav-list').innerText();
  ok('front desk menu hides Trainers / Plans', !nav.includes('Trainers') && !nav.includes('Plans'), nav);
  await page.goto(APP + '/trainers');
  await page.locator(tid('access-denied')).waitFor();
  ok('front desk /trainers -> Access denied page', true);
});

// ---------- dashboard + New Check-in ----------
await step('check-in', async () => {
  await page.goto(APP + '/dashboard');
  const kpi = page.locator(tid('kpi-checkins'));
  await kpi.waitFor();
  const before = Number(await kpi.innerText());
  await page.click(tid('nav-new-checkin'));
  await pickMember('Marcus V', 'Marcus Vance');
  await page.locator(tid('picked-member')).waitFor();
  await page.click(tid('checkin-save'));
  const toast = page.locator(tid('toast'), { hasText: 'Marcus Vance checked in at' });
  await toast.waitFor();
  ok("toast '<Name> checked in at <time>'", /Marcus Vance checked in at \d\d:\d\d [AP]M/.test(await toast.innerText()));
  await expect(kpi).toHaveText(String(before + 1));
  ok('dashboard KPI refreshes immediately', true);

  await page.click(tid('nav-new-checkin'));
  await pickMember('John Sm', 'John Smith');
  const err = await page.locator(tid('member-error')).innerText();
  ok('frozen member cannot check in (inline reason)', err.toLowerCase().includes('frozen'), err);
  await page.keyboard.press('Escape');
});

// ---------- members ----------
await step('members', async () => {
  await page.goto(APP + '/members');
  await page.locator(tid('member-row')).first().waitFor();
  const summary = await page.locator(tid('paging-summary')).innerText();
  ok('directory paging summary', summary.includes('Showing 1 to 10 of 30 entries'), summary);
  await fill(page.locator(tid('header-search')), 'john');
  await expect(page.locator(tid('member-row'))).toHaveCount(2);
  ok('search highlights matches', (await page.locator(`${tid('member-row')} mark.hl`).count()) >= 2);
  ok('search kept in query string', page.url().includes('q=john'), page.url());

  const row = page.locator(tid('member-row'), { hasText: 'John Smith' });
  await row.locator('app-row-menu button').first().click();
  ok('Check-In disabled for frozen member', await row.locator('button[role=menuitem]', { hasText: 'Check-In' }).isDisabled());
  await page.mouse.click(700, 140);
  await expect(row.locator('[role=menu]')).toHaveCount(0);
  ok('row menu closes on outside click', true);
  await fill(page.locator(tid('header-search')), '');
  await page.waitForTimeout(700);

  await page.click(tid('members-filter'));
  await page.locator('#mf-Expired').check();
  await page.click(tid('filter-apply'));
  await page.waitForTimeout(800);
  const statuses = await page.locator(`${tid('member-row')} app-status-badge`).allInnerTexts();
  ok('status filter Expired', statuses.length > 0 && statuses.every(s => s.includes('Expired')), statuses);
  ok('filter kept in query string', page.url().includes('status=Expired'), page.url());
  await page.locator(tid('member-row')).first().locator('button').first().click();
  await page.waitForURL(/\/members\/[0-9a-f-]{36}$/);
  await page.click(tid('profile-back'));
  await page.waitForURL('**status=Expired**');
  ok('Back returns to the same filters', true);

  await page.goto(APP + '/members');
  await page.click(tid('add-member'));
  await fill(tid('member-name'), 'R2D2');
  await page.click(tid('member-save'));
  await expect(page.locator('.invalid-feedback').first()).toContainText('letters');
  ok('add member name validation', true);
  await fill(tid('member-name'), 'Lina Haddad');
  await page.click(tid('member-save'));
  await page.waitForURL(/\/members\/[0-9a-f-]{36}$/);
  await page.locator(tid('profile-number')).waitFor();
  ok('new member gets the next TF number', /#TF-10\d\d/.test(await page.locator(tid('profile-number')).innerText()));
  await page.locator(tid('no-plan')).waitFor();
  ok('new member has no plan', true);

  await page.click(tid('profile-sell'));
  await page.locator('#sell-plan option', { hasText: 'Quarterly' }).first().waitFor({ state: 'attached' });
  const options = await page.locator('#sell-plan option').allInnerTexts();
  await page.selectOption('#sell-plan', { label: options.find(o => o.startsWith('Quarterly')) });
  await page.click(tid('sell-save'));
  await expect(page.locator(tid('plan-name'))).toHaveText('Quarterly');
  ok('sell plan -> Current Plan card', true);

  await page.click(tid('edit-profile'));
  await fill('#edit-name', 'Lina Haddad-Odeh');
  await page.click(tid('edit-member-save'));
  await expect(page.locator(tid('profile-name'))).toHaveText('Lina Haddad-Odeh');
  ok('edit member saved', true);

  await page.goto(APP + '/members?q=Alex');
  await page.locator(tid('member-row'), { hasText: 'Alex Rivera' }).locator('button').first().click();
  await expect(page.locator(tid('freezes-used'))).toHaveText(/2 \/ 3/);
  ok('Alex freezes 2 / 3', true);
  await expect(page.locator(tid('guest-passes'))).toHaveText(/1 \/ 5/);
  ok('Alex guest passes 1 / 5', true);
  await page.locator(tid('activity-row')).first().waitFor();
  const acts = await page.locator(tid('activity-row')).count();
  ok('recent activity shows up to 7', acts >= 1 && acts <= 7, acts);
  ok('activity includes class attendance', (await page.locator(tid('activity-row'), { hasText: 'Class Attendance' }).count()) >= 1);
  const url = page.url();
  await page.reload();
  await page.locator(tid('profile-name')).waitFor();
  ok('profile URL is bookmarkable (refresh)', page.url() === url);
  await page.goto(APP + '/members/00000000-0000-0000-0000-000000000001');
  await page.locator(tid('member-not-found')).waitFor();
  ok("unknown member -> 'Member not found'", true);
});

// ---------- freeze ----------
await step('freeze', async () => {
  await page.goto(APP + '/members?q=Marcus%20Vance');
  const row = page.locator(tid('member-row'), { hasText: 'Marcus Vance' });
  await row.locator('app-row-menu button').first().click();
  await row.locator('button[role=menuitem]', { hasText: 'Freeze Membership' }).click();
  await page.waitForURL('**/freeze');
  await page.locator(tid('current-end')).waitFor();
  const end = await page.locator(tid('current-end')).innerText();
  const confirm = page.locator(tid('confirm-freeze'));
  ok('Confirm Freeze disabled until a reason is chosen', await confirm.isDisabled());
  await page.click(tid('duration-2'));
  await expect(page.locator(tid('impact-duration'))).toHaveText('2 Months');
  ok('Freeze Duration updates', true);
  const newEnd = await page.locator(tid('new-end')).innerText();
  ok('New End Date computed', newEnd !== '—' && newEnd !== end, newEnd);
  await fill(tid('freeze-start'), '');
  await page.locator('#freeze-notes').click();
  await expect(page.locator(tid('new-end'))).toHaveText('—');
  ok("New End Date shows '—' without a start date", true);
  await fill(tid('freeze-start'), mdy(new Date()));
  await page.locator('#freeze-notes').click();
  ok('3 Months disabled (needs 90 of 60 days)', await page.locator(tid('duration-3')).isDisabled());
  await page.selectOption(tid('freeze-reason'), { label: 'Extended Travel' });
  await fill('#freeze-notes', 'Summer trip');
  await confirm.click();
  await page.waitForURL(/\/members\/[0-9a-f-]{36}$/);
  await page.locator(tid('toast'), { hasText: 'frozen' }).waitFor();
  ok('freeze toast', true);
  await page.goto(APP + '/members?q=Marcus%20Vance');
  await expect(page.locator(tid('member-row'), { hasText: 'Marcus Vance' }).locator('app-status-badge')).toContainText('Frozen');
  ok('row badge becomes Frozen', true);

  await page.goto(APP + '/members?q=Alice');
  await page.locator(tid('member-row'), { hasText: 'Alice Williams' }).locator('button').first().click();
  await page.waitForURL(/\/members\/[0-9a-f-]{36}$/);
  await page.goto(page.url() + '/freeze');
  await page.locator(tid('duration-1')).waitFor();
  await page.waitForTimeout(500);
  ok('15 freeze days -> 1 Month disabled', await page.locator(tid('duration-1')).isDisabled());
  await fill('#freeze-notes', 'x');
  await page.click('text=Back to Member Profile');
  await page.locator('text=Discard changes?').waitFor();
  ok("leaving with changes asks 'Discard changes?'", true);
  await page.click("button:has-text('Discard')");
  await page.waitForURL(/\/members\/[0-9a-f-]{36}$/);
});

// ---------- classes ----------
await step('classes', async () => {
  await page.goto(APP + '/classes');
  await page.locator(tid('class-count')).waitFor();
  await page.click(tid('add-class'));
  await page.locator(tid('class-name')).waitFor();
  await page.click(tid('class-save'));
  await expect(page.locator('.dialog-body .invalid-feedback')).toHaveCount(3);
  ok('Add New Class required errors', true);
  const d = addDays(new Date(), 2);
  await fill(tid('class-name'), 'QA Bootcamp');
  await fill(tid('class-date'), mdy(d));
  await fill(tid('class-time'), '07:00 AM');
  await page.selectOption(tid('class-studio'), { label: 'Zen Room (20 spots)' });
  await fill(tid('class-capacity'), '25');
  await page.locator('#cd-description').click();
  await expect(page.locator("text=Not above the room's capacity (20).")).toHaveCount(1);
  ok('capacity above the room blocked', true);
  await fill(tid('class-capacity'), '2');
  await page.selectOption(tid('class-trainer'), { label: 'Lina Saleh' });
  await page.click(tid('class-save'));
  await page.locator(tid('toast'), { hasText: 'QA Bootcamp scheduled' }).waitFor();
  ok('Schedule Class toast', true);
  await fill(tid('schedule-date'), iso(d));
  await page.locator(tid('schedule-date')).dispatchEvent('change');
  const row = page.locator(tid('session-row'), { hasText: 'QA Bootcamp' });
  await row.waitFor();
  ok('new class appears in the schedule', true);
  ok('date kept in the query string', page.url().includes(`date=${iso(d)}`), page.url());

  await page.click(tid('add-class'));
  await fill(tid('class-name'), 'Clash Class');
  await fill(tid('class-date'), mdy(d));
  await fill(tid('class-time'), '07:15 AM');
  await page.selectOption(tid('class-trainer'), { label: 'Lina Saleh' });
  await page.click(tid('class-save'));
  await expect(page.locator('.dialog-body .invalid-feedback').first()).toContainText('overlapping');
  ok('trainer double booking rejected on the field', true);
  await page.click(".dialog-footer button:has-text('Cancel')");
  await page.locator('text=Discard changes?').waitFor();
  await page.click("button:has-text('Discard')");
  await expect(page.locator('mat-dialog-container')).toHaveCount(0);

  let last = '';
  for (const name of ['Jane Doe', 'Alex Rivera', 'Liam Wilson']) {
    await row.locator('app-row-menu button').first().click();
    await row.locator('button[role=menuitem]', { hasText: 'Book Session' }).click();
    await pickMember(name.split(' ')[0], name);
    await page.click(tid('confirm-booking'));
    await expect(page.locator('mat-dialog-container')).toHaveCount(0);
    last = await page.locator(tid('toast'), { hasText: name }).last().innerText();
  }
  ok('third booking goes to the waitlist', last.toLowerCase().includes('waitlist'), last);
  await expect(row.locator(tid('waitlist'))).toHaveText('Waitlist: 1');
  ok('row shows Full + Waitlist: 1', (await row.locator('app-status-badge').innerText()).includes('Full'));

  await row.locator('app-row-menu button').first().click();
  await row.locator('button[role=menuitem]', { hasText: 'View Class' }).click();
  await page.locator(tid('class-enrolment')).waitFor();
  ok('View mode: editors disabled', await page.locator(tid('class-name')).isDisabled());
  const enr = await page.locator(tid('class-enrolment')).innerText();
  ok('View shows enrolment and waitlist', enr.includes('2 / 2 enrolled') && enr.includes('Waitlist: 1'), enr);
  await page.click(tid('class-edit'));
  ok('branch locked when the class has bookings', await page.locator('#cd-branch').isDisabled());
  await fill(tid('class-capacity'), '1');
  await page.locator('#cd-description').click();
  await expect(page.locator('text=Not below the current enrolment (2).')).toHaveCount(1);
  ok('capacity below enrolment blocked', true);
  await fill(tid('class-capacity'), '3');
  await page.click(tid('class-save'));
  await page.locator(tid('toast'), { hasText: 'Class updated' }).waitFor();
  ok('Edit Class saved', true);
  await expect(row.locator(tid('waitlist'))).toHaveCount(0);
  ok('waitlisted member promoted when capacity grows', true);

  await row.locator('app-row-menu button').first().click();
  await row.locator('button[role=menuitem]', { hasText: 'Cancel Class' }).click();
  await page.click("button:has-text('Cancel Class')");
  await expect(row.locator('app-status-badge')).toHaveText(/Cancelled/);
  ok('Cancel Class -> Cancelled', true);

  await page.selectOption(tid('schedule-branch'), { label: 'All Branches' });
  await page.click(tid('view-week'));
  await page.waitForTimeout(1000);
  ok('Week view groups by day', (await page.locator('.day-head').count()) >= 5);
  ok('All Branches shows the branch', (await page.locator(tid('session-row'), { hasText: 'Uptown' }).count()) > 0);
});

// ---------- trainers (manager) ----------
await step('trainers', async () => {
  await page.click(tid('user-menu'));
  await page.click(tid('sign-out'));
  await login('manager', 'Manager@123');
  await page.waitForURL('**/dashboard');
  await page.goto(APP + '/trainers');
  await page.locator(tid('trainer-row')).first().waitFor();
  ok('trainer directory paging', (await page.locator(tid('paging-summary')).innerText()).includes('of 12 entries'));
  await page.click(tid('trainers-filter'));
  await page.click(tid('filter-specialties'));
  await page.locator('mat-option', { hasText: 'Cycling' }).click();
  await page.keyboard.press('Escape');
  await page.locator('#tfs-Active').check();
  await page.click(tid('filter-apply'));
  await expect(page.locator(tid('trainer-row'))).toHaveCount(2);
  const names = (await page.locator(`${tid('trainer-row')} td:first-child`).allInnerTexts()).map(n => n.split('\n').pop().trim()).sort();
  ok('filter Specialty Cycling + Active', JSON.stringify(names) === JSON.stringify(['Elena Rodriguez', 'Nora Ali']), names);

  await page.goto(APP + '/trainers?q=Sarah');
  const row = page.locator(tid('trainer-row'), { hasText: 'Sarah Jenkins' });
  await row.locator('app-row-menu button').first().click();
  await row.locator('button[role=menuitem]', { hasText: 'View Trainer' }).click();
  await page.locator(tid('mode-badge'), { hasText: 'View mode' }).waitFor();
  ok('View mode: editors disabled', await page.locator(tid('t-name')).isDisabled());
  await page.click(tid('edit-trainer'));
  await expect(page.locator(tid('mode-badge'))).toContainText('Update mode');
  ok('Edit Trainer switches in place, URL ends with /edit', page.url().endsWith('/edit'), page.url());
  await fill(tid('t-email'), 'm.lee@titanfitness.com');
  await page.click(tid('save'));
  await expect(page.locator(tid('t-email-error'))).toContainText('already exists');
  ok('duplicate email -> server message on the field', true);
  await fill(tid('t-email'), 's.jenkins@titanfitness.com');
  await fill(tid('t-phone'), '+1 (555) 019-9999');
  await page.click(tid('save'));
  await page.locator(tid('toast'), { hasText: 'Trainer updated' }).waitFor();
  await expect(page.locator(tid('mode-badge'))).toContainText('View mode');
  ok("toast 'Trainer updated' and back to View mode", !page.url().endsWith('/edit'), page.url());
  await page.click(tid('edit-trainer'));
  await fill(tid('t-name'), 'Changed');
  await page.click(tid('cancel'));
  await page.locator('text=Discard changes?').waitFor();
  await page.click("button:has-text('Discard')");
  await expect(page.locator(tid('mode-badge'))).toContainText('View mode');
  ok('Cancel from View returns to View mode', true);

  await page.goto(APP + '/trainers/new');
  await page.locator(tid('save')).waitFor();
  await page.click(tid('save'));
  await expect(page.locator('.invalid-feedback')).toHaveCount(3);
  ok('Add Trainer required errors', true);
  await fill(tid('t-name'), 'Rami Aziz');
  await page.selectOption(tid('t-branch'), { label: 'Uptown Branch' });
  await fill(tid('t-email'), 'r.aziz@titanfitness.com');
  await fill(tid('t-phone'), 'abc');
  await page.click(tid('save'));
  await expect(page.locator('text=Enter a valid phone number')).toHaveCount(1);
  ok('phone format validation', true);
  await fill(tid('t-phone'), '');
  await page.click(tid('save'));
  await page.locator(tid('mode-badge'), { hasText: 'View mode' }).waitFor();
  await expect(page.locator('.page-subtitle')).toHaveText(/#TR-\d{4}/);
  ok('new trainer gets a TR id', true);
  await page.goto(APP + '/trainers/00000000-0000-0000-0000-000000000001');
  await page.locator(tid('trainer-not-found')).waitFor();
  ok("unknown trainer -> 'Trainer not found'", true);
});

// ---------- plans ----------
await step('plans', async () => {
  await page.goto(APP + '/plans');
  await page.locator(tid('plan-row')).first().waitFor();
  ok('freezeAllowance pipe "60 days / 3 freezes"', (await page.locator(tid('plan-row'), { hasText: 'Annual Pro' }).innerText()).includes('60 days / 3 freezes'));
  ok('freezeAllowance pipe "None"', (await page.locator(tid('plan-row'), { hasText: 'Monthly' }).first().innerText()).includes('None'));
  await page.click("th:has-text('Price')");
  await page.waitForTimeout(600);
  await page.click("th:has-text('Price')");
  await expect(page.locator(tid('plan-row')).first()).toContainText('Two-Year Elite');
  ok('sort by price descending', true);
  await page.click(tid('plans-filter'));
  await page.click(tid('chip-1'));
  await page.locator('#ps-Retired').check();
  await page.click(tid('filter-apply'));
  await expect(page.locator(tid('plan-row'))).toHaveCount(1);
  ok('filter 1 month + Retired -> Student 2024', (await page.locator(tid('plan-row')).innerText()).includes('Student 2024'));

  await page.goto(APP + '/plans/new');
  await fill(tid('p-name'), 'QA Plan');
  await fill(tid('p-price'), '45.5');
  await fill(tid('p-duration'), '2');
  await fill(tid('p-freezes'), '1');
  await page.click(tid('save'));
  await expect(page.locator(tid('p-freezes-error'))).toHaveCount(1);
  ok('freezes must be 0 when freeze days is 0', true);
  await fill(tid('p-days'), '10');
  await page.click(tid('access-AllBranches'));
  await page.locator(tid('p-published')).check();
  await page.click(tid('save'));
  await page.locator(tid('toast'), { hasText: 'QA Plan created' }).waitFor();
  await page.locator(tid('mode-badge'), { hasText: 'View mode' }).waitFor();
  ok("toast '<Plan> created' then View screen", true);
  await page.click(tid('edit-plan'));
  await fill(tid('p-name'), 'Monthly');
  await page.click(tid('save'));
  await expect(page.locator(tid('p-name-error'))).toContainText('already exists');
  ok('duplicate plan name -> 409 on the field', true);
  await fill(tid('p-name'), 'QA Plan Plus');
  await page.click(tid('save'));
  await page.locator(tid('toast'), { hasText: 'Plan updated' }).waitFor();
  ok('plan updated', true);
});

// ---------- 401 / not found / redirect ----------
await step('errors', async () => {
  await page.goto(APP + '/members');
  await page.locator(tid('member-row')).first().waitFor();
  await page.evaluate(() => {
    const s = JSON.parse(localStorage.getItem('tf.session'));
    s.token = 'bad.token';
    localStorage.setItem('tf.session', JSON.stringify(s));
  });
  await page.goto(APP + '/members?page=2');
  await page.waitForURL('**/login**');
  ok('401 -> sign-in page with returnUrl', page.url().includes('returnUrl=%2Fmembers%3Fpage%3D2'), page.url());
  await fill('#userName', 'manager');
  await fill('#password', 'Manager@123');
  await page.click(tid('login-submit'));
  await page.waitForURL('**/members?page=2');
  ok('after sign-in, back to the same URL', true);
  await page.goto(APP + '/no-such-page');
  await page.locator(tid('not-found-page')).waitFor();
  ok('unknown URL -> Page not found', true);
  await page.goto(APP + '/');
  await page.waitForURL('**/dashboard');
  ok('default redirect to /dashboard', true);
});

// ---------- member self-service ----------
await step('member-portal', async () => {
  await page.click(tid('user-menu'));
  await page.click(tid('sign-out'));
  await login('alex', 'Member@123');
  await page.waitForURL('**/member/classes');
  await page.locator(tid('member-class')).first().waitFor();
  ok('member sees the class list', true);
  await page.locator(tid('member-class'), { hasText: 'Sunrise Yoga' }).first().locator("a:has-text('Book')").click();
  await page.locator(tid('eligibility')).waitFor();
  ok('Eligibility: Active Membership', (await page.locator(tid('eligibility')).innerText()).includes('Active Membership'));
  await page.click(tid('member-confirm'));
  await page.locator(tid('booking-done')).waitFor();
  ok('member books a class', true);
  await page.goto(APP + '/dashboard');
  await page.waitForURL('**/member/classes');
  ok('member cannot open staff screens', true);
});

ok('no uncaught errors in the browser', pageErrors.length === 0, pageErrors.slice(0, 3));
await browser.close();

const failed = results.filter(r => !r[0]);
console.log(`\nUI TESTS: ${results.length - failed.length} passed, ${failed.length} failed`);
if (failed.length) console.log(`Screenshots of failures: ${SHOTS}`);
process.exit(failed.length ? 1 : 0);
