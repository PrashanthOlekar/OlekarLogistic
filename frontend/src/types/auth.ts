import type { Role } from './common';

/** GET /auth/me, and the `user` of every sign-in response. */
export interface UserProfile {
  id: number;
  role: Role;
  fullName: string;
  mobile: string;
  email?: string | null;
  status: string;
  /** CustomerProfile, OwnerProfile or DriverProfile, depending on the role. */
  detail?: ProfileDetail | null;
}

export interface ProfileDetail {
  kycStatus?: string;
  rejectionReason?: string | null;
  [key: string]: unknown;
}

/** POST /auth/login, /auth/refresh and /registrations/*. */
export interface AuthResponse {
  accessToken: string;
  /** Seconds until the access token expires. */
  expiresIn: number;
  expiresAt: string;
  refreshToken: string;
  user: UserProfile;
}

export type OtpPurpose = 'Login' | 'Signup';

export interface SendOtpRequest {
  mobile: string;
  purpose: OtpPurpose;
}

export interface SendOtpResponse {
  sent: boolean;
  /** Only returned by a development server, so you can test without SMS. */
  devCode?: string | null;
}

export interface LoginRequest {
  mobile: string;
  code: string;
}

interface RegistrationBase {
  fullName: string;
  mobile: string;
  code: string;
  email?: string | null;
}

export interface RegisterCustomerRequest extends RegistrationBase {
  companyName?: string | null;
  gstin?: string | null;
}

export interface RegisterOwnerRequest extends RegistrationBase {
  businessName?: string | null;
  pan: string;
  aadhaarLast4: string;
  accountHolder: string;
  accountNumber: string;
  ifsc: string;
  bankName?: string | null;
}

export interface RegisterDriverRequest extends RegistrationBase {
  licenceNumber: string;
  licenceClass: string;
  licenceExpiry: string;
  aadhaarLast4?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  ownerMobile?: string | null;
}
