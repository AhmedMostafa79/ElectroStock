using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure.Repositories
{
    public class UserRepository:IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(User entity)
        {
           await _dbContext.Users.AddAsync(entity);
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            return await _dbContext.Users.FindAsync(id);
        }
        public async Task<RegisteredUser?> GetByEmailAsync(string email)
        {
            return await _dbContext.Users
                 .OfType<RegisteredUser>() // SELECT * FROM Users where UserType='RegisteredUser'
                 .FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<User?> GetByPhoneAsync(string phone)
        {
            return await _dbContext.Users
                 .FirstOrDefaultAsync(u => u.Phone == phone);
        }
        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _dbContext.Users
                .AsNoTracking()
                .ToListAsync();
        } 
        public async Task<IEnumerable<User>> GetAllByRoleAsync(UserRole role)
        {
            return await _dbContext.Users
                .Where(u => u.Role == role)
                .AsNoTracking()
                .ToListAsync();
        }

        public void Update(User entity)
        {
            _dbContext.Users.Update(entity);
        }
        
       
    }
}
