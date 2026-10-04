/** Operations-only endpoints: approvals, users, payments and the dashboards. */
import type {
  AccountStatus,
  AdminDashboard,
  OwnerDashboard,
  PagedResult,
  PaymentListItem,
  PaymentQuery,
  PendingApprovals,
  UserListItem,
  UserQuery,
} from '../types';
import { apiClient, cleanParams } from './apiClient';

export const approvalsApi = {
  async getPending(): Promise<PendingApprovals> {
    const { data } = await apiClient.get<PendingApprovals>('/approvals');
    return data;
  },
};

export const usersApi = {
  async list(query: UserQuery = {}): Promise<PagedResult<UserListItem>> {
    const { data } = await apiClient.get<PagedResult<UserListItem>>('/users', { params: cleanParams(query) });
    return data;
  },

  /** Blocking signs the user out everywhere straight away. */
  async setStatus(id: number, status: AccountStatus): Promise<void> {
    await apiClient.patch(`/users/${id}`, { status });
  },
};

export const paymentsApi = {
  async list(query: PaymentQuery = {}): Promise<PagedResult<PaymentListItem>> {
    const { data } = await apiClient.get<PagedResult<PaymentListItem>>('/payments', { params: cleanParams(query) });
    return data;
  },
};

export const dashboardsApi = {
  async getAdmin(): Promise<AdminDashboard> {
    const { data } = await apiClient.get<AdminDashboard>('/dashboards/admin');
    return data;
  },

  async getOwner(): Promise<OwnerDashboard> {
    const { data } = await apiClient.get<OwnerDashboard>('/dashboards/owner');
    return data;
  },
};
