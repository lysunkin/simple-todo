import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TodoForm } from '../components/TodoForm';

describe('TodoForm', () => {
  it('renders the form fields', () => {
    render(<TodoForm onSubmit={vi.fn()} />);
    expect(screen.getByLabelText(/title/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/description/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/priority/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/due date/i)).toBeInTheDocument();
  });

  it('submit button is disabled when title is empty', () => {
    render(<TodoForm onSubmit={vi.fn()} />);
    expect(screen.getByRole('button', { name: /add task/i })).toBeDisabled();
  });

  it('submit button enables when title is filled', async () => {
    const user = userEvent.setup();
    render(<TodoForm onSubmit={vi.fn()} />);

    await user.type(screen.getByLabelText(/title/i), 'My task');
    expect(screen.getByRole('button', { name: /add task/i })).not.toBeDisabled();
  });

  it('calls onSubmit with correct data and resets the form', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<TodoForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText(/title/i), 'Buy milk');
    await user.selectOptions(screen.getByLabelText(/priority/i), 'High');
    await user.click(screen.getByRole('button', { name: /add task/i }));

    await waitFor(() => {
      expect(onSubmit).toHaveBeenCalledWith(
        expect.objectContaining({ title: 'Buy milk', priority: 'High' })
      );
    });

    // Form resets after submit
    expect(screen.getByLabelText(/title/i)).toHaveValue('');
  });

  it('shows error message when onSubmit rejects', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn().mockRejectedValue(new Error('Server error'));
    render(<TodoForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText(/title/i), 'Task');
    await user.click(screen.getByRole('button', { name: /add task/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Server error');
    });
  });
});
