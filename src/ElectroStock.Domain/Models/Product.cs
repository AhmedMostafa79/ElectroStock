using ElectroStock.Domain.Enums;
using ElectroStock.Domain.ValueObjects;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ElectroStock.Domain.Models
{
    public abstract class Product
    {
        // Parameterless constructor for EF Core materialization (TPH)
        protected Product() { }

        [Key]
        public Guid Id { get; private set; }

        [Required]
        [MaxLength(50)]
        public string Sku { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Brand { get; private set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; private set; }

        [Required]
        public ProductType ProductType { get; private set; }

        [Required]
        public ProductCategory Category { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Model { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitCost { get; private set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get;  private set; }

        [Required]
        public ProductStatus Status { get; private set; }

        [Required]
        public DateTime CreatedAt { get; private set; }
       public Product(ProductCreationData data ,ProductType productType)
        {
            ValidateSku(data.Sku);
            Id = Guid.NewGuid();

            Sku = data.Sku;
            Brand = data.Brand;
            Name = data.Name;

            Category = data.Category;

            Model = data.Model;


            ValidateAmount(data.UnitCost,nameof(UnitCost));
            UnitCost=data.UnitCost;

            ChangePrice(data.UnitPrice);

            ProductType= productType;
            Status = ProductStatus.Draft;
            CreatedAt = DateTime.UtcNow;
        }
        public void Discontinue( )
        {
            if (Status == ProductStatus.SoldOut)
                throw new InvalidOperationException($"Can't discontinue product with status: {Status}");

                Status = ProductStatus.Discontinued;
        }
        public void Publish( )
        {
            Status=ProductStatus.Active;
        }
        
        public void ChangePrice(decimal price)
        {
            if (price < UnitCost)
                throw new InvalidOperationException($"Unit Price can't be less than Unit Cost, " +
                $"Price to change {price}, Unit Cost {UnitCost}");

            ValidateAmount(price, nameof(price));
            UnitPrice = price;
        }
        public void ChangeCost(decimal cost)
        {
            if (cost > UnitPrice)
                throw new InvalidOperationException($"Unit Price can't be less than Unit Cost, " +
                $"Unit Price {UnitPrice}, Cost to change{cost}");

            ValidateAmount(cost,nameof(cost));
            UnitCost = cost;
        }
        protected void ChangeStatus(ProductStatus status)
        {
            Status=status;
        }
        private void ValidateAmount(decimal amount, string fieldName)
        {
            if (amount < 0)
                throw new ArgumentException($"{fieldName} can't be negative! {fieldName} {amount} ");
        }
        private void ValidateSku(string sku)
        {
            // matches "XX-YY-ZZ"
            string skuRegex = "^[A-Za-z]{2,}-[A-Za-z0-9]{2,}-[A-Za-z0-9]{2,}$";
            if (!System.Text.RegularExpressions.Regex.IsMatch(sku, skuRegex))
                throw new ArgumentException($"Invalid Sku: {sku}!");
        }
        public abstract bool CanReserveOrSell(int quantity = 1);
        public abstract void Reserve(int quantity = 1);
        public abstract void ConfirmSale(int quantity = 1);
        public abstract void ConfirmReservedSale(int quantity);
        public abstract void ReleaseReservation(int quantity = 1);
        public abstract void Refund(int quantity = 1);
    }
}
