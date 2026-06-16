import { useState, useEffect, useCallback } from 'react';
import { todoApi } from '../api/todoApi';
import type { TodoItem, TodoFilters, CreateTodoRequest, UpdateTodoRequest } from '../types/todo';

const DEFAULT_FILTERS: TodoFilters = {
  page: 1,
  pageSize: 20,
  sortBy: 'dueDate',
  sortDir: 'asc',
};

/**
 * Custom hook that owns all todo state and exposes a clean API to components.
 * Keeping data-fetching logic here rather than in components makes the components
 * simpler and keeps the hook independently testable.
 */
export function useTodos(initialFilters: TodoFilters = {}) {
  const [items, setItems] = useState<TodoItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [filters, setFilters] = useState<TodoFilters>({ ...DEFAULT_FILTERS, ...initialFilters });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchTodos = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await todoApi.getAll(filters);
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load tasks');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => {
    fetchTodos();
  }, [fetchTodos]);

  const createTodo = useCallback(async (request: CreateTodoRequest) => {
    const created = await todoApi.create(request);
    setTotalCount((c) => c + 1);
    // Go to page 1 after creating so the new item is visible under the current sort
    setFilters((prev) => ({ ...prev, page: 1 }));
    return created;
  }, []);

  const updateTodo = useCallback(async (id: number, request: UpdateTodoRequest) => {
    const updated = await todoApi.update(id, request);
    setItems((prev) => prev.map((t) => (t.id === id ? updated : t)));
    return updated;
  }, []);

  const toggleTodo = useCallback(async (item: TodoItem) => {
    const updated = await todoApi.toggleComplete(item);
    setItems((prev) => prev.map((t) => (t.id === item.id ? updated : t)));
    return updated;
  }, []);

  const deleteTodo = useCallback(async (id: number) => {
    await todoApi.delete(id);
    setItems((prev) => prev.filter((t) => t.id !== id));
    setTotalCount((c) => c - 1);
  }, []);

  // Filter changes always reset to page 1 to avoid landing on a non-existent page
  const updateFilters = useCallback((newFilters: Partial<TodoFilters>) => {
    setFilters((prev) => ({ ...prev, ...newFilters, page: 1 }));
  }, []);

  // Page navigation does NOT reset other filters
  const goToPage = useCallback((page: number) => {
    setFilters((prev) => ({ ...prev, page }));
  }, []);

  const currentPage = filters.page ?? 1;
  const pageSize = filters.pageSize ?? 20;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return {
    items,
    totalCount,
    totalPages,
    currentPage,
    pageSize,
    filters,
    loading,
    error,
    fetchTodos,
    createTodo,
    updateTodo,
    toggleTodo,
    deleteTodo,
    updateFilters,
    goToPage,
  };
}
