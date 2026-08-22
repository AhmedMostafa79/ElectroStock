using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    abstract class CreateProductDto
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

        [Required]
        public ProductType ProductType { get; set; }
    }
}
