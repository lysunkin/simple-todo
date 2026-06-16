import type { TodoFilters, Priority } from '../types/todo';

interface Props {
  filters: TodoFilters;
  onChange: (filters: Partial<TodoFilters>) => void;
}

const PRIORITIES: Priority[] = ['Low', 'Medium', 'High'];

/**
 * Filter bar. Each control immediately calls onChange so the parent hook
 * re-fetches. Debouncing the search input would be a useful future improvement
 * to avoid one request per keystroke.
 */
export function TodoFilterBar({ filters, onChange }: Props) {
  return (
    <div className="filter-bar" role="search" aria-label="Filter tasks">
      <input
        type="search"
        placeholder="Search tasks…"
        value={filters.search ?? ''}
        onChange={(e) => onChange({ search: e.target.value || undefined })}
        aria-label="Search tasks"
        className="filter-search"
      />

      <select
        value={filters.isCompleted === undefined ? '' : String(filters.isCompleted)}
        onChange={(e) =>
          onChange({
            isCompleted: e.target.value === '' ? undefined : e.target.value === 'true',
          })
        }
        aria-label="Filter by status"
      >
        <option value="">All statuses</option>
        <option value="false">Active</option>
        <option value="true">Completed</option>
      </select>

      <select
        value={filters.priority ?? ''}
        onChange={(e) =>
          onChange({ priority: e.target.value ? (e.target.value as Priority) : undefined })
        }
        aria-label="Filter by priority"
      >
        <option value="">All priorities</option>
        {PRIORITIES.map((p) => (
          <option key={p} value={p}>
            {p}
          </option>
        ))}
      </select>
    </div>
  );
}
