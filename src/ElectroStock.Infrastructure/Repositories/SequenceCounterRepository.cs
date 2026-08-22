using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure.Repositories
{
    public class SequenceCounterRepository : ISequenceCounterRepository
    {
        private readonly AppDbContext _dbContext;
        public SequenceCounterRepository(AppDbContext dbContext)
        {
            _dbContext= dbContext;
        }
        public async Task<SequenceCounter> GetOrCreateAsync(CounterType type)
        {
            var counter = await _dbContext.SequenceCounters.FirstOrDefaultAsync(c => c.CounterType == type);
            if(counter == null)
            {
                counter=new SequenceCounter(type,DateOnly.FromDateTime(DateTime.UtcNow));
                await _dbContext.SequenceCounters.AddAsync(counter);
            }
            return counter;
        }
        public void Update(SequenceCounter counter)
        {
            _dbContext.SequenceCounters.Update(counter);
        }
    }
}
