import { useTodos } from './hooks/useTodos';
import { TodoForm } from './components/TodoForm';
import { TodoFilterBar } from './components/TodoFilters';
import { TodoList } from './components/TodoList';
import { Pagination } from './components/Pagination';
import './App.css';

function App() {
  const {
    items,
    totalCount,
    totalPages,
    currentPage,
    pageSize,
    filters,
    loading,
    error,
    createTodo,
    updateTodo,
    toggleTodo,
    deleteTodo,
    updateFilters,
    goToPage,
  } = useTodos();

  const completedCount = items.filter((t) => t.isCompleted).length;

  return (
    <div className="app">
      <header className="app-header">
        <h1>📋 Todo App</h1>
        <p className="app-subtitle">
          {totalCount} task{totalCount !== 1 ? 's' : ''} · {completedCount} completed
        </p>
      </header>

      <main className="app-main">
        <section aria-label="Add new task">
          <TodoForm onSubmit={createTodo} />
        </section>

        <section aria-label="Task filters">
          <TodoFilterBar filters={filters} onChange={updateFilters} />
        </section>

        <section aria-label="Task list">
          <TodoList
            items={items}
            loading={loading}
            error={error}
            onToggle={toggleTodo}
            onUpdate={updateTodo}
            onDelete={deleteTodo}
          />

          <Pagination
            currentPage={currentPage}
            totalPages={totalPages}
            totalCount={totalCount}
            pageSize={pageSize}
            onPageChange={goToPage}
          />
        </section>
      </main>
    </div>
  );
}

export default App;
