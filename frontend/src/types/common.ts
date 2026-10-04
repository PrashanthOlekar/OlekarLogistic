/** Data shapes shared by several features. */

/** The four roles the API issues tokens for. */
export type Role = 'Customer' | 'Owner' | 'Driver' | 'Admin';

export const ROLES: Role[] = ['Customer', 'Owner', 'Driver', 'Admin'];

/** A pickup or delivery point on a booking. */
export interface Stop {
  city?: string | null;
  address: string;
  contact?: string | null;
  phone?: string | null;
}

/** A document as listed under a vehicle or an applicant. */
export interface DocumentChip {
  id: number;
  docType: string;
  status: string;
  expiryDate?: string | null;
}

/** Returned by POST endpoints that create something. */
export interface CreatedResource {
  id: number;
}

/** Body of every approve/reject endpoint. */
export interface VerificationRequest {
  approve: boolean;
  reason?: string | null;
}
