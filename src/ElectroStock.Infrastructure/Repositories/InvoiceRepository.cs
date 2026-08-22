using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AppDbContext _dbContext;
        public InvoiceRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Invoice invoice)
        {
             await _dbContext.Invoices.AddAsync(invoice);
        }

        public async Task<Invoice?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Invoices
                        .FindAsync(id);
        } 
        public async Task<Invoice?> GetByNumberAsync(string invoiceNumber)
        {
            return await _dbContext.Invoices
                        .FirstOrDefaultAsync(i=>i.InvoiceNumber==invoiceNumber);
        }
        public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId)
        {
            return await _dbContext.Invoices
                .AsNoTracking()
                .Where(i => i.CustomerId == customerId)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
        }

        public async  Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await _dbContext.Invoices
                .AsNoTracking()
                .ToListAsync();
        }

        public void Update(Invoice invoice)
        {
            _dbContext.Invoices.Update(invoice);
        }
    }
}
