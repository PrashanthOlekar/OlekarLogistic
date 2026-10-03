import type { Stop } from '../../lib/types';

/** One row of GET /api/bookings. */
export interface BookingRow {
  id: number;
  bookingNumber: string;
  status: string;
  pickupDate: string;
  weightKg: number;
  from: string;
  to: string;
  vehicleType: string;
  total?: number | null;
}

/** GET /api/bookings/{id} */
export interface BookingDetail {
  id: number;
  bookingNumber: string;
  status: string;
  pickupDate: string;
  pickupSlot?: string;
  weightKg: number;
  goodsDescription: string;
  goods: string;
  vehicleType: string;
  specialInstructions?: string;
  cancelledReason?: string;
  pickup: Stop;
  drop: Stop;
  quote?: BookingQuote | null;
  payment?: BookingPayment | null;
  trip?: BookingTrip | null;
  invoice?: BookingInvoice | null;
}

export interface BookingQuote {
  distanceKm: number;
  vehicleCost: number;
  driverCost: number;
  taxAmount: number;
  totalAmount: number;
  validUntil: string;
  status: string;
  expired: boolean;
}

export interface BookingPayment {
  method: string;
  gateway: string;
  status: string;
  amount: number;
  paidAt?: string;
  gatewayPaymentId?: string;
}

export interface BookingTrip {
  tripNumber: string;
  status: string;
  vehicle: string;
  driverName: string;
  driverMobile: string;
  /** Only shown to the customer who booked. */
  pickupOtp?: string | null;
  deliveryOtp?: string | null;
  events: { eventType: string; note?: string; createdAt: string }[];
}

export interface BookingInvoice {
  invoiceNumber: string;
  taxableAmount: number;
  cgst: number;
  sgst: number;
  igst: number;
  totalAmount: number;
  issuedAt: string;
}

/** POST /api/quotes/estimate */
export interface PriceEstimate {
  distanceKm: number;
  days: number;
  vehicleCost: number;
  driverCost: number;
  freight: number;
  taxAmount: number;
  totalAmount: number;
  gstPercent: number;
  overCapacity: boolean;
  maxLoadKg: number;
  suggested?: { id: number; name: string } | null;
}
