/** /auth: one-time codes, sign-in, refresh, sign-out and the signed-in profile. */
import type { AuthResponse, LoginRequest, SendOtpRequest, SendOtpResponse, UserProfile } from '../types';
import { apiClient } from './apiClient';

export const authApi = {
  async sendCode(request: SendOtpRequest): Promise<SendOtpResponse> {
    const { data } = await apiClient.post<SendOtpResponse>('/auth/otp', request, { skipAuth: true });
    return data;
  },

  async login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/auth/login', request, { skipAuth: true });
    return data;
  },

  /** Revokes the refresh token on the server. */
  async logout(refreshToken: string | null): Promise<void> {
    await apiClient.post('/auth/logout', { refreshToken });
  },

  async getProfile(): Promise<UserProfile> {
    const { data } = await apiClient.get<UserProfile>('/auth/me');
    return data;
  },
};
