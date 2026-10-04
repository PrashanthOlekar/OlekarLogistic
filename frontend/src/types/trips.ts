import type { Stop } from './common';
import type { ListQuery } from './paging';

export interface TripEvent {
  eventType: string;
  note?: string | null;
  createdAt: string;
}

/** GET /trips (owners and drivers see their own; admins see all). */
export interface TripQuery extends ListQuery {
  status?: string;
  sort?: 'Newest' | 'ActiveFirst';
}

export interface TripListItem {
  id: number;
  tripNumber: string;
  status: string;
  bookingNumber: string;
  from?: string | null;
  to?: string | null;
  pickupDate: string;
  vehicle: string;
  driver: string;
  driverMobile: string;
  owner: string;
  ownerPayout?: number | null;
  createdAt: string;
  deliveredAt?: string | null;
  /** Document id of the latest delivery receipt. */
  pod?: number | null;
  lastEvent?: string | null;
}

/** GET /trips/{id} */
export interface TripDetail {
  id: number;
  tripNumber: string;
  status: string;
  vehicle: string;
  hasPod: boolean;
  plannedDistanceKm?: number | null;
  driverPay?: number | null;
  booking: {
    bookingNumber: string;
    goodsDescription: string;
    weightKg: number;
    pickupDate: string;
    pickupSlot?: string | null;
    specialInstructions?: string | null;
    pickup: Stop;
    drop: Stop;
  };
  events: TripEvent[];
}

/** POST /trips: an owner taking a load, or an admin assigning one. */
export interface AssignTripRequest {
  bookingId: number;
  vehicleId: number;
  driverId: number;
}

export interface CreatedTrip {
  id: number;
  tripNumber: string;
}

export type TripAction = 'EnRoute' | 'ReachedPickup' | 'StartTrip' | 'ReachedDestination';
export type HandoverKind = 'Pickup' | 'Delivery';
export type TripPhotoKind = 'POD' | 'PickupPhoto';

export interface TripEventRequest {
  action: TripAction;
  latitude?: number;
  longitude?: number;
}
