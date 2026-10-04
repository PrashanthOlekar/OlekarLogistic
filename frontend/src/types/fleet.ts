import type { DocumentChip } from './common';
import type { ListQuery } from './paging';

/** GET /vehicles (owners see their own; admins see all). */
export interface VehicleQuery extends ListQuery {
  vehicleTypeId?: number;
  availabilityStatus?: string;
  verificationStatus?: string;
  sort?: string;
}

export interface VehicleListItem {
  id: number;
  registrationNumber: string;
  vehicleType: string;
  vehicleTypeId: number;
  capacityKg: number;
  makeModel?: string | null;
  availabilityStatus: string;
  verificationStatus: string;
  currentDriverId?: number | null;
  driverName?: string | null;
  ownerName: string;
  createdAt: string;
  documents: DocumentChip[];
}

export interface CreateVehicleRequest {
  registrationNumber: string;
  vehicleTypeId: number;
  capacityKg: number;
  makeModel?: string | null;
  manufactureYear?: number | null;
  homeCityId?: number | null;
}

/** PATCH /vehicles/{id}: send only what changes. */
export interface UpdateVehicleRequest {
  availabilityStatus?: string;
  currentDriverId?: number | null;
  /** Clears the regular driver. */
  removeDriver?: boolean;
}

/** GET /drivers (owners see their own; admins see all). */
export interface DriverQuery extends ListQuery {
  kycStatus?: string;
  dutyStatus?: string;
  sort?: string;
}

export interface DriverListItem {
  id: number;
  name: string;
  mobile: string;
  licenceNumber: string;
  licenceClass: string;
  licenceExpiry: string;
  kycStatus: string;
  dutyStatus: string;
  rating?: number | null;
  ownerName?: string | null;
  createdAt: string;
}

/** GET /dashboards/owner */
export interface OwnerDashboard {
  ownerId: number;
  businessName?: string | null;
  kycStatus: string;
  rejectionReason?: string | null;
  vehicles: number;
  vehiclesApproved: number;
  drivers: number;
  activeTrips: number;
  earnedThisMonth: number;
  pendingPayout: number;
  bank?: { accountHolder: string; accountLast4: string; ifsc: string; bankName?: string | null } | null;
}

/** GET /loads: paid bookings that fit one of my verified, available trucks. */
export interface LoadsResponse {
  kycApproved: boolean;
  loads: AvailableLoad[];
}

export interface AvailableLoad {
  id: number;
  bookingNumber: string;
  pickupDate: string;
  pickupSlot?: string | null;
  weightKg: number;
  goods: string;
  from?: string | null;
  fromAddress: string;
  to?: string | null;
  vehicleType: string;
  distanceKm?: number | null;
  payout?: number | null;
  vehicles: { id: number; registrationNumber: string; currentDriverId?: number | null }[];
}

/** Drivers who are verified and not on a trip. */
export function freeDrivers(drivers: DriverListItem[] | null | undefined): DriverListItem[] {
  return (drivers ?? []).filter((driver) => driver.kycStatus === 'Approved' && driver.dutyStatus === 'Available');
}
