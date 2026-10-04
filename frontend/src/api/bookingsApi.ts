/** /bookings: customers book, pay and cancel; admins list them and find trucks to assign. */
import type {
  AssignableVehicle,
  BookingDetail,
  BookingListItem,
  BookingQuery,
  CreateBookingRequest,
  CreatedBooking,
  CreatedResource,
  PagedResult,
  PaymentReceipt,
} from '../types';
import { apiClient, cleanParams } from './apiClient';

export const bookingsApi = {
  async list(query: BookingQuery = {}): Promise<PagedResult<BookingListItem>> {
    const { data } = await apiClient.get<PagedResult<BookingListItem>>('/bookings', { params: cleanParams(query) });
    return data;
  },

  async get(id: number): Promise<BookingDetail> {
    const { data } = await apiClient.get<BookingDetail>(`/bookings/${id}`);
    return data;
  },

  async create(request: CreateBookingRequest): Promise<CreatedBooking> {
    const { data } = await apiClient.post<CreatedBooking>('/bookings', request);
    return data;
  },

  /** A fresh quote after the previous one expired. */
  async requote(id: number): Promise<CreatedResource> {
    const { data } = await apiClient.post<CreatedResource>(`/bookings/${id}/quotes`);
    return data;
  },

  async pay(id: number, method: string): Promise<PaymentReceipt> {
    const { data } = await apiClient.post<PaymentReceipt>(`/bookings/${id}/payments`, { method });
    return data;
  },

  async cancel(id: number, reason: string | null): Promise<void> {
    await apiClient.post(`/bookings/${id}/cancellation`, { reason });
  },

  async getAssignableVehicles(id: number): Promise<AssignableVehicle[]> {
    const { data } = await apiClient.get<AssignableVehicle[]>(`/bookings/${id}/assignable-vehicles`);
    return data;
  },
};
