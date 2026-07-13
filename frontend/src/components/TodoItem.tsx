import { useState } from 'react';
import type { TodoItem as TodoItemType, Priority, UpdateTodoRequest } from '../types/todo';

interface Props {
  item: TodoItemType;
  onToggle: (item: TodoItemType) => Promise<unknown>;
  onUpdate: (id: number, request: UpdateTodoRequest) => Promise<unknown>;
  onDelete: (id: number) => Promise<void>;
}

const PRIORITY_COLORS: Record<Priority, string> = {
  High: '#ef4444',
  Medium: '#f59e0b',
  Low: '#10b981',
};

/**
 * Displays a single todo item with inline edit support.
 * Editing is toggled by the Edit button — no separate edit page/modal needed
 * for this scale of app. A modal would be appropriate if more fields were added.
 */
export function TodoItem({ item, onToggle, onUpdate, onDelete }: Props) {
  const [editing, setEditing] = useState(false);
  const [editTitle, setEditTitle] = useState(item.title);
  const [editDescription, setEditDescription] = useState(item.description ?? '');
  const [editPriority, setEditPriority] = useState<Priority>(item.priority);
  const [saving, setSaving] = useState(false);

  const handleToggle = async () => {
    await onToggle(item);
  };

  const handleSave = async () => {
    if (!editTitle.trim()) return;
    setSaving(true);
    try {
      await onUpdate(item.id, {
        title: editTitle.trim(),
        // Send null when the field is cleared so the backend actually removes the value.
        // Sending undefined (absent) would leave the existing value unchanged.
        description: editDescription.trim() || null,
        priority: editPriority,
      });
      setEditing(false);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async () => {
    if (window.confirm(`Delete "${item.title}"?`)) {
      await onDelete(item.id);
    }
  };

  const isOverdue =
    !item.isCompleted &&
    item.dueDate != null &&
    new Date(item.dueDate) < new Date();

  if (editing) {
    return (
      <li className="todo-item todo-item--editing" aria-label={`Editing: ${item.title}`}>
        <input
          type="text"
          value={editTitle}
          onChange={(e) => setEditTitle(e.target.value)}
          maxLength={200}
          required
          autoFocus
          aria-label="Edit title"
        />
        <textarea
          value={editDescription}
          onChange={(e) => setEditDescription(e.target.value)}
          rows={2}
          maxLength={2000}
          aria-label="Edit description"
        />
        <select
          value={editPriority}
          onChange={(e) => setEditPriority(e.target.value as Priority)}
          aria-label="Edit priority"
        >
          <option value="Low">Low</option>
          <option value="Medium">Medium</option>
          <option value="High">High</option>
        </select>
        <div className="todo-item__actions">
          <button onClick={handleSave} disabled={saving || !editTitle.trim()} className="btn btn-primary btn-sm">
            {saving ? 'Saving…' : 'Save'}
          </button>
          <button onClick={() => setEditing(false)} className="btn btn-ghost btn-sm">
            Cancel
          </button>
        </div>
      </li>
    );
  }

  return (
    <li
      className={`todo-item ${item.isCompleted ? 'todo-item--completed' : ''} ${isOverdue ? 'todo-item--overdue' : ''}`}
      aria-label={item.title}
    >
      {/* Priority indicator */}
      <span
        className="priority-dot"
        style={{ backgroundColor: PRIORITY_COLORS[item.priority] }}
        aria-label={`Priority: ${item.priority}`}
        title={`${item.priority} priority`}
      />

      {/* Completion checkbox */}
      <input
        type="checkbox"
        checked={item.isCompleted}
        onChange={handleToggle}
        aria-label={item.isCompleted ? 'Mark incomplete' : 'Mark complete'}
        className="todo-checkbox"
      />

      <div className="todo-item__content">
        <span className={`todo-item__title ${item.isCompleted ? 'strikethrough' : ''}`}>
          {item.title}
        </span>
        {item.description && (
          <p className="todo-item__description">{item.description}</p>
        )}
        <div className="todo-item__meta">
          <span className="badge" style={{ borderColor: PRIORITY_COLORS[item.priority] }}>
            {item.priority}
          </span>
          {item.dueDate && (
            <span className={`due-date ${isOverdue ? 'due-date--overdue' : ''}`}>
              Due: {new Date(item.dueDate).toLocaleDateString()}
            </span>
          )}
        </div>
      </div>

      <div className="todo-item__actions">
        <button
          onClick={() => setEditing(true)}
          className="btn btn-ghost btn-sm"
          aria-label={`Edit ${item.title}`}
          disabled={item.isCompleted}
        >
          Edit
        </button>
        <button
          onClick={handleDelete}
          className="btn btn-danger btn-sm"
          aria-label={`Delete ${item.title}`}
        >
          Delete
        </button>
      </div>
    </li>
  );
}
