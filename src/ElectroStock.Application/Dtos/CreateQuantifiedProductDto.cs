using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    public class CreateQuantifiedProductDto
    {
        [Required]
        public string Sku { get; set; }

        [Required]
        public string Brand { get; set; }

        [Required]
        public string Name { get; set; }
        
        [Required]
        public ProductCategory Category { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public decimal UnitCost { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public int InitialQuantity {  get; set; }

        [Required]
        public int MinimumStockLevel {  get; set; }
    }
}
