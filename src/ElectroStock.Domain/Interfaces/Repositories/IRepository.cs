using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Domain.Interfaces.Repositories
{
    /// <summary>
    /// Defines the basic persistence operations shared across aggregate repositories.
    /// </summary>
    /// <typeparam name="T">Aggregate root entity type.</typeparam>
    public interface IRepository <T> where T: class
    {
        /// <summary>
        /// Adds a new entity to the current persistence context.
        /// </summary>
        /// <param name="entity">Entity instance to add.</param>
        public Task AddAsync(T entity);

        /// <summary>
        /// Marks an existing entity as modified in the current persistence context.
        /// </summary>
        /// <param name="entity">Entity instance to update.</param>
        public void Update(T entity);

        /// <summary>
        /// Retrieves an entity by its identifier.
        /// </summary>
        /// <param name="id">Entity identifier.</param>
        /// <returns>The matching entity, or <c>null</c> when not found.</returns>
        public  Task<T?> GetByIdAsync(Guid id);

        /// <summary>
        /// Retrieves all entities of the repository type.
        /// </summary>
        /// <returns>All persisted entities for the type.</returns>
        public Task<IEnumerable<T>> GetAllAsync();

    }
}
