import { useState } from 'react';
import { DEFAULT_PAGE_SIZE, type PagedResult } from '../types';
import { useLoad, type LoadState } from './useLoad';

export interface PageRequest {
  pageNumber: number;
  pageSize: number;
}

export interface PagedLoadState<T> extends LoadState<PagedResult<T>> {
  /** The rows of the current page, or null before the first load. */
  items: T[] | null;
  pageNumber: number;
  setPageNumber: (pageNumber: number) => void;
}

/**
 * A server-paged list. Changing any of `filters` goes back to page 1.
 *
 *   const bookings = usePagedLoad((page) => bookingsApi.list({ ...page, status }), [status]);
 *   bookings.items, <Pagination page={bookings.data} onPageChange={bookings.setPageNumber} />
 */
export function usePagedLoad<T>(
  load: (page: PageRequest) => Promise<PagedResult<T>>,
  filters: unknown[] = [],
  pageSize = DEFAULT_PAGE_SIZE,
): PagedLoadState<T> {
  const [pageNumber, setPageNumber] = useState(1);

  // Back to the first page when a filter changes (adjusting state during render, not in an effect,
  // so the old page number is never requested with the new filters).
  const filtersKey = JSON.stringify(filters);
  const [loadedFiltersKey, setLoadedFiltersKey] = useState(filtersKey);
  if (filtersKey !== loadedFiltersKey) {
    setLoadedFiltersKey(filtersKey);
    setPageNumber(1);
  }

  const state = useLoad(() => load({ pageNumber, pageSize }), [filtersKey, pageNumber, pageSize]);

  return {
    ...state,
    items: state.data?.items ?? null,
    pageNumber,
    setPageNumber: (next: number) => {
      setPageNumber(next);
      window.scrollTo(0, 0);
    },
  };
}
