interface Props {
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
}

/**
 * Page navigation bar. Renders Prev / page buttons / Next.
 * Shows at most 5 page buttons centred around the current page.
 * Hidden entirely when there is only one page.
 */
export function Pagination({ currentPage, totalPages, totalCount, pageSize, onPageChange }: Props) {
  if (totalPages <= 1) return null;

  const firstItem = (currentPage - 1) * pageSize + 1;
  const lastItem = Math.min(currentPage * pageSize, totalCount);

  // Build the window of page numbers to show (max 5, centred on currentPage)
  const window = 2; // pages on each side of current
  const start = Math.max(1, currentPage - window);
  const end = Math.min(totalPages, currentPage + window);
  const pages = Array.from({ length: end - start + 1 }, (_, i) => start + i);

  return (
    <nav className="pagination" aria-label="Pagination">
      <span className="pagination-info">
        {firstItem}–{lastItem} of {totalCount}
      </span>

      <div className="pagination-controls">
        <button
          className="btn btn-ghost btn-sm"
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          aria-label="Previous page"
        >
          ‹ Prev
        </button>

        {start > 1 && (
          <>
            <button className="btn btn-ghost btn-sm" onClick={() => onPageChange(1)}>1</button>
            {start > 2 && <span className="pagination-ellipsis">…</span>}
          </>
        )}

        {pages.map((p) => (
          <button
            key={p}
            className={`btn btn-sm ${p === currentPage ? 'btn-primary' : 'btn-ghost'}`}
            onClick={() => onPageChange(p)}
            aria-current={p === currentPage ? 'page' : undefined}
            aria-label={`Page ${p}`}
          >
            {p}
          </button>
        ))}

        {end < totalPages && (
          <>
            {end < totalPages - 1 && <span className="pagination-ellipsis">…</span>}
            <button className="btn btn-ghost btn-sm" onClick={() => onPageChange(totalPages)}>
              {totalPages}
            </button>
          </>
        )}

        <button
          className="btn btn-ghost btn-sm"
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          aria-label="Next page"
        >
          Next ›
        </button>
      </div>
    </nav>
  );
}
