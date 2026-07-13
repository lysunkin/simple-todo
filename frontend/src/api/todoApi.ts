import axios from 'axios';
import type {
  TodoItem,
  PagedResponse,
  CreateTodoRequest,
  UpdateTodoRequest,
  TodoFilters,
} from '../types/todo';

/**
 * Base URL comes from an environment variable injected at build time.
 * In dev (Vite proxy), /api is forwarded to the .NET backend.
 * In Docker, nginx proxies /api to the backend container.
 */
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '',
  headers: { 'Content-Type': 'application/json' },
});

// Centralised error handling: extract the opaque errorId returned by the
// backend's ErrorHandlingMiddleware and surface that to the user.
// The full error details are in the server logs under the same ID.
api.interceptors.response.use(
  (res) => res,
  (err) => {
    const data = err.response?.data;

    // Backend ErrorHandlingMiddleware returns { errorId, message }
    if (data?.errorId) {
      return Promise.reject(new Error(`Something went wrong. Reference: ${data.errorId}`));
    }

    // Validation errors (400): two possible shapes:
    //   1. { errorId, message } — thrown by the service layer (e.g. invalid priority)
    //   2. { title, ... }      — ASP.NET model-binding / DataAnnotations failures
    if (err.response?.status === 400) {
      if (data?.message) return Promise.reject(new Error(data.message));
      if (data?.title)   return Promise.reject(new Error(data.title));
    }

    // Network-level failure (no response at all)
    if (!err.response) {
      return Promise.reject(new Error('Unable to reach the server. Check your connection.'));
    }

    return Promise.reject(new Error('An unexpected error occurred.'));
  }
);

export const todoApi = {
  getAll: async (filters: TodoFilters = {}): Promise<PagedResponse<TodoItem>> => {
    const params = {
      ...(filters.isCompleted !== undefined && { isCompleted: filters.isCompleted }),
      ...(filters.priority && { priority: filters.priority }),
      ...(filters.search && { search: filters.search }),
      page: filters.page ?? 1,
      pageSize: filters.pageSize ?? 20,
      sortBy: filters.sortBy ?? 'dueDate',
      sortDir: filters.sortDir ?? 'asc',
    };
    const { data } = await api.get<PagedResponse<TodoItem>>('/api/todo', { params });
    return data;
  },

  getById: async (id: number): Promise<TodoItem> => {
    const { data } = await api.get<TodoItem>(`/api/todo/${id}`);
    return data;
  },

  create: async (request: CreateTodoRequest): Promise<TodoItem> => {
    const { data } = await api.post<TodoItem>('/api/todo', request);
    return data;
  },

  update: async (id: number, request: UpdateTodoRequest): Promise<TodoItem> => {
    const { data } = await api.patch<TodoItem>(`/api/todo/${id}`, request);
    return data;
  },

  delete: async (id: number): Promise<void> => {
    await api.delete(`/api/todo/${id}`);
  },

  /** Convenience toggle — keeps the toggle logic out of components. */
  toggleComplete: async (item: TodoItem): Promise<TodoItem> =>
    todoApi.update(item.id, { isCompleted: !item.isCompleted }),
};
