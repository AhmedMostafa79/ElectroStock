using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Dtos
{
    public class ProductDto
    {
        public Guid Id { get; init; }
        public string Sku { get; init; }
        public string Brand { get; init; }
        public string Name { get; init; }
        public ProductCategory Category { get; init; }
        public string Model { get; init; }
        public decimal UnitCost { get; init; }
        public decimal UnitPrice { get; init; }
        public ProductType ProductType { get; init; }
        public ProductStatus ProductStatus { get; init; }
        public DateTime CreatedAt { get; init; }

        // Optional properties for specific types
        public string? SerialNumber { get; init; }      // Only for SerializedProduct
        public string? Imei { get; init; }              // Only for SerializedProduct
        public WarrantyPeriod? WarrantyPeriod { get; init; }  // Only for SerializedProduct
        public DateTime? SoldDate { get; init; }         // Only for SerializedProduct

        public int? QuantityOnHand { get; init; }       // Only for QuantifiedProduct
        public int? QuantityReserved { get; init; }     // Only for QuantifiedProduct
        public int? MinimumStockLevel { get; init; }    // Only for QuantifiedProduct
        public int? AvailableQuantity { get; init; }    // Only for QuantifiedProduct

    }
}
