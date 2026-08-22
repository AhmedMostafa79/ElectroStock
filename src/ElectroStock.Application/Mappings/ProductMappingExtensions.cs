using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Models;
using ElectroStock.Domain.ValueObjects;

namespace ElectroStock.Application.Mappings
{
    public static class ProductMappingExtensions
    {
        public static ProductDto ToDto(this Product product)
        {
            if (product == null)
                return null;

            return product switch
    {
        SerializedProduct s => new ProductDto
        {
            Id = s.Id,
            Sku = s.Sku,
            Brand = s.Brand,
            Name = s.Name,
            Category = s.Category,
            Model = s.Model,
            UnitCost = s.UnitCost,
            UnitPrice = s.UnitPrice,
            ProductType = s.ProductType,
            ProductStatus = s.Status,
            CreatedAt = s.CreatedAt,
            // Serialized-specific properties
            SerialNumber = s.SerialNumber,
            Imei = s.Imei,
            WarrantyPeriod = s.WarrantyPeriod,
            SoldDate = s.SoldDate,
        },
        
        QuantifiedProduct q => new ProductDto
        {
            Id = q.Id,
            Sku = q.Sku,
            Brand = q.Brand,
            Name = q.Name,
            Category = q.Category,
            Model = q.Model,
            UnitCost = q.UnitCost,
            UnitPrice = q.UnitPrice,
            ProductType = q.ProductType,
            ProductStatus = q.Status,
            CreatedAt = q.CreatedAt,
            // Quantified-specific properties
            QuantityOnHand = q.QuantityOnHand,
            QuantityReserved = q.QuantityReserved,
            MinimumStockLevel = q.MinimumStockLevel,
            AvailableQuantity = q.AvailableQuantity
        },
        
        _ => throw new InvalidOperationException("Unknown product type")
    };

        }
        public static ProductCreationData ToCreationData(this CreateQuantifiedProductDto productDto)
        {
            if (productDto == null)
                return null;

            return new ProductCreationData {
                Sku = productDto.Sku,
                Brand = productDto.Brand,
                Name = productDto.Name,
                Category = productDto.Category,
                Model = productDto.Model,
                UnitCost = productDto.UnitCost,
                UnitPrice = productDto.UnitPrice,
            };
        } 
        public static ProductCreationData ToCreationData(this CreateSerializedProductDto productDto, string sku)
        {
            if (productDto == null)
                return null;

            return new ProductCreationData {
                Sku = sku,
                Brand = productDto.Brand,
                Name = productDto.Name,
                Category = productDto.Category,
                Model = productDto.Model,
                UnitCost = productDto.UnitCost,
                UnitPrice= productDto.UnitPrice
            };
        }
    }
}
