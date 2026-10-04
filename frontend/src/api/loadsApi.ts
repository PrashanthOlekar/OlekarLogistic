/** /loads: paid bookings an owner's trucks can carry. Taking one is POST /trips. */
import type { LoadsResponse } from '../types';
import { apiClient } from './apiClient';

export const loadsApi = {
  async listAvailable(): Promise<LoadsResponse> {
    const { data } = await apiClient.get<LoadsResponse>('/loads');
    return data;
  },

  /** "Not interested": hides the load from this owner. */
  async decline(bookingId: number): Promise<void> {
    await apiClient.delete(`/loads/${bookingId}`);
  },
};
