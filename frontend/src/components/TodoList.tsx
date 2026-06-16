import type { TodoItem as TodoItemType, UpdateTodoRequest } from '../types/todo';
import { TodoItem } from './TodoItem';

interface Props {
  items: TodoItemType[];
  loading: boolean;
  error: string | null;
  onToggle: (item: TodoItemType) => Promise<unknown>;
  onUpdate: (id: number, request: UpdateTodoRequest) => Promise<unknown>;
  onDelete: (id: number) => Promise<void>;
}

/**
 * Renders the list of todo items with loading, error, and empty states.
 * Separating the list container from the item keeps each component focused.
 */
export function TodoList({ items, loading, error, onToggle, onUpdate, onDelete }: Props) {
  if (loading) {
    return (
      <div className="state-container" aria-busy="true" aria-label="Loading tasks">
        <div className="spinner" aria-hidden="true" />
        <p>Loading tasks…</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="state-container error-state" role="alert">
        <p>⚠️ {error}</p>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="state-container empty-state" aria-label="No tasks found">
        <p>No tasks found. Add one above!</p>
      </div>
    );
  }

  return (
    <ul className="todo-list" aria-label="Task list">
      {items.map((item) => (
        <TodoItem
          key={item.id}
          item={item}
          onToggle={onToggle}
          onUpdate={onUpdate}
          onDelete={onDelete}
        />
      ))}
    </ul>
  );
}
