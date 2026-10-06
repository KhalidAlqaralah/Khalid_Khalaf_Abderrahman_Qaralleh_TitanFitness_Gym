// Typed shapes of the TitanFitness.Api JSON. Dates arrive as ISO strings:
// DateOnly "2026-10-06", TimeOnly "14:30:00", DateTime "2026-10-06T14:30:00" (gym local time).

export type SortDirection = 'Asc' | 'Desc';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** ProblemDetails / ValidationProblemDetails returned by the API for every error. */
export interface ApiProblem {
  status?: number;
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

// ---------- auth ----------
export type Role = 'FrontDesk' | 'Manager' | 'Member';

export interface Session {
  token: string;
  expiresAtUtc: string;
  userName: string;
  displayName: string;
  role: Role;
  memberId: string | null;
}

// ---------- branches ----------
export interface Studio {
  id: string;
  name: string;
  capacity: number;
}

export interface Branch {
  id: string;
  name: string;
  address: string | null;
  opens: string;
  closes: string;
  studios: Studio[];
}

// ---------- members ----------
export type MemberStatus = 'Active' | 'Frozen' | 'Expired' | 'Pending' | 'Cancelled' | 'None';

export interface MemberListItem {
  id: string;
  membershipNumber: string;
  fullName: string;
  photoUrl: string | null;
  status: MemberStatus;
  branchId: string;
  branchName: string;
  lastVisit: string | null;
  remainingFreezes: number;
}

export interface Member {
  id: string;
  membershipNumber: string;
  fullName: string;
  email: string | null;
  phone: string | null;
  address: string | null;
  joinedOn: string;
  photoUrl: string | null;
  homeBranchId: string;
  homeBranchName: string;
  status: MemberStatus;
  lastVisit: string | null;
  createdBy: string;
  createdAt: string;
}

export interface MemberQuery {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: SortDirection;
  branchId?: string | null;
  statuses?: MemberStatus[];
}

export type ActivityKind = 'CheckIn' | 'ClassAttendance';

export interface MemberActivity {
  kind: ActivityKind;
  title: string;
  detail: string;
  occurredAt: string;
}

// ---------- memberships ----------
export type MembershipStatus = 'Pending' | 'Active' | 'Frozen' | 'Expired' | 'Cancelled';
export type AccessScope = 'HomeBranchOnly' | 'AllBranches';
export type FreezeReason = 'ExtendedTravel' | 'Medical' | 'Injury' | 'Financial' | 'Other';

export interface Freeze {
  id: string;
  startDate: string;
  endDate: string;
  durationInMonths: number;
  reason: FreezeReason;
  notes: string | null;
  endedEarlyOn: string | null;
  daysUsed: number;
}

export interface GuestPass {
  id: string;
  issuedOn: string;
  usedOn: string | null;
  guestName: string | null;
}

export interface Membership {
  id: string;
  memberId: string;
  planId: string;
  planName: string;
  price: number;
  durationInMonths: number;
  accessScope: AccessScope;
  startDate: string;
  endDate: string;
  status: MembershipStatus;
  cancelledOn: string | null;
  maxFreezes: number;
  freezesUsed: number;
  remainingFreezes: number;
  maxFreezeDays: number;
  freezeDaysUsed: number;
  remainingFreezeDays: number;
  guestPassQuota: number;
  guestPassesIssued: number;
  remainingGuestPasses: number;
  canFreeze: boolean;
  cannotFreezeReason: string | null;
  freezes: Freeze[];
  guestPasses: GuestPass[];
}

export interface FreezeRequest {
  startDate: string | null;
  durationInMonths: number | null;
  reason: FreezeReason | null;
  notes: string | null;
}

export interface FreezeApplied {
  freezeId: string;
  startDate: string;
  endDate: string;
  daysUsed: number;
  newEndDate: string;
}

// ---------- check-ins ----------
export interface CheckInRequest {
  memberId: string | null;
  branchId: string | null;
  date: string | null;
  time: string | null;
  notes: string | null;
}

export interface CheckIn {
  id: string;
  memberId: string;
  memberName: string;
  branchId: string;
  branchName: string;
  occurredAt: string;
}

// ---------- dashboard ----------
export interface CheckInsToday {
  today: number;
  sameDayLastWeek: number;
  changePercent: number | null;
}

export interface ActiveMembers {
  activeMembers: number;
  onFloor: number;
  onFloorWindowMinutes: number;
}

// ---------- classes ----------
export type ClassState = 'Upcoming' | 'InProgress' | 'Full' | 'Completed' | 'Cancelled';
export type BookingStatus = 'Waitlisted' | 'Confirmed' | 'Cancelled' | 'Attended' | 'NoShow';

export interface ClassSession {
  id: string;
  className: string;
  description: string | null;
  branchId: string;
  branchName: string;
  trainerId: string | null;
  trainerName: string | null;
  studioId: string | null;
  studioName: string | null;
  studioCapacity: number | null;
  date: string;
  startTime: string;
  endTime: string;
  durationInMinutes: number;
  capacityLimit: number;
  enrolled: number;
  waitlist: number;
  activeBookings: number;
  remainingPlaces: number;
  isCancelled: boolean;
  state: ClassState;
}

export interface ClassScheduleQuery {
  branchId?: string | null;
  date?: string | null;
  days?: number;
  search?: string | null;
}

export interface CapacityOverview {
  sessionCount: number;
  totalBookings: number;
  averageFillRate: number;
}

export interface ClassSessionRequest {
  className: string;
  branchId: string | null;
  trainerId: string | null;
  studioId: string | null;
  date: string | null;
  startTime: string | null;
  durationInMinutes: number | null;
  capacityLimit: number | null;
  description: string | null;
}

export interface BookingResult {
  bookingId: string;
  status: BookingStatus;
  waitlistPosition: number | null;
}

export interface SessionBooking {
  bookingId: string;
  memberId: string;
  memberName: string;
  membershipNumber: string;
  status: BookingStatus;
  position: number;
  note: string | null;
  bookedOn: string;
}

// ---------- trainers ----------
export interface TrainerListItem {
  id: string;
  code: string;
  name: string;
  specialty: string | null;
  branchId: string;
  branchName: string;
  isActive: boolean;
}

export interface Trainer extends TrainerListItem {
  email: string;
  phone: string | null;
  createdBy: string;
  createdAt: string;
}

export interface TrainerRequest {
  name: string;
  specialty: string | null;
  branchId: string | null;
  email: string;
  phone: string | null;
  isActive: boolean;
}

export interface TrainerLookup {
  id: string;
  name: string;
  branchId: string;
  isActive: boolean;
}

export interface TrainerFilters {
  branchIds: string[];
  specialties: string[];
  statuses: ('Active' | 'Inactive')[];
}

export interface TrainerQuery extends TrainerFilters {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: SortDirection;
}

// ---------- plans ----------
export interface Plan {
  id: string;
  name: string;
  price: number;
  durationInMonths: number;
  maxFreezeDays: number;
  maxFreezes: number;
  guestPassQuota: number;
  accessScope: AccessScope;
  isPublished: boolean;
}

export interface PlanRequest {
  name: string;
  price: number | null;
  durationInMonths: number | null;
  isPublished: boolean;
  maxFreezeDays: number | null;
  maxFreezes: number | null;
  guestPassQuota: number | null;
  accessScope: AccessScope | null;
}

export interface PlanFilters {
  durations: number[];
  access: AccessScope | null;
  minPrice: number | null;
  maxPrice: number | null;
  statuses: ('Published' | 'Retired')[];
}

export interface PlanQuery extends PlanFilters {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: SortDirection;
}

export interface PlanFilterOptions {
  durations: number[];
  minPrice: number;
  maxPrice: number;
}

// ---------- self-service ----------
export interface Eligibility {
  memberId: string;
  memberName: string;
  membershipNumber: string;
  status: MemberStatus;
  eligible: boolean;
  title: string;
  detail: string;
  existingBooking: BookingStatus | null;
}

/** Created responses. */
export interface CreatedId {
  id: string;
}

export interface MemberCreated {
  id: string;
  membershipNumber: string;
}

export interface TrainerCreated {
  id: string;
  code: string;
}
