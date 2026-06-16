import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TodoList } from '../components/TodoList';
import type { TodoItem } from '../types/todo';

const mockItem: TodoItem = {
  id: 1,
  title: 'Test task',
  description: 'A description',
  isCompleted: false,
  priority: 'Medium',
  dueDate: null,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
};

const noOp = () => Promise.resolve();

describe('TodoList', () => {
  it('shows loading spinner', () => {
    render(
      <TodoList
        items={[]} loading={true} error={null}
        onToggle={noOp} onUpdate={noOp} onDelete={noOp}
      />
    );
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('shows error message', () => {
    render(
      <TodoList
        items={[]} loading={false} error="Network error"
        onToggle={noOp} onUpdate={noOp} onDelete={noOp}
      />
    );
    expect(screen.getByRole('alert')).toHaveTextContent('Network error');
  });

  it('shows empty state when no items', () => {
    render(
      <TodoList
        items={[]} loading={false} error={null}
        onToggle={noOp} onUpdate={noOp} onDelete={noOp}
      />
    );
    expect(screen.getByText(/no tasks found/i)).toBeInTheDocument();
  });

  it('renders todo items', () => {
    render(
      <TodoList
        items={[mockItem]} loading={false} error={null}
        onToggle={noOp} onUpdate={noOp} onDelete={noOp}
      />
    );
    expect(screen.getByText('Test task')).toBeInTheDocument();
    expect(screen.getByText('A description')).toBeInTheDocument();
  });

  it('calls onToggle when checkbox is clicked', async () => {
    const user = userEvent.setup();
    const onToggle = vi.fn().mockResolvedValue(undefined);
    render(
      <TodoList
        items={[mockItem]} loading={false} error={null}
        onToggle={onToggle} onUpdate={noOp} onDelete={noOp}
      />
    );

    await user.click(screen.getByRole('checkbox'));
    expect(onToggle).toHaveBeenCalledWith(mockItem);
  });
});
