import type { DocumentChip } from '../../lib/types';

/** GET /api/owner/summary */
export interface OwnerSummary {
  kycStatus: string;
  rejectionReason?: string;
  vehicles: number;
  vehiclesApproved: number;
  drivers: number;
  activeTrips: number;
  earnedThisMonth: number;
  pendingPayout: number;
  bank?: { accountHolder: string; accountLast4: string; ifsc: string };
}

/** One paid booking from GET /api/owner/loads, with my trucks that can carry it. */
export interface AvailableLoad {
  id: number;
  bookingNumber: string;
  pickupDate: string;
  pickupSlot?: string;
  weightKg: number;
  goods: string;
  from: string;
  fromAddress: string;
  to: string;
  vehicleType: string;
  distanceKm?: number;
  payout?: number;
  vehicles: { id: number; registrationNumber: string; currentDriverId?: number | null }[];
}

export interface LoadsResult {
  kycApproved: boolean;
  loads: AvailableLoad[];
}

/** GET /api/owner/drivers */
export interface OwnerDriver {
  id: number;
  name: string;
  mobile: string;
  licenceNumber: string;
  licenceClass: string;
  licenceExpiry: string;
  kycStatus: string;
  dutyStatus: string;
  rating?: number | null;
}

/** GET /api/owner/vehicles */
export interface OwnerVehicle {
  id: number;
  registrationNumber: string;
  vehicleType: string;
  vehicleTypeId: number;
  capacityKg: number;
  makeModel?: string;
  availabilityStatus: string;
  verificationStatus: string;
  currentDriverId?: number | null;
  driverName?: string | null;
  documents: DocumentChip[];
}

/** GET /api/owner/trips */
export interface OwnerTrip {
  id: number;
  tripNumber: string;
  status: string;
  bookingNumber: string;
  from: string;
  to: string;
  pickupDate: string;
  vehicle: string;
  driver: string;
  ownerPayout: number;
  deliveredAt?: string;
}

/** GET /api/owner/settlements */
export interface OwnerPayout {
  id: number;
  trip: string;
  grossAmount: number;
  commissionAmount: number;
  tdsAmount: number;
  netAmount: number;
  status: string;
  utr?: string;
  releasedAt?: string;
  createdAt: string;
}

/** Drivers who are verified and not on a trip. */
export function freeDrivers(drivers: OwnerDriver[] | null): OwnerDriver[] {
  return (drivers ?? []).filter(
    (driver) => driver.kycStatus === 'Approved' && driver.dutyStatus === 'Available',
  );
}
