import type { PagedResult } from '../types';
import { Button } from './Button';

interface PaginationProps {
  page: PagedResult<unknown> | null;
  onPageChange: (pageNumber: number) => void;
  /** Without a table above it (card grids), the top border is dropped. */
  standalone?: boolean;
}

/** "Showing 21–40 of 63" with Previous / Next, under a server-paged list. */
export function Pagination({ page, onPageChange, standalone }: PaginationProps) {
  if (!page || page.totalRecords === 0) {
    return null;
  }

  const { pageNumber, pageSize, totalRecords, totalPages } = page;
  const first = (pageNumber - 1) * pageSize + 1;
  const last = Math.min(pageNumber * pageSize, totalRecords);

  return (
    <nav className={standalone ? 'pager standalone' : 'pager'} aria-label="Pages">
      <span>
        Showing {first}–{last} of {totalRecords.toLocaleString('en-IN')}
      </span>
      {totalPages > 1 && (
        <div className="row tight">
          <Button
            variant="secondary"
            size="sm"
            disabled={pageNumber <= 1}
            onClick={() => onPageChange(pageNumber - 1)}
          >
            Previous
          </Button>
          <span className="pager-pages">
            Page {pageNumber} of {totalPages}
          </span>
          <Button
            variant="secondary"
            size="sm"
            disabled={pageNumber >= totalPages}
            onClick={() => onPageChange(pageNumber + 1)}
          >
            Next
          </Button>
        </div>
      )}
    </nav>
  );
}
