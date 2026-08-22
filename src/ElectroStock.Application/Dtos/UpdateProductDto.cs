using ElectroStock.Domain.Enums;

namespace ElectroStock.Application.Dtos
{
    public class UpdateProductDto
    {
        public string Brand { get; set; }
        public string Name { get; set; }
        public ProductCategory Category { get; set; }
        public string Model { get; set; }
        public decimal UnitCost { get; set; }
        public decimal UnitPrice { get; set; }

        // Optional properties for specific types
        public WarrantyPeriod? WarrantyPeriod { get; set; }  // Only for SerializedProduct

        public int? MinimumStockLevel { get; set; }    // Only for QuantifiedProduct
    }
}
