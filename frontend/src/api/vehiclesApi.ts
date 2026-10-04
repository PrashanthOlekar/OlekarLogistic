/** /vehicles: an owner's fleet; admins verify them. */
import type {
  CreateVehicleRequest,
  CreatedResource,
  PagedResult,
  UpdateVehicleRequest,
  VehicleListItem,
  VehicleQuery,
  VerificationRequest,
} from '../types';
import { apiClient, cleanParams } from './apiClient';

export const vehiclesApi = {
  async list(query: VehicleQuery = {}): Promise<PagedResult<VehicleListItem>> {
    const { data } = await apiClient.get<PagedResult<VehicleListItem>>('/vehicles', { params: cleanParams(query) });
    return data;
  },

  async create(request: CreateVehicleRequest): Promise<CreatedResource> {
    const { data } = await apiClient.post<CreatedResource>('/vehicles', request);
    return data;
  },

  async update(id: number, request: UpdateVehicleRequest): Promise<void> {
    await apiClient.patch(`/vehicles/${id}`, request);
  },

  async setVerification(id: number, request: VerificationRequest): Promise<void> {
    await apiClient.put(`/vehicles/${id}/verification`, request);
  },
};
