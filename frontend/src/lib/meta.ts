import { api } from './api';
import type { Meta } from './types';

let cached: Promise<Meta> | null = null;

/** Cities, vehicle types and goods rarely change, so they are fetched once per visit. */
export function getMeta(): Promise<Meta> {
  cached ??= api<Meta>('/meta').catch((error) => {
    cached = null;
    throw error;
  });
  return cached;
}
