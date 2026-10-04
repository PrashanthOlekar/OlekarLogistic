/** /reference-data and /price-estimates: public, no sign-in needed. */
import type { PriceEstimate, PriceEstimateRequest, ReferenceData } from '../types';
import { apiClient } from './apiClient';

export const referenceDataApi = {
  async get(): Promise<ReferenceData> {
    const { data } = await apiClient.get<ReferenceData>('/reference-data', { skipAuth: true });
    return data;
  },

  async estimatePrice(request: PriceEstimateRequest, signal?: AbortSignal): Promise<PriceEstimate> {
    const { data } = await apiClient.post<PriceEstimate>('/price-estimates', request, { skipAuth: true, signal });
    return data;
  },
};
