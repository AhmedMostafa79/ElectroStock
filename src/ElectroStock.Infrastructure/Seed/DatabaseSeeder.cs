using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectroStock.Infrastructure.Seed
{
    public class DatabaseSeeder:IDatabaseSeeder
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHasher _passwordHasher;
        private readonly AdminSeedSettings _adminSettings;

        public DatabaseSeeder(AppDbContext dbContext, IPasswordHasher passwordHasher,
            IOptions<AdminSeedSettings>adminSettings)
        {
            _dbContext = dbContext;
            _passwordHasher = passwordHasher;
            _adminSettings = adminSettings.Value; 
        }

        public async Task SeedAsync()
        {
            await SeedAdminUserAsync();
        }
        private async Task SeedAdminUserAsync()
        {
            var email = _adminSettings.Email;
            var password =_adminSettings.Password;

            bool isAdminExists = await _dbContext.Users
                .OfType<RegisteredUser>()
                .AnyAsync(u => u.Email == email);

            if (isAdminExists)// already seeded
                return;

            var admin = new RegisteredUser(
                data: new UserCreationData {
                    PhoneNumber= _adminSettings.Phone,
                    FirstName= _adminSettings.FirstName,
                    LastName= _adminSettings.LastName,
                    Role= UserRole.Admin
                    },
                email:email,
                passwordHash:_passwordHasher.HashPassword(password));

            await _dbContext.Users.AddAsync(admin);
            await _dbContext.SaveChangesAsync();
        }
    }
}
