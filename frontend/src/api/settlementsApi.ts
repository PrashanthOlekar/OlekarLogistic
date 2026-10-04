/** /settlements: what each owner is paid per trip. Admins record the bank transfer. */
import type { PagedResult, SettlementListItem, SettlementQuery } from '../types';
import { apiClient, cleanParams } from './apiClient';

export const settlementsApi = {
  async list(query: SettlementQuery = {}): Promise<PagedResult<SettlementListItem>> {
    const { data } = await apiClient.get<PagedResult<SettlementListItem>>('/settlements', {
      params: cleanParams(query),
    });
    return data;
  },

  /** Records a payout already sent from the bank, with its UTR (transfer reference). */
  async recordPayout(id: number, utr: string): Promise<void> {
    await apiClient.put(`/settlements/${id}/payout`, { utr });
  },
};
