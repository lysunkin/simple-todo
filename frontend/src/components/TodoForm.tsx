import { useState, type FormEvent } from 'react';
import type { CreateTodoRequest, Priority } from '../types/todo';

interface Props {
  onSubmit: (request: CreateTodoRequest) => Promise<unknown>;
}

const PRIORITIES: Priority[] = ['Low', 'Medium', 'High'];

/**
 * Controlled form for creating a new todo item.
 * Validation is intentionally minimal here — the real guard is the backend.
 * A production app might use React Hook Form + Zod for richer client-side validation.
 */
export function TodoForm({ onSubmit }: Props) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [priority, setPriority] = useState<Priority>('Medium');
  const [dueDate, setDueDate] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    setSubmitting(true);
    setError(null);
    try {
      await onSubmit({
        title: title.trim(),
        description: description.trim() || undefined,
        priority,
        dueDate: dueDate || undefined,
      });
      // Reset form on success
      setTitle('');
      setDescription('');
      setPriority('Medium');
      setDueDate('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create task');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="todo-form" aria-label="Add new task">
      <h2>Add Task</h2>

      {error && (
        <div role="alert" className="error-banner">
          {error}
        </div>
      )}

      <div className="form-group">
        <label htmlFor="title">Title *</label>
        <input
          id="title"
          type="text"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="What needs to be done?"
          maxLength={200}
          required
          disabled={submitting}
        />
      </div>

      <div className="form-group">
        <label htmlFor="description">Description</label>
        <textarea
          id="description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="Optional details..."
          rows={2}
          maxLength={2000}
          disabled={submitting}
        />
      </div>

      <div className="form-row">
        <div className="form-group">
          <label htmlFor="priority">Priority</label>
          <select
            id="priority"
            value={priority}
            onChange={(e) => setPriority(e.target.value as Priority)}
            disabled={submitting}
          >
            {PRIORITIES.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label htmlFor="dueDate">Due Date</label>
          <input
            id="dueDate"
            type="date"
            value={dueDate}
            onChange={(e) => setDueDate(e.target.value)}
            disabled={submitting}
          />
        </div>
      </div>

      <button type="submit" disabled={submitting || !title.trim()} className="btn btn-primary">
        {submitting ? 'Adding…' : 'Add Task'}
      </button>
    </form>
  );
}
