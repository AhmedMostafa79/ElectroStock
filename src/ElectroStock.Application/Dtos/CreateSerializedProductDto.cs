using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    public class CreateSerializedProductDto
    {

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
        public string SerialNumber { get; set; }

        [Required]
        public string? Imei { get; set; }


        public WarrantyPeriod WarrantyPeriod { get; set; }
    }
}
