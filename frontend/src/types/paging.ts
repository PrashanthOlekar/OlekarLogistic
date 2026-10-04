/** Server-side paging, filtering and sorting, as every list endpoint returns it. */

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

/** Query string accepted by every list endpoint. Feature queries add their own filters. */
export interface ListQuery {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}

export const DEFAULT_PAGE_SIZE = 20;

/** The largest page the API returns; used for short lists shown in a dropdown. */
export const MAX_PAGE_SIZE = 100;
