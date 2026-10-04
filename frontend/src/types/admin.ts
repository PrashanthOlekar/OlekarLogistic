import type { DocumentChip, Role } from './common';
import type { ListQuery } from './paging';

/** GET /dashboards/admin */
export interface AdminDashboard {
  bookingsToday: number;
  activeTrips: number;
  completedThisMonth: number;
  awaitingPayment: number;
  awaitingTruck: number;
  revenueThisMonth: number;
  commissionThisMonth: number;
  pendingApprovals: number;
  pendingDocuments: number;
  podToApprove: number;
  settlementsToRelease: number;
  last7: { date: string; count: number }[];
  routes: { route: string; count: number }[];
  cities: { city: string; count: number }[];
  vehicles: { status: string; count: number }[];
}

/** GET /approvals */
export interface PendingApprovals {
  owners: PendingOwner[];
  drivers: PendingDriver[];
  vehicles: PendingVehicle[];
}

export interface PendingOwner {
  id: number;
  name: string;
  mobile: string;
  businessName?: string | null;
  panLast4?: string | null;
  aadhaarLast4?: string | null;
  bank?: string | null;
  createdAt: string;
  documents: DocumentChip[];
}

export interface PendingDriver {
  id: number;
  name: string;
  mobile: string;
  licenceNumber: string;
  licenceClass: string;
  licenceExpiry: string;
  owner?: string | null;
  createdAt: string;
  documents: DocumentChip[];
}

export interface PendingVehicle {
  id: number;
  registrationNumber: string;
  vehicleType: string;
  capacityKg: number;
  owner: string;
  createdAt: string;
  documents: DocumentChip[];
}

/** GET /users */
export interface UserQuery extends ListQuery {
  role?: Role | '';
  status?: string;
  sort?: string;
}

export interface UserListItem {
  id: number;
  role: Role;
  fullName: string;
  mobile: string;
  email?: string | null;
  status: string;
  createdAt: string;
  lastLoginAt?: string | null;
}

export type AccountStatus = 'Active' | 'Blocked';
