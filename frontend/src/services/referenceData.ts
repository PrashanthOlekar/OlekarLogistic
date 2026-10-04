/** Cities, vehicle types and goods rarely change, so they are fetched once per visit. */
import { referenceDataApi } from '../api/referenceDataApi';
import type { ReferenceData } from '../types';

let cached: Promise<ReferenceData> | null = null;

export function getReferenceData(): Promise<ReferenceData> {
  cached ??= referenceDataApi.get().catch((error) => {
    cached = null;
    throw error;
  });
  return cached;
}
