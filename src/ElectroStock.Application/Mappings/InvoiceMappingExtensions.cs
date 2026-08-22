using ElectroStock.Application.Dtos;
using ElectroStock.Domain.Models;

namespace ElectroStock.Application.Mappings
{
    public static class InvoiceMappingExtension
    {
        public static InvoiceDto ToDto(this Invoice invoice)
        {
            if (invoice == null)
                return null;

            return new InvoiceDto
            {
                Id=invoice.Id,
                InvoiceNumber=invoice.InvoiceNumber,
                CustomerId=invoice.CustomerId,
                Items=invoice.Items,
                TotalPrice=invoice.TotalPrice,
                OrderSource=invoice.OrderSource,
                CreatedBy=invoice.CreatedBy,
                PaymentMethod=invoice.PaymentMethod,
                CreatedAt=invoice.CreatedAt
                
            };
        }
    }
}
