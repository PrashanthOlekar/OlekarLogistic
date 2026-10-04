/** /registrations: new customer, owner and driver accounts. Each signs the new user in. */
import type {
  AuthResponse,
  RegisterCustomerRequest,
  RegisterDriverRequest,
  RegisterOwnerRequest,
} from '../types';
import { apiClient } from './apiClient';

export const registrationsApi = {
  async registerCustomer(request: RegisterCustomerRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/registrations/customers', request, { skipAuth: true });
    return data;
  },

  async registerOwner(request: RegisterOwnerRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/registrations/owners', request, { skipAuth: true });
    return data;
  },

  async registerDriver(request: RegisterDriverRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>('/registrations/drivers', request, { skipAuth: true });
    return data;
  },
};
