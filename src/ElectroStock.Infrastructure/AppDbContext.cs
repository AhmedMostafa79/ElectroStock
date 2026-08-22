using ElectroStock.Domain.Enums;
using ElectroStock.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectroStock.Infrastructure
{
    public class AppDbContext:DbContext
    {
        public DbSet<User> Users {  get; set; } //Staff 
        public DbSet<Product> Products { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<SequenceCounter> SequenceCounters { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<StockMovement> StockMovements { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPH(Table Per Hierarchy) configuration

            // Product Configuration with discriminator values
            modelBuilder.Entity<Product>()
                .HasDiscriminator<ProductType>("ProductType")
                .HasValue<QuantifiedProduct>(ProductType.Quantified)
                .HasValue<SerializedProduct>(ProductType.Serialized);

            // User Configuration with discriminator values
            modelBuilder.Entity<User>()
                .HasDiscriminator<UserType>("UserType")
                .HasValue<RegisteredUser>(UserType.Registered)
                .HasValue<GuestUser>(UserType.Guest);
            
            // configuring table for InvoiceItem table, to ensure it is owned by Invoice
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

                // invoice has many invoiceItems (Value Objects)
                // so it creates a InvoiceItem table where it references Invoice table
                entity.OwnsMany(e => e.Items, item =>
                {
                    item.WithOwner().HasForeignKey("InvoiceId");
                    item.Property(x => x.ProductId).IsRequired();
                    item.Property(x => x.Quantity).IsRequired();
                    item.Property(x => x.UnitPrice)
                    .IsRequired();

                });

                modelBuilder.Entity<SequenceCounter>(entity =>
                {
                    entity.ToTable("SequenceCounters");

                    entity.Property(e => e.Id)
                        .ValueGeneratedNever()  // ← This is the key!
                        .IsRequired();

                    entity.Property(e => e.CounterType)
                        .IsRequired()
                        .HasMaxLength(10);

                    entity.HasIndex(e => e.CounterType).IsUnique();
                });
            });

            // Configure
            // ===== Models Constraints =====

          
        }
    }
}
