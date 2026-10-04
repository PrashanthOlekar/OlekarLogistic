/**
 * The driver's checklist. Each step is one big button in the app.
 *
 *   Assigned → EnRouteToPickup → AtPickup ─(pickup code)→ Loaded → InTransit
 *            → AtDestination ─(POD photo + delivery code)→ Delivered
 */
import type { TripDetail } from '../../types';

export const STEP_LABELS = [
  'Go to pickup',
  'Reached pickup',
  'Pickup code',
  'Start trip',
  'Reached destination',
  'Upload POD',
  'Delivery code',
];

export enum TripStep {
  GoToPickup = 0,
  ReachPickup = 1,
  EnterPickupCode = 2,
  StartTrip = 3,
  ReachDestination = 4,
  UploadPod = 5,
  EnterDeliveryCode = 6,
  Finished = 7,
}

/** Trip statuses after which the driver has nothing left to do. */
export const FINISHED_STATUSES = ['Delivered', 'Completed', 'Cancelled'];

export function currentStep(trip: Pick<TripDetail, 'status' | 'hasPod'>): TripStep {
  switch (trip.status) {
    case 'Assigned':
      return TripStep.GoToPickup;
    case 'EnRouteToPickup':
      return TripStep.ReachPickup;
    case 'AtPickup':
      return TripStep.EnterPickupCode;
    case 'Loaded':
      return TripStep.StartTrip;
    case 'InTransit':
      return TripStep.ReachDestination;
    case 'AtDestination':
      return trip.hasPod ? TripStep.EnterDeliveryCode : TripStep.UploadPod;
    default:
      return TripStep.Finished;
  }
}
