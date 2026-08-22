using System.Data;

namespace ElectroStock.Application.Interfaces.Services
{
    /// <summary>
    /// Defines a transactional boundary for coordinating repository changes as a single unit of work.
    /// </summary>
    public interface IUnitOfWork
    {

        /// <summary>
        /// Starts a new database transaction using the requested isolation level.
        /// </summary>
        /// <param name="isolationLevel">Isolation level used for the transaction scope.</param>
        Task BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);

        /// <summary>
        /// Commits the active transaction and persists all pending changes within that transaction.
        /// </summary>
        Task CommitTransactionAsync();

        /// <summary>
        /// Rolls back the active transaction and discards uncommitted changes in that scope.
        /// </summary>
        Task RollbackTransactionAsync();

        /// <summary>
        /// Persists pending context changes when no explicit transaction commit is required.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        Task<int> SaveChangesAsync();

        /// <summary>
        /// Clears tracked entities from the current context to prevent stale tracking state.
        /// </summary>
        void DetachAllEntities();

        /// <summary>
        /// Releases unit-of-work resources and database-related handles.
        /// </summary>
        void Dispose();
    }
}
