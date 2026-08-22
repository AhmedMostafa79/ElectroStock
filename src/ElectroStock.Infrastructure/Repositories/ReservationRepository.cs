using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _dbContext;

        public ReservationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Reservation reservation)
        {
           await _dbContext.Reservations.AddAsync(reservation);
        }

        public async Task<Reservation?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Reservations.FindAsync(id);
        }
        public async Task<Reservation?> GetByNumberAsync(string reservationNumber)
        {
            return await _dbContext.Reservations.FirstOrDefaultAsync(r=>r.ReservationNumber == reservationNumber);
        }

        public async Task<IEnumerable<Reservation>> GetByCustomerAsync(Guid customerId)
        {
            return await _dbContext.Reservations
                .AsNoTracking()
                .Where(r => r.CustomerId == customerId)
                .OrderBy(r=>r.CreatedAt)
                .ToListAsync();
        }
        public async Task<IEnumerable<Reservation>> GetByProductAsync(Guid productId)
        {
            return await _dbContext.Reservations
                .Where(r => r.ProductId == productId)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<IEnumerable<Reservation>> GetByProductIdsAsync(List<Guid> productIds)
        {
            return await _dbContext.Reservations
                .Where(r=>productIds.Contains(r.ProductId))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Reservation>> GetAllAsync()
        {
             return await _dbContext.Reservations // SELECT * FROM Reservations
                .AsNoTracking()
                .ToListAsync();
        }

        public void Update(Reservation reservation)
        {
            _dbContext.Reservations.Update(reservation);
        }
      
    }
}
