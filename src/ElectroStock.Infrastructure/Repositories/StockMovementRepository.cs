using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure.Repositories
{
    public class StockMovementRepository : IStockMovementRepository
    {
        private readonly AppDbContext _dbContext;

        public StockMovementRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(StockMovement stockMovement)
        {
            await _dbContext.StockMovements.AddAsync(stockMovement);
        }

        public async Task<IEnumerable<StockMovement>> GetByDateRangeAsync(Guid productId, DateTime start, DateTime end)
        {
            return await _dbContext.StockMovements // SELECT * FROM StockMovements WHERE 
                .Where(s=>s.ProductId==productId && start <= s.CreatedAt && s.CreatedAt <= end)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<StockMovement>> GetByProductIdAsync(Guid productId)
        {
           return await _dbContext.StockMovements
                .Where(s => productId == s.ProductId)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
