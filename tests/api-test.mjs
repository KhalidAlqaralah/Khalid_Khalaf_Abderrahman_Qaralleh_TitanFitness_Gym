// API tests for every endpoint and business rule (needs a FRESHLY SEEDED database).
// Run: node api-test.mjs   (API on http://localhost:5162)
const BASE = (process.env.API_URL ?? 'http://localhost:5162') + '/api';
const results = [];

// A failed request earlier can break a later step; print the totals instead of a bare stack trace.
process.on('uncaughtException', e => {
  const failed = results.filter(r => !r).length;
  console.log(`\nSTOPPED EARLY after a failure above (${e.message})`);
  console.log(`API TESTS: ${results.length - failed} passed, ${failed + 1} failed`);
  process.exit(1);
});

async function call(method, path, { body, token, expect, name } = {}) {
  const res = await fetch(BASE + path, {
    method,
    headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  const text = await res.text();
  let payload = null;
  try { payload = text ? JSON.parse(text) : null; } catch { payload = text; }
  const passed = expect === undefined || res.status === expect;
  results.push(passed);
  if (expect !== undefined) console.log(`${passed ? 'PASS' : 'FAIL'} ${name ?? method + ' ' + path}${passed ? '' : `: got ${res.status}, expected ${expect}: ${text.slice(0, 300)}`}`);
  return [res.status, payload];
}

function check(cond, name, detail = '') {
  results.push(!!cond);
  console.log(`${cond ? 'PASS' : 'FAIL'} ${name}${cond ? '' : '  ' + JSON.stringify(detail).slice(0, 400)}`);
}

const pad = n => String(n).padStart(2, '0');
const iso = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
const addDays = (d, n) => { const c = new Date(d.getFullYear(), d.getMonth(), d.getDate()); c.setDate(c.getDate() + n); return c; };
const fromIso = s => { const [y, m, d] = s.split('-').map(Number); return new Date(y, m - 1, d); };
const daysBetween = (a, b) => Math.round((Date.UTC(b.getFullYear(), b.getMonth(), b.getDate()) - Date.UTC(a.getFullYear(), a.getMonth(), a.getDate())) / 86400000);
const today = addDays(new Date(), 0);
const has = (obj, ...keys) => keys.every(k => obj && Object.prototype.hasOwnProperty.call(obj, k));

async function login(userName, password) {
  const [, b] = await call('POST', '/auth/login', { body: { userName, password }, expect: 200, name: `login ${userName}` });
  return b.token;
}

// ---------- auth ----------
await call('GET', '/members', { expect: 401, name: 'no token -> 401' });
await call('POST', '/auth/login', { body: { userName: 'manager', password: 'wrong' }, expect: 401, name: 'bad password -> 401' });
const M = await login('manager', 'Manager@123');
const F = await login('frontdesk', 'Desk@123');
const A = await login('alex', 'Member@123');
await call('GET', '/members', { token: 'garbage.token', expect: 401, name: 'bad token -> 401' });
await call('GET', '/trainers', { token: F, expect: 403, name: 'front desk trainers -> 403' });
await call('GET', '/plans', { token: F, expect: 403, name: 'front desk plans -> 403' });
await call('GET', '/trainers/lookup', { token: F, expect: 200, name: 'front desk trainer lookup -> 200' });
await call('GET', '/plans/lookup', { token: F, expect: 200, name: 'front desk plan lookup -> 200' });
await call('GET', '/members', { token: A, expect: 403, name: 'member cannot use the staff API -> 403' });
const [, me] = await call('GET', '/auth/me', { token: A, expect: 200, name: 'GET /auth/me' });
check(me.role === 'Member' && me.memberId, 'member token carries the member id');

// ---------- branches ----------
const [, branches] = await call('GET', '/branches', { token: F, expect: 200, name: 'GET /branches' });
const down = branches.find(b => b.name === 'Downtown');
const up = branches.find(b => b.name === 'Uptown');
check(down.studios.length === 4, 'Downtown has 4 studios');

// ---------- dashboard (split endpoints) ----------
const [, ci] = await call('GET', `/dashboard/check-ins-today?branchId=${down.id}`, { token: F, expect: 200, name: 'dashboard check-ins-today' });
const [, am] = await call('GET', `/dashboard/active-members?branchId=${down.id}`, { token: F, expect: 200, name: 'dashboard active-members' });
const [, uc] = await call('GET', `/dashboard/upcoming-classes?branchId=${down.id}&take=10`, { token: F, expect: 200, name: 'dashboard upcoming-classes' });
check(ci.today >= 0 && am.activeMembers > 0, 'dashboard KPIs present', { ci, am });
check(uc.every(c => c.state !== 'Completed'), 'upcoming classes has no completed class');
await call('GET', '/dashboard/upcoming-classes?take=0', { token: F, expect: 400, name: 'upcoming take=0 -> 400' });

// ---------- members ----------
const [, page] = await call('GET', '/members?page=1&pageSize=10&sortBy=name', { token: F, expect: 200, name: 'members page 1' });
check(page.totalCount === 30 && page.items.length === 10 && page.totalPages === 3, 'members paging 30 / 10 / 3 pages', page.totalCount);
const [, p2] = await call('GET', '/members?search=tf-1003', { token: F, expect: 200, name: 'members search by number' });
check(p2.totalCount === 1 && p2.items[0].fullName === 'John Smith', 'search by number finds John Smith');
const [, p3] = await call('GET', '/members?search=frozen', { token: F, expect: 200, name: 'members search by status' });
check(p3.items.some(i => i.fullName === 'John Smith'), 'search by status text');
const [, p4] = await call('GET', `/members?branchId=${up.id}&statuses=Active&statuses=Frozen`, { token: F, expect: 200, name: 'members branch + status filter' });
check(p4.items.every(i => i.branchName === 'Uptown' && ['Active', 'Frozen'].includes(i.status)), 'branch + status filters applied');
await call('GET', '/members?sortBy=shoe', { token: F, expect: 400, name: 'bad sortBy -> 400' });
await call('GET', '/members?pageSize=500', { token: F, expect: 400, name: 'pageSize 500 -> 400' });
await call('GET', '/members?sortBy=lastVisit&sortDirection=Desc&pageSize=5', { token: F, expect: 200, name: 'sort by last visit' });

const [, bad] = await call('POST', '/members', { body: { fullName: 'J', homeBranchId: null }, token: F, expect: 400, name: 'add member invalid -> 400' });
check(has(bad?.errors, 'fullName', 'homeBranchId'), '400 has field errors fullName + homeBranchId', bad?.errors);
await call('POST', '/members', { body: { fullName: 'R2-D2', homeBranchId: down.id }, token: F, expect: 400, name: 'name with digits -> 400' });
const [, created] = await call('POST', '/members', { body: { fullName: '  Nadia   Hammad ', homeBranchId: down.id }, token: F, expect: 201, name: 'add member' });
check(created.membershipNumber === 'TF-1031', 'next membership number TF-1031', created);
let [, nadia] = await call('GET', `/members/${created.id}`, { token: F, expect: 200, name: 'get new member' });
check(nadia.fullName === 'Nadia Hammad' && nadia.joinedOn === iso(today) && nadia.createdBy === 'frontdesk', 'name trimmed, joined today, createdBy recorded', nadia);
await call('GET', `/members/${created.id}/current-membership`, { token: F, expect: 404, name: 'new member has no membership -> 404' });
await call('PUT', `/members/${created.id}`, { body: { fullName: 'Nadia Hammad-Saleh', homeBranchId: up.id }, token: F, expect: 204, name: 'edit member' });
[, nadia] = await call('GET', `/members/${created.id}`, { token: F, expect: 200, name: 'get edited member' });
check(nadia.homeBranchName === 'Uptown', 'edited branch saved');
await call('GET', '/members/00000000-0000-0000-0000-000000000001', { token: F, expect: 404, name: 'unknown member -> 404' });
await call('PUT', '/members/00000000-0000-0000-0000-000000000001', { body: { fullName: 'Abc', homeBranchId: up.id }, token: F, expect: 404, name: 'edit unknown member -> 404' });

async function member(name) {
  const [, r] = await call('GET', `/members?search=${encodeURIComponent(name)}`, { token: F });
  return r.items[0];
}
const alex = await member('Alex Rivera');
const john = await member('John Smith');
const robert = await member('Robert Johnson');
const marcus = await member('Marcus Vance');
const alice = await member('Alice Williams');
const samir = await member('Samir Nasser');

const [, alexMs] = await call('GET', `/members/${alex.id}/current-membership`, { token: F, expect: 200, name: "Alex's membership" });
check(alexMs.freezesUsed === 2 && alexMs.maxFreezes === 3 && alexMs.guestPassesIssued === 1, 'Alex 2/3 freezes, 1 guest pass', alexMs);
const [, act] = await call('GET', `/members/${alex.id}/activity?take=7`, { token: F, expect: 200, name: "Alex's activity" });
check(act.some(a => a.kind === 'ClassAttendance') && act.length <= 7, 'activity mixes check-ins and classes');
await call('GET', `/members/${alex.id}/activity?take=0`, { token: F, expect: 400, name: 'activity take=0 -> 400' });

// ---------- check-ins ----------
const now = new Date();
const t5 = `${pad(now.getHours())}:${pad(now.getMinutes() - (now.getMinutes() % 5))}:00`;
await call('POST', '/check-ins', { body: { memberId: marcus.id, branchId: down.id, date: iso(today), time: t5, notes: 'Front door' }, token: F, expect: 201, name: 'check in an active member' });
const [, ci2] = await call('GET', `/dashboard/check-ins-today?branchId=${down.id}`, { token: F });
check(ci2.today === ci.today + 1, 'check-ins today goes up by 1');
await call('POST', '/check-ins', { body: { memberId: john.id, branchId: up.id, date: iso(today), time: '00:00:00' }, token: F, expect: 409, name: 'frozen member check-in -> 409' });
await call('POST', '/check-ins', { body: { memberId: robert.id, branchId: down.id, date: iso(today), time: '00:00:00' }, token: F, expect: 409, name: 'expired member check-in -> 409' });
await call('POST', '/check-ins', { body: { memberId: samir.id, branchId: down.id, date: iso(today), time: '00:00:00' }, token: F, expect: 409, name: 'home-branch-only member elsewhere -> 409' });
await call('POST', '/check-ins', { body: { memberId: marcus.id, branchId: down.id, date: iso(addDays(today, 1)), time: '08:00:00' }, token: F, expect: 422, name: 'future check-in -> 422' });
await call('POST', '/check-ins', { body: { memberId: marcus.id, branchId: down.id, date: iso(addDays(today, -8)), time: '08:00:00' }, token: F, expect: 422, name: 'check-in 8 days ago -> 422' });
await call('POST', '/check-ins', { body: { memberId: null, branchId: down.id, date: null, time: null }, token: F, expect: 400, name: 'check-in missing fields -> 400' });
await call('POST', '/check-ins', { body: { memberId: marcus.id, branchId: down.id, date: iso(today), time: '00:00:00', notes: 'x'.repeat(251) }, token: F, expect: 400, name: 'notes 251 chars -> 400' });

// ---------- freeze ----------
const [, mv] = await call('GET', `/members/${marcus.id}/current-membership`, { token: F, expect: 200, name: "Marcus's membership" });
check(mv.canFreeze && mv.remainingFreezeDays === 60, 'Marcus can freeze, 60 days', mv);
const fz = body => call('POST', `/memberships/${mv.id}/freezes`, { body, token: F });
let [s, e] = await fz({ startDate: iso(addDays(today, -1)), durationInMonths: 1, reason: 'ExtendedTravel' });
check(s === 422 && has(e?.errors, 'startDate'), 'freeze in the past -> 422 on startDate', [s, e]);
[s] = await fz({ startDate: iso(addDays(today, 1)), durationInMonths: 4, reason: 'ExtendedTravel' });
check(s === 400, 'freeze 4 months -> 400');
[s, e] = await fz({ startDate: iso(addDays(today, 1)), durationInMonths: 1, reason: null });
check(s === 400 && has(e?.errors, 'reason'), 'freeze without reason -> 400');
[s, e] = await fz({ startDate: iso(addDays(today, 1)), durationInMonths: 3, reason: 'Medical' });
check(s === 422 && has(e?.errors, 'durationInMonths'), 'freeze 3 months (90 > 60 days) -> 422', [s, e]);
[s] = await fz({ startDate: mv.endDate, durationInMonths: 1, reason: 'Medical' });
check(s === 422, 'freeze starting on the end date -> 422');
const start = addDays(today, 1);
let done;
[s, done] = await fz({ startDate: iso(start), durationInMonths: 1, reason: 'ExtendedTravel', notes: 'Trip' });
check(s === 200, 'freeze 1 month ok', [s, done]);
const [, mv2] = await call('GET', `/members/${marcus.id}/current-membership`, { token: F });
check(daysBetween(fromIso(mv.endDate), fromIso(mv2.endDate)) === done.daysUsed, 'end date moved out by the frozen days', { old: mv.endDate, new: mv2.endDate, done });
[s, e] = await fz({ startDate: iso(addDays(start, 3)), durationInMonths: 1, reason: 'Other' });
check(s === 409 || s === 422, 'overlapping freeze rejected', [s, e]);
const [, q] = await call('GET', `/members/${alice.id}/current-membership`, { token: F });
[s, e] = await call('POST', `/memberships/${q.id}/freezes`, { body: { startDate: iso(start), durationInMonths: 1, reason: 'Other' }, token: F });
check(s === 422, 'Quarterly (15 days) cannot freeze 1 month', [s, e]);
const [, jm] = await call('GET', `/members/${john.id}/current-membership`, { token: F });
check(!jm.canFreeze && jm.status === 'Frozen', 'John is frozen and cannot freeze again');
await call('POST', `/memberships/${jm.id}/freezes`, { body: { startDate: iso(start), durationInMonths: 1, reason: 'Other' }, token: F, expect: 409, name: 'freeze a frozen membership -> 409' });

// ---------- plans ----------
const [, plans] = await call('GET', '/plans?page=1&pageSize=10&sortBy=price&sortDirection=Desc', { token: M, expect: 200, name: 'plans page 1 by price desc' });
check(plans.totalCount === 12 && plans.totalPages === 2 && plans.items[0].name === 'Two-Year Elite', 'plans paging + sort', plans.items.map(p => p.name));
const [, opts] = await call('GET', '/plans/filter-options', { token: M, expect: 200, name: 'plan filter options' });
check(JSON.stringify(opts.durations) === '[1,3,6,12,24]' && opts.minPrice === 49 && opts.maxPrice === 1599, 'filter options: durations and price bounds', opts);
const [, f1] = await call('GET', '/plans?durations=1&access=HomeBranchOnly&statuses=Published', { token: M, expect: 200, name: 'plan filters' });
check(f1.totalCount === 3 && f1.items.every(p => p.durationInMonths === 1 && p.accessScope === 'HomeBranchOnly' && p.isPublished), 'duration + access + status filters', f1.items.map(p => p.name));
const [, f2] = await call('GET', '/plans?minPrice=100&maxPrice=300', { token: M, expect: 200, name: 'price range filter' });
check(f2.items.every(p => p.price >= 100 && p.price <= 300), 'price range applied');
await call('GET', '/plans?minPrice=300&maxPrice=100', { token: M, expect: 400, name: 'min > max -> 400' });
await call('GET', '/plans?statuses=Gone', { token: M, expect: 400, name: 'bad status -> 400' });
[, e] = await call('POST', '/plans', { body: { name: 'X', price: -1, durationInMonths: 40, isPublished: false, maxFreezeDays: 0, maxFreezes: 2 }, token: M, expect: 400, name: 'invalid plan -> 400' });
check(has(e?.errors, 'name', 'price', 'durationInMonths', 'maxFreezes'), 'plan 400 field errors', e?.errors);
await call('POST', '/plans', { body: { name: 'Annual Pro', price: 10, durationInMonths: 1, isPublished: true }, token: M, expect: 409, name: 'duplicate plan name -> 409' });
await call('POST', '/plans', { body: { name: 'Odd', price: 10.123, durationInMonths: 1, isPublished: true }, token: M, expect: 400, name: 'price with 3 decimals -> 400' });
const [, np] = await call('POST', '/plans', { body: { name: 'Test Flex', price: 120.5, durationInMonths: 2, isPublished: true, maxFreezeDays: null, maxFreezes: null, guestPassQuota: null, accessScope: null }, token: M, expect: 201, name: 'create plan with empty optionals' });
const [, tp] = await call('GET', `/plans/${np.id}`, { token: M, expect: 200, name: 'get new plan' });
check(tp.maxFreezeDays === 0 && tp.accessScope === 'HomeBranchOnly', 'empty optionals saved as 0 / Home branch only', tp);
await call('PUT', `/plans/${np.id}`, { body: { name: 'Test Flex', price: 130, durationInMonths: 2, isPublished: false, maxFreezeDays: 10, maxFreezes: 1, guestPassQuota: 1, accessScope: 'AllBranches' }, token: M, expect: 204, name: 'update plan' });
await call('PUT', `/plans/${np.id}`, { body: { name: 'Monthly', price: 130, durationInMonths: 2, isPublished: false }, token: M, expect: 409, name: 'rename to an existing name -> 409' });
await call('GET', '/plans/00000000-0000-0000-0000-000000000001', { token: M, expect: 404, name: 'unknown plan -> 404' });
const [, beforeAlex] = await call('GET', `/members/${alex.id}/current-membership`, { token: F });
const ap = plans.items.find(p => p.name === 'Annual Pro');
await call('PUT', `/plans/${ap.id}`, { body: { name: 'Annual Pro', price: 999, durationInMonths: 12, isPublished: true, maxFreezeDays: 10, maxFreezes: 1, guestPassQuota: 1, accessScope: 'AllBranches' }, token: M, expect: 204, name: 'change Annual Pro terms' });
const [, afterAlex] = await call('GET', `/members/${alex.id}/current-membership`, { token: F });
check(afterAlex.price === beforeAlex.price && afterAlex.maxFreezes === 3, 'a sold membership keeps its own terms');
await call('PUT', `/plans/${ap.id}`, { body: { name: 'Annual Pro', price: 899, durationInMonths: 12, isPublished: true, maxFreezeDays: 60, maxFreezes: 3, guestPassQuota: 5, accessScope: 'AllBranches' }, token: M, expect: 204, name: 'restore Annual Pro' });

// ---------- memberships ----------
const student = (await call('GET', '/plans?search=Student', { token: M }))[1].items[0];
await call('POST', '/memberships', { body: { memberId: created.id, planId: student.id, startDate: iso(today) }, token: F, expect: 409, name: 'sell a retired plan -> 409' });
const monthly = (await call('GET', '/plans?search=Monthly&durations=1', { token: M }))[1].items.find(p => p.name === 'Monthly');
await call('POST', '/memberships', { body: { memberId: created.id, planId: monthly.id, startDate: iso(addDays(today, -1)) }, token: F, expect: 422, name: 'start in the past -> 422' });
const [, sold] = await call('POST', '/memberships', { body: { memberId: created.id, planId: monthly.id, startDate: iso(today) }, token: F, expect: 201, name: 'sell Monthly' });
await call('POST', '/memberships', { body: { memberId: created.id, planId: monthly.id, startDate: iso(addDays(today, 3)) }, token: F, expect: 409, name: 'overlapping membership -> 409' });
const [, ren] = await call('POST', `/memberships/${sold.id}/renewals`, { body: { planId: null }, token: F, expect: 201, name: 'renew' });
check(ren.startDate === iso(addDays(fromIso(sold.endDate), 1)), 'renewal starts the day after the end');
const [, gp] = await call('POST', `/memberships/${alexMs.id}/guest-passes`, { token: F, expect: 200, name: 'issue a guest pass' });
await call('POST', `/memberships/${alexMs.id}/guest-passes/${gp.guestPassId}/use`, { body: { guestName: 'Sam' }, token: F, expect: 204, name: 'use the guest pass' });
await call('POST', `/memberships/${alexMs.id}/guest-passes/${gp.guestPassId}/use`, { body: { guestName: 'Sam' }, token: F, expect: 409, name: 'use it twice -> 409' });
await call('POST', `/memberships/${ren.id}/cancel`, { token: F, expect: 204, name: 'cancel the renewal' });
await call('POST', `/memberships/${ren.id}/cancel`, { token: F, expect: 409, name: 'cancel twice -> 409' });
const [, fr] = await call('GET', `/members/${marcus.id}/current-membership`, { token: F });
await call('POST', `/memberships/${fr.id}/freezes/${fr.freezes[0].id}/end`, { body: { endedOn: iso(addDays(start, 5)) }, token: F, expect: 204, name: 'end a freeze early' });
const [, fr2] = await call('GET', `/members/${marcus.id}/current-membership`, { token: F });
check(fr2.freezeDaysUsed === 6, 'unused freeze days given back', fr2.freezeDaysUsed);

// ---------- trainers ----------
const [, tr] = await call('GET', '/trainers?page=1&pageSize=10&sortBy=name', { token: M, expect: 200, name: 'trainers page 1' });
check(tr.totalCount === 12 && tr.items.length === 10, 'trainers paging', tr.totalCount);
const [, sp] = await call('GET', '/trainers/specialties', { token: M, expect: 200, name: 'trainer specialties' });
check(sp.includes('HIIT / Strength') && JSON.stringify(sp) === JSON.stringify([...sp].sort()), 'specialties distinct and sorted');
const [, tf] = await call('GET', `/trainers?branchIds=${up.id}&specialties=Cycling&statuses=Active`, { token: M, expect: 200, name: 'trainer filters' });
check(tf.items.map(t => t.name).join() === 'Elena Rodriguez', 'branch + specialty + status filters', tf.items);
const [, ts] = await call('GET', '/trainers?search=tr-1042', { token: M, expect: 200, name: 'trainer search by id' });
check(ts.totalCount === 1, 'search by trainer id');
[, e] = await call('POST', '/trainers', { body: { name: 'A', email: 'bad', branchId: null, phone: 'abc' }, token: M, expect: 400, name: 'invalid trainer -> 400' });
check(has(e?.errors, 'name', 'email', 'branchId', 'phone'), 'trainer 400 field errors', e?.errors);
await call('POST', '/trainers', { body: { name: 'Copy Cat', email: 'S.Jenkins@TitanFitness.com', branchId: down.id, isActive: true }, token: M, expect: 409, name: 'duplicate trainer email -> 409' });
const [, nt] = await call('POST', '/trainers', { body: { name: 'Zed Morgan', specialty: 'Kettlebells', email: 'z.morgan@titanfitness.com', branchId: down.id, phone: '+1 (555) 000-1234', isActive: true }, token: M, expect: 201, name: 'create trainer' });
check(nt.code === 'TR-1119', 'next trainer code TR-1119', nt);
const [, t1] = await call('GET', `/trainers/${nt.id}`, { token: M, expect: 200, name: 'get new trainer' });
check(t1.createdBy === 'manager', 'trainer createdBy recorded');
await call('PUT', `/trainers/${nt.id}`, { body: { name: 'Zed Morgan', specialty: 'Kettlebells', email: 'z.morgan@titanfitness.com', branchId: up.id, phone: null, isActive: false }, token: M, expect: 204, name: 'update trainer' });
await call('GET', '/trainers/00000000-0000-0000-0000-000000000001', { token: M, expect: 404, name: 'unknown trainer -> 404' });

// ---------- classes ----------
const [, look] = await call('GET', `/trainers/lookup?branchId=${down.id}`, { token: F, expect: 200, name: 'trainer lookup' });
const sarah = look.find(t => t.name === 'Sarah Jenkins');
const studioA = down.studios.find(x => x.name === 'Studio A');
const zen = down.studios.find(x => x.name === 'Zen Room');
const d10 = addDays(today, 10);
const body = (over = {}) => ({ className: 'Test Blast', branchId: down.id, trainerId: null, studioId: null, date: iso(d10), startTime: '14:00:00', durationInMinutes: 45, capacityLimit: null, description: null, ...over });
[, e] = await call('POST', '/class-sessions', { body: body({ className: 'AB', durationInMinutes: 50, capacityLimit: 101 }), token: F, expect: 400, name: 'invalid class -> 400' });
check(has(e?.errors, 'className', 'durationInMinutes', 'capacityLimit'), 'class 400 field errors', e?.errors);
await call('POST', '/class-sessions', { body: body({ date: iso(addDays(today, -1)) }), token: F, expect: 422, name: 'class in the past -> 422' });
await call('POST', '/class-sessions', { body: body({ studioId: zen.id, capacityLimit: 25 }), token: F, expect: 422, name: 'capacity above the room -> 422' });
await call('POST', '/class-sessions', { body: body({ studioId: up.studios[0].id }), token: F, expect: 422, name: 'room from another branch -> 422' });
const [, c1] = await call('POST', '/class-sessions', { body: body({ trainerId: sarah.id, studioId: studioA.id }), token: F, expect: 201, name: 'schedule a class' });
const [, c1d] = await call('GET', `/class-sessions/${c1.id}`, { token: F, expect: 200, name: 'get the class' });
check(c1d.capacityLimit === 20 && c1d.state === 'Upcoming' && c1d.trainerName === 'Sarah Jenkins', 'default capacity 20, Upcoming', c1d);
await call('POST', '/class-sessions', { body: body({ trainerId: sarah.id, startTime: '14:30:00' }), token: F, expect: 409, name: 'trainer double booked -> 409' });
await call('POST', '/class-sessions', { body: body({ studioId: studioA.id, startTime: '14:15:00' }), token: F, expect: 409, name: 'room double booked -> 409' });
await call('POST', '/class-sessions', { body: body({ trainerId: sarah.id, studioId: studioA.id, startTime: '14:45:00' }), token: F, expect: 201, name: 'back-to-back allowed' });
const mike = (await call('GET', '/trainers/lookup?activeOnly=false', { token: F }))[1].find(t => t.name === 'Mike Turner');
await call('POST', '/class-sessions', { body: body({ branchId: up.id, trainerId: mike.id }), token: F, expect: 409, name: 'inactive trainer -> 409' });
const elena = (await call('GET', '/trainers/lookup', { token: F }))[1].find(t => t.name === 'Elena Rodriguez');
await call('POST', '/class-sessions', { body: body({ trainerId: elena.id }), token: F, expect: 422, name: 'trainer from another branch -> 422' });

const [, small] = await call('POST', '/class-sessions', { body: body({ className: 'Tiny Class', startTime: '18:00:00', capacityLimit: 1 }), token: F, expect: 201, name: 'schedule a 1-place class' });
const [, b1] = await call('POST', `/class-sessions/${small.id}/bookings`, { body: { memberId: marcus.id, note: 'Knee' }, token: F, expect: 200, name: 'book -> confirmed' });
check(b1.status === 'Confirmed', 'first booking confirmed');
const [, b2] = await call('POST', `/class-sessions/${small.id}/bookings`, { body: { memberId: alex.id }, token: F, expect: 200, name: 'book -> waitlisted' });
check(b2.status === 'Waitlisted' && b2.waitlistPosition === 1, 'second booking waitlisted #1', b2);
await call('POST', `/class-sessions/${small.id}/bookings`, { body: { memberId: alex.id }, token: F, expect: 409, name: 'double booking -> 409' });
await call('POST', `/class-sessions/${small.id}/bookings`, { body: { memberId: john.id }, token: F, expect: 409, name: 'frozen member booking -> 409' });
await call('POST', `/class-sessions/${small.id}/bookings`, { body: { memberId: robert.id }, token: F, expect: 409, name: 'expired member booking -> 409' });
const [, sm] = await call('GET', `/class-sessions/${small.id}`, { token: F });
check(sm.state === 'Full' && sm.waitlist === 1, 'class Full with waitlist 1', sm);
const [, canc] = await call('DELETE', `/class-sessions/${small.id}/bookings/${b1.bookingId}`, { token: F, expect: 200, name: 'cancel booking -> waitlist promoted' });
check(canc.promotedMemberId === alex.id, 'Alex promoted from the waitlist');
const [, roster] = await call('GET', `/class-sessions/${small.id}/bookings`, { token: F, expect: 200, name: 'class roster' });
check(roster.some(r => r.memberName === 'Alex Rivera' && r.status === 'Confirmed'), 'roster shows the promotion');
await call('PUT', `/class-sessions/${small.id}`, { body: body({ className: 'Tiny Class', startTime: '18:00:00', capacityLimit: 1, branchId: up.id }), token: F, expect: 409, name: 'branch locked once booked -> 409' });
await call('PUT', `/class-sessions/${small.id}`, { body: body({ className: 'Tiny Class Renamed', startTime: '18:00:00', capacityLimit: 3, description: 'Edited' }), token: F, expect: 204, name: 'edit class' });
await call('PUT', `/class-sessions/${small.id}`, { body: body({ className: 'Tiny Class Renamed', startTime: '18:00:00', capacityLimit: 0 }), token: F, expect: 400, name: 'capacity 0 -> 400' });
await call('POST', `/class-sessions/${small.id}/bookings/${b2.bookingId}/attendance`, { body: { attended: true }, token: F, expect: 409, name: 'attendance before the start -> 409' });
await call('POST', `/class-sessions/${small.id}/cancel`, { token: F, expect: 204, name: 'cancel class' });
await call('POST', `/class-sessions/${small.id}/cancel`, { token: F, expect: 409, name: 'cancel class twice -> 409' });
const [, cs] = await call('GET', `/class-sessions/${small.id}`, { token: F });
check(cs.state === 'Cancelled' && cs.enrolled === 0, 'cancelled class has no enrolment');
const [, sched] = await call('GET', `/class-sessions?branchId=${down.id}&date=${iso(d10)}&days=1`, { token: F, expect: 200, name: 'day schedule' });
check(sched.some(c => c.className === 'Test Blast'), 'schedule lists the new class');
const [, wk] = await call('GET', `/class-sessions?date=${iso(today)}&days=7`, { token: F, expect: 200, name: 'week schedule, all branches' });
check(new Set(wk.map(c => c.branchName)).size === 2 && new Set(wk.map(c => c.date)).size >= 6, 'week view: both branches, every day');
const [, srch] = await call('GET', `/class-sessions?date=${iso(addDays(today, 1))}&days=1&search=zen`, { token: F, expect: 200, name: 'schedule search' });
check(srch.length > 0 && srch.every(c => c.studioName === 'Zen Room'), 'search by studio');
await call('GET', '/class-sessions?days=3', { token: F, expect: 400, name: 'days=3 -> 400' });
const [, cap] = await call('GET', `/class-sessions/capacity-overview?branchId=${down.id}&date=${iso(addDays(today, 1))}&days=1`, { token: F, expect: 200, name: 'capacity overview' });
check(cap.sessionCount >= 1 && cap.averageFillRate >= 0 && cap.averageFillRate <= 1, 'capacity overview values', cap);
await call('GET', '/class-sessions/00000000-0000-0000-0000-000000000001', { token: F, expect: 404, name: 'unknown class -> 404' });

// ---------- member self-service ----------
const [, el] = await call('GET', '/me/eligibility', { token: A, expect: 200, name: 'member eligibility' });
check(el.eligible && el.membershipNumber === 'TF-1001', 'Alex is eligible', el);
const [, mc] = await call('GET', `/me/classes?date=${iso(addDays(today, 1))}&days=1`, { token: A, expect: 200, name: "member's classes" });
const target = mc.find(c => c.state === 'Upcoming');
await call('POST', '/me/bookings', { body: { sessionId: target.id, note: 'Shoulder' }, token: A, expect: 200, name: 'member books a class' });
const [, el2] = await call('GET', `/me/eligibility?sessionId=${target.id}`, { token: A, expect: 200, name: 'eligibility for that class' });
check(['Confirmed', 'Waitlisted'].includes(el2.existingBooking), 'eligibility shows the existing booking', el2);
await call('POST', '/me/bookings', { body: { sessionId: target.id }, token: A, expect: 409, name: 'member double booking -> 409' });
await call('GET', '/me/eligibility', { token: F, expect: 403, name: 'staff cannot use /me -> 403' });

const failed = results.filter(r => !r).length;
console.log(`\nAPI TESTS: ${results.length - failed} passed, ${failed} failed`);
process.exit(failed ? 1 : 0);
