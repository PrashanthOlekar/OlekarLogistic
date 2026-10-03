import type { Stop } from '../../lib/types';

/** One row of GET /api/driver/trips */
export interface DriverTripRow {
  id: number;
  tripNumber: string;
  status: string;
  from: string;
  to: string;
  pickupDate: string;
  vehicle: string;
}

/** GET /api/driver/trips/{id} */
export interface DriverTrip {
  id: number;
  tripNumber: string;
  status: string;
  vehicle: string;
  hasPod: boolean;
  plannedDistanceKm?: number;
  booking: {
    bookingNumber: string;
    goodsDescription: string;
    weightKg: number;
    pickupDate: string;
    pickupSlot?: string;
    specialInstructions?: string;
    pickup: Stop;
    drop: Stop;
  };
  events: { eventType: string; createdAt: string }[];
}
