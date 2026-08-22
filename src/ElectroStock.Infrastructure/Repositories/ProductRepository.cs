using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _dbContext;

        public ProductRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Product product)
        {
            await _dbContext.Products.AddAsync(product);
        }

        public async Task<IEnumerable<Product>> GetByCategoryAsync(ProductCategory category)
        {
            return await _dbContext.Products
                .Where(p=>p.Category==category)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Products.FindAsync(id);
        }

        public async Task<IEnumerable<Product>> GetBySkuAsync(string sku)
        {
            return await _dbContext.Products
                .Where(p=>p.Sku==sku)
                .ToListAsync();
        }
        //public async Task<IEnumerable<Product>> GetByCriteria(SearchCriteria)
        //{
        //    
        //}

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _dbContext.Products
                 .AsNoTracking()
                 .ToListAsync();
        }
        public void Update(Product product)
        {
           _dbContext.Products.Update(product);
        }

    }
}
