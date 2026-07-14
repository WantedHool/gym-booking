export interface AuthUser {
  id: string;
  tenantId: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
}

export interface RegisterRequest {
  password: string;
  firstName: string;
  lastName: string;
}

export interface ClassType {
  id: string;
  name: string;
  description: string;
  defaultDurationMinutes: number;
  defaultCapacity: number;
  isActive: boolean;
}

export interface CreateClassTypeRequest {
  name: string;
  description: string;
  defaultDurationMinutes: number;
  defaultCapacity: number;
}

export type UpdateClassTypeRequest = CreateClassTypeRequest;

export interface ClassSession {
  id: string;
  classTypeId: string;
  classTypeName: string;
  instructorId: string;
  instructorName: string;
  startsAt: string; // ISO UTC
  durationMinutes: number;
  capacity: number;
  bookedCount: number;
  isCancelled: boolean;
}

export interface CreateClassSessionRequest {
  classTypeId: string;
  instructorId?: string | null;
  startsAt: string; // ISO UTC
  durationMinutes: number;
  capacity: number;
}

export interface Booking {
  id: string;
  classSessionId: string;
  classTypeName: string;
  startsAt: string; // ISO UTC
  status: string;   // 'Confirmed' | 'Cancelled'
  createdAt: string;
}

export interface CreateBookingRequest {
  classSessionId: string;
}

export interface CreateInvitationRequest {
  email: string;
  role: 'User' | 'Instructor';
}

export interface ScheduleSession {
  id: string;
  classTypeId: string;
  classTypeName: string;
  instructorId: string;
  instructorName: string;
  startsAt: string; // ISO UTC
  durationMinutes: number;
  capacity: number;
  bookedCount: number;
  isBookedByMe: boolean;
  myBookingId: string | null;
}

export interface WaitlistEntry {
  classSessionId: string;
  classTypeName: string;
  startsAt: string; // ISO UTC
  position: number;
}

export interface Instructor {
  id: string;
  name: string;
}

export interface ScheduleQuery {
  from: string; // ISO UTC
  to: string;   // ISO UTC
  classTypeId?: string;
  instructorId?: string;
}

export type PlanType = 'SessionPack' | 'Unlimited';

export interface MembershipPlan {
  id: string;
  name: string;
  type: PlanType;
  sessionsCount: number;
  durationDays: number;
  price: number;
  isActive: boolean;
}

export interface CreatePlanRequest {
  name: string;
  type: PlanType;
  sessionsCount: number;
  durationDays: number;
  price: number;
}

export interface AssignSubscriptionRequest {
  email: string;
  planId: string;
}

export interface Subscription {
  id: string;
  planName: string;
  type: PlanType;
  remainingSessions: number | null;
  sessionsTotal: number | null;
  validFrom: string;
  validTo: string;
}
