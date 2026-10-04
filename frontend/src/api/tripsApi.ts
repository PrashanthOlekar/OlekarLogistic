/** /trips: assignment, the driver's steps, photos and POD approval. */
import type {
  AssignTripRequest,
  CreatedResource,
  CreatedTrip,
  HandoverKind,
  PagedResult,
  TripDetail,
  TripEventRequest,
  TripListItem,
  TripPhotoKind,
  TripQuery,
} from '../types';
import type { Position } from '../services/location';
import { apiClient, cleanParams } from './apiClient';

export const tripsApi = {
  async list(query: TripQuery = {}): Promise<PagedResult<TripListItem>> {
    const { data } = await apiClient.get<PagedResult<TripListItem>>('/trips', { params: cleanParams(query) });
    return data;
  },

  async get(id: number): Promise<TripDetail> {
    const { data } = await apiClient.get<TripDetail>(`/trips/${id}`);
    return data;
  },

  /** An owner taking a load, or an admin assigning a truck by hand. */
  async assign(request: AssignTripRequest): Promise<CreatedTrip> {
    const { data } = await apiClient.post<CreatedTrip>('/trips', request);
    return data;
  },

  async recordEvent(id: number, request: TripEventRequest): Promise<void> {
    await apiClient.post(`/trips/${id}/events`, request);
  },

  /** The 4-digit pickup or delivery code. */
  async confirmHandover(id: number, kind: HandoverKind, code: string): Promise<void> {
    await apiClient.post(`/trips/${id}/handovers`, { kind, code });
  },

  async uploadPhoto(id: number, kind: TripPhotoKind, file: File, position: Position): Promise<CreatedResource> {
    const form = new FormData();
    form.set('kind', kind);
    form.set('file', file);
    if (position.latitude != null && position.longitude != null) {
      form.set('latitude', String(position.latitude));
      form.set('longitude', String(position.longitude));
    }
    const { data } = await apiClient.post<CreatedResource>(`/trips/${id}/photos`, form);
    return data;
  },

  /** Completes the trip, issues the invoice and unlocks the owner's payout. */
  async approvePod(id: number): Promise<void> {
    await apiClient.put(`/trips/${id}/pod-approval`);
  },
};
