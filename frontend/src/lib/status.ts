/** Friendly words and colours for every status code the API sends. */

export type StatusColour = 'green' | 'amber' | 'red' | 'blue' | 'orange' | 'grey';

const STATUSES: Record<string, { label: string; colour: StatusColour }> = {
  // Bookings
  QuotePending: { label: 'Awaiting quote', colour: 'amber' },
  Quoted: { label: 'Awaiting payment', colour: 'amber' },
  Confirmed: { label: 'Finding a truck', colour: 'orange' },
  Assigned: { label: 'Truck assigned', colour: 'blue' },
  InTransit: { label: 'In transit', colour: 'blue' },
  Delivered: { label: 'Delivered', colour: 'green' },
  Completed: { label: 'Completed', colour: 'green' },
  Cancelled: { label: 'Cancelled', colour: 'grey' },

  // Trips
  EnRouteToPickup: { label: 'Going to pickup', colour: 'blue' },
  AtPickup: { label: 'At pickup', colour: 'orange' },
  Loaded: { label: 'Loaded', colour: 'blue' },
  AtDestination: { label: 'At destination', colour: 'orange' },

  // KYC and documents
  Pending: { label: 'Under review', colour: 'amber' },
  Approved: { label: 'Verified', colour: 'green' },
  Rejected: { label: 'Rejected', colour: 'red' },
  Verified: { label: 'Verified', colour: 'green' },
  Expired: { label: 'Expired', colour: 'red' },

  // Vehicles and drivers
  Available: { label: 'Available', colour: 'green' },
  Busy: { label: 'Busy', colour: 'orange' },
  Maintenance: { label: 'Maintenance', colour: 'red' },
  OnTrip: { label: 'On a trip', colour: 'blue' },
  OffDuty: { label: 'Off duty', colour: 'grey' },

  // Money
  AwaitingPod: { label: 'Waiting for POD', colour: 'amber' },
  Released: { label: 'Paid', colour: 'green' },
  Captured: { label: 'Paid', colour: 'green' },
  Refunded: { label: 'Refunded', colour: 'grey' },
  Failed: { label: 'Failed', colour: 'red' },

  // Accounts
  Active: { label: 'Active', colour: 'green' },
  Blocked: { label: 'Blocked', colour: 'red' },
  PendingKyc: { label: 'KYC pending', colour: 'amber' },

  // Quotes
  Sent: { label: 'Valid', colour: 'green' },
  Accepted: { label: 'Accepted', colour: 'green' },
  Superseded: { label: 'Replaced', colour: 'grey' },
};

export function describeStatus(status: string): { label: string; colour: StatusColour } {
  return STATUSES[status] ?? { label: status, colour: 'grey' };
}
