/** How a booking's status maps onto the progress bar and timeline the customer sees. */

/** Statuses in the order a booking moves through them. */
export const BOOKING_ORDER = [
  'QuotePending',
  'Quoted',
  'Confirmed',
  'Assigned',
  'InTransit',
  'Delivered',
  'Completed',
];

/** The six steps on the progress bar: [status reached, label]. */
export const PROGRESS_STEPS: [string, string][] = [
  ['Quoted', 'Paid'],
  ['Confirmed', 'Truck found'],
  ['Assigned', 'Pickup'],
  ['InTransit', 'In transit'],
  ['Delivered', 'Delivered'],
  ['Completed', 'Completed'],
];

/** Booking statuses in which the customer may still cancel. */
export const CANCELLABLE_BOOKING = ['QuotePending', 'Quoted', 'Confirmed', 'Assigned'];

/** Once the goods are loaded the trip can no longer be cancelled. */
export const CANCELLABLE_TRIP = ['Assigned', 'EnRouteToPickup', 'AtPickup'];

/** Timeline wording for each trip event. */
export const EVENT_TEXT: Record<string, string> = {
  Assigned: 'Truck and driver assigned',
  EnRouteToPickup: 'Driver is on the way to pickup',
  ReachedPickup: 'Driver reached pickup',
  PickupOtpVerified: 'Goods loaded (pickup code confirmed)',
  GoodsPhotoUploaded: 'Photo of loaded goods added',
  TripStarted: 'Trip started',
  ReachedDestination: 'Truck reached the delivery address',
  PodUploaded: 'Signed delivery receipt uploaded',
  DeliveryOtpVerified: 'Delivered (delivery code confirmed)',
  PodApproved: 'Delivery proof approved',
  Cancelled: 'Trip cancelled',
};

export function progressStepClass(bookingStatus: string, stepStatus: string): string {
  const current = BOOKING_ORDER.indexOf(bookingStatus);
  const step = BOOKING_ORDER.indexOf(stepStatus);
  if (current > step || bookingStatus === 'Completed') {
    return 'done';
  }
  return current === step ? 'now' : '';
}
