import type { Stop } from './common';
import type { ListQuery } from './paging';
import type { TripEvent } from './trips';

/** GET /bookings (customers see their own; admins see all). */
export interface BookingQuery extends ListQuery {
  status?: string;
  sort?: 'Newest' | 'PickupDate';
}

export interface BookingListItem {
  id: number;
  bookingNumber: string;
  status: string;
  customer: string;
  customerMobile: string;
  from?: string | null;
  to?: string | null;
  pickupDate: string;
  weightKg: number;
  vehicleType: string;
  vehicleTypeId: number;
  createdAt: string;
  total?: number | null;
}

/** GET /bookings/{id} */
export interface BookingDetail {
  id: number;
  bookingNumber: string;
  status: string;
  pickupDate: string;
  pickupSlot?: string | null;
  weightKg: number;
  goodsValue?: number | null;
  goodsDescription: string;
  specialInstructions?: string | null;
  createdAt: string;
  cancelledReason?: string | null;
  goods: string;
  vehicleType: string;
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
  loadingCharges: number;
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
  paidAt?: string | null;
  gatewayPaymentId?: string | null;
}

export interface BookingTrip {
  tripNumber: string;
  status: string;
  vehicle: string;
  driverName: string;
  driverMobile: string;
  /** Only sent to the customer who booked. */
  pickupOtp?: string | null;
  deliveryOtp?: string | null;
  events: TripEvent[];
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

/** POST /bookings */
export interface CreateBookingRequest {
  pickupCityId: number;
  pickupAddress: string;
  pickupContactName?: string | null;
  pickupContactPhone?: string | null;
  dropCityId: number;
  dropAddress: string;
  dropContactName?: string | null;
  dropContactPhone?: string | null;
  goodsCategoryId: number;
  goodsDescription: string;
  weightKg: number;
  goodsValue?: number | null;
  vehicleTypeId: number;
  pickupDate: string;
  pickupSlot?: string | null;
  specialInstructions?: string | null;
}

export interface CreatedBooking {
  id: number;
  bookingNumber: string;
}

export interface PaymentReceipt {
  paymentId: number;
  amount: number;
}

/** GET /bookings/{id}/assignable-vehicles (admin) */
export interface AssignableVehicle {
  id: number;
  registrationNumber: string;
  owner: string;
  currentDriverId?: number | null;
  drivers: { id: number; name: string }[];
}
