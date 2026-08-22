using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Microsoft.Extensions.DependencyInjection;
using ElectroStock.Infrastructure;
using ElectroStock.Domain.Interfaces.Repositories;
using ElectroStock.Infrastructure.Repositories;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Services;
using ElectroStock.Application.Interfaces.Services;
using ElectroStock.Infrastructure.Services;
using Microsoft.Extensions.Options;
using ElectroStock.Application.Helpers;
using Microsoft.Extensions.Configuration;

namespace ElectroStock.Tests.IntegrationTests.Fixtures
{
    // the fixture is as the sandbox that is used to configure test database and 
    // setup used services and their instances and scopes 
    // so it is destroyed once the test is finished
    // and also used at each test with fresh empty ready database
    public class TestContainersFixture : IAsyncLifetime
    {
        private MsSqlContainer _container; //MsSql database test-container
        private IServiceScope _scope; //scope where we define scope of services instance

        public IServiceProvider ServiceProvider { get; private set; }// provider that is used to provide services 

        public async Task InitializeAsync()
        {
            // start SQL Server container

            _container = new MsSqlBuilder()
                   .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                   .WithPassword("YourStrong!Passw0rd")
                   .WithEnvironment("ACCEPT_EULA", "Y")
                   .Build();


            // run the database image
            await _container.StartAsync();

            // passing the MsSql container as the default database 
            var appOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(_container.GetConnectionString())
                .Options;

            // DbContext now references the test-container

            using (var dbContext = new AppDbContext(appOptions))
            {
                await dbContext.Database.MigrateAsync();
            }


            // AuditContext
            var auditOptions = new DbContextOptionsBuilder<AuditDbContext>()
        .UseSqlServer(_container.GetConnectionString())
        .Options;

            using (var auditContext = new AuditDbContext(auditOptions))
            {
                await auditContext.Database.MigrateAsync();
            }

            // Build DI container with REAL services (not mocks!), DI as the program.cs
            ServiceProvider = BuilderServiceProvider();

            _scope = ServiceProvider.CreateScope();
        }

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
            _scope?.Dispose();
        }

        private IServiceProvider BuilderServiceProvider()
        {
            var services = new ServiceCollection();

            // Register Real new DbContext 
            services.AddScoped(sp =>
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(_container.GetConnectionString())
                    .Options;
                return new AppDbContext(options);
            });

            // Register Real new AuditDbContext 
            services.AddScoped(sp =>
            {
                var options = new DbContextOptionsBuilder<AuditDbContext>()
                    .UseSqlServer(_container.GetConnectionString())
                    .Options;
                return new AuditDbContext(options);
            });


            // Register REAL Repositories
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IReservationRepository, ReservationRepository>();
            services.AddScoped<IStockMovementRepository, StockMovementRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ISequenceCounterRepository, SequenceCounterRepository>();


            // Register REAL Services
            services.AddScoped<IAuditLogService, AuditLogService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IReservationService, ReservationService>();
            services.AddScoped<ISequenceNumberGenerator, SequenceNumberGenerator>();
            services.AddScoped<IStockService, StockService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITokenService, JwtTokenGenerator>();


            // Register REAL Helpers
            services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("C:\\Users\\J.AHMED\\source\\repos\\C#\\ElectroStock\\appsettings.json") // or add in-memory config
                .Build();

            services.Configure<JWT>(configuration.GetSection("JWT"));
            return services.BuildServiceProvider();
        }

        // Helper to clean database between tests (to keep them isolated)
        public async Task CleanDatabaseAsync()
        {
            using var scope = ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM StockMovements");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Invoices");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Reservations");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Products");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM Users");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM SequenceCounters");
            await dbContext.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs");
            await dbContext.SaveChangesAsync();

        }
    }
}
