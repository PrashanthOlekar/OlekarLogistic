/** Data shapes shared by several pages. Page-specific shapes live next to their page. */

export type Role = 'Customer' | 'Owner' | 'Driver' | 'Admin';

export interface SessionUser {
  id: number;
  role: Role;
  fullName: string;
  mobile: string;
  email?: string | null;
  status: string;
  detail?: Record<string, unknown> | null;
}

/** Reply from /auth/login and /auth/register/*. */
export interface SignInResult {
  token: string;
  user: SessionUser;
}

/** Lists from GET /api/meta: cities, vehicle types and goods categories. */
export interface Meta {
  cities: City[];
  vehicleTypes: VehicleType[];
  goods: { id: number; name: string }[];
}

export interface City {
  id: number;
  name: string;
  nameKn?: string | null;
  state: string;
}

export interface VehicleType {
  id: number;
  code: string;
  name: string;
  bodyType: string;
  maxLoadKg: number;
  lengthFt: number;
  widthFt: number;
  heightFt?: number | null;
  recommendedGoods?: string | null;
}

/** A pickup or delivery point on a booking. */
export interface Stop {
  city?: string;
  address: string;
  contact?: string;
  phone?: string;
}

/** A document as listed under a vehicle or an applicant. */
export interface DocumentChip {
  id: number;
  docType: string;
  status: string;
  expiryDate?: string;
}
