import type { DocumentChip } from '../../lib/types';

/** GET /api/admin/summary */
export interface AdminSummary {
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

/** GET /api/admin/approvals */
export interface PendingApprovals {
  owners: {
    id: number;
    name: string;
    mobile: string;
    businessName?: string;
    panLast4?: string;
    aadhaarLast4?: string;
    bank?: string;
    createdAt: string;
    documents: DocumentChip[];
  }[];
  drivers: {
    id: number;
    name: string;
    mobile: string;
    licenceNumber: string;
    licenceClass: string;
    licenceExpiry: string;
    owner?: string;
    createdAt: string;
    documents: DocumentChip[];
  }[];
  vehicles: {
    id: number;
    registrationNumber: string;
    vehicleType: string;
    capacityKg: number;
    owner: string;
    createdAt: string;
    documents: DocumentChip[];
  }[];
}

/** GET /api/admin/documents */
export interface AdminDocument {
  id: number;
  entityType: string;
  entityId: number;
  docType: string;
  fileName: string;
  documentNumber?: string;
  expiryDate?: string;
  status: string;
  rejectionReason?: string;
  uploadedAt: string;
}

/** GET /api/admin/bookings */
export interface AdminBooking {
  id: number;
  bookingNumber: string;
  status: string;
  customer: string;
  customerMobile: string;
  from: string;
  to: string;
  pickupDate: string;
  weightKg: number;
  vehicleType: string;
  total?: number;
  createdAt: string;
}

/** GET /api/admin/bookings/{id}/assignable */
export interface AssignableTruck {
  id: number;
  registrationNumber: string;
  owner: string;
  currentDriverId?: number;
  drivers: { id: number; name: string }[];
}

/** GET /api/admin/trips */
export interface AdminTrip {
  id: number;
  tripNumber: string;
  status: string;
  bookingNumber: string;
  from: string;
  to: string;
  vehicle: string;
  driver: string;
  driverMobile: string;
  owner: string;
  createdAt: string;
  deliveredAt?: string;
  /** Document id of the latest delivery receipt. */
  pod?: number | null;
  lastEvent?: string;
}

/** GET /api/admin/payments */
export interface AdminPayment {
  id: number;
  bookingNumber: string;
  customer: string;
  amount: number;
  method: string;
  gateway: string;
  status: string;
  paidAt?: string;
  gatewayPaymentId?: string;
}

/** GET /api/admin/settlements */
export interface AdminSettlement {
  id: number;
  trip: string;
  owner: string;
  bank?: string;
  grossAmount: number;
  commissionAmount: number;
  tdsAmount: number;
  netAmount: number;
  status: string;
  utr?: string;
  releasedAt?: string;
}

/** GET /api/admin/users */
export interface AdminUser {
  id: number;
  role: string;
  fullName: string;
  mobile: string;
  email?: string;
  status: string;
  createdAt: string;
  lastLoginAt?: string;
}
