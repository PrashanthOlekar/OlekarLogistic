/** /drivers and /owners: people who run the trucks; admins verify them. */
import type { DriverListItem, DriverQuery, PagedResult, VerificationRequest } from '../types';
import { apiClient, cleanParams } from './apiClient';

export const driversApi = {
  async list(query: DriverQuery = {}): Promise<PagedResult<DriverListItem>> {
    const { data } = await apiClient.get<PagedResult<DriverListItem>>('/drivers', { params: cleanParams(query) });
    return data;
  },

  async setVerification(id: number, request: VerificationRequest): Promise<void> {
    await apiClient.put(`/drivers/${id}/verification`, request);
  },
};

export const ownersApi = {
  async setVerification(id: number, request: VerificationRequest): Promise<void> {
    await apiClient.put(`/owners/${id}/verification`, request);
  },
};
