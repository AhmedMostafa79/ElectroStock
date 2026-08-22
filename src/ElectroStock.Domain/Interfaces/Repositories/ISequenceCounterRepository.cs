using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectroStock.Application.Interfaces.Services
{
    /// <summary>
    /// Provides sequence counter persistence operations used for business number generation.
    /// </summary>
    public interface ISequenceCounterRepository

    {
        /// <summary>
        /// Retrieves the counter for a type, creating an initial counter record when none exists.
        /// </summary>
        /// <param name="type">Counter type (for example, invoice or reservation).</param>
        /// <returns>An existing or newly created sequence counter.</returns>
        public Task<SequenceCounter> GetOrCreateAsync(CounterType type);

        /// <summary>
        /// Marks a sequence counter as modified in the current persistence context.
        /// </summary>
        /// <param name="counter">Counter entity to update.</param>
        public void Update(SequenceCounter counter);
    }
}
