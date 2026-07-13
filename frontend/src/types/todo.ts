// Mirrors the backend DTOs exactly so the API contract is explicit in the codebase.

export type Priority = 'Low' | 'Medium' | 'High';

export interface TodoItem {
  id: number;
  title: string;
  description: string | null;
  isCompleted: boolean;
  priority: Priority;
  dueDate: string | null; // ISO 8601 string from JSON
  createdAt: string;
  updatedAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateTodoRequest {
  title: string;
  description?: string;
  priority?: Priority;
  dueDate?: string;
}

export interface UpdateTodoRequest {
  title?: string;
  // null explicitly clears the field; undefined (absent) leaves it unchanged.
  // Mirrors the backend Optional<T> pattern introduced in the PATCH handler.
  description?: string | null;
  isCompleted?: boolean;
  priority?: Priority;
  dueDate?: string | null;
}

export interface TodoFilters {
  isCompleted?: boolean;
  priority?: Priority;
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: 'dueDate' | 'createdAt' | 'priority' | 'title';
  sortDir?: 'asc' | 'desc';
}
