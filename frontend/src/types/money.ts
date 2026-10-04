import type { ListQuery } from './paging';

/** GET /payments (admin) */
export interface PaymentQuery extends ListQuery {
  status?: string;
}

export interface PaymentListItem {
  id: number;
  bookingNumber: string;
  customer: string;
  amount: number;
  method: string;
  gateway: string;
  status: string;
  paidAt?: string | null;
  gatewayPaymentId?: string | null;
  createdAt: string;
}

/** GET /settlements (owners see their own; admins see all). */
export interface SettlementQuery extends ListQuery {
  status?: string;
}

export interface SettlementListItem {
  id: number;
  trip: string;
  owner: string;
  bank?: string | null;
  grossAmount: number;
  commissionAmount: number;
  tdsAmount: number;
  netAmount: number;
  status: string;
  utr?: string | null;
  releasedAt?: string | null;
  createdAt: string;
}
