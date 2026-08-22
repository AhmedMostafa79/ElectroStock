using ElectroStock.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Domain.Models
{
    public class Invoice
    {
        private readonly List<InvoiceItem> _items = new();
        [Key]
        public Guid Id { get; private set; }

        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; private set; }

        [Required]
        public Guid CustomerId { get; private set; }

        [Required]
        public RequestSource OrderSource { get; private set; }

        public InvoiceStatus Status { get; private set; }
        
        [Required]
        public Guid CreatedBy { get; private set; }

        [Required]
        public PaymentMethod PaymentMethod { get; private set; }

        public decimal TotalPrice=>_items.Select(x => x.TotalPrice()).Sum();

        [Required]
        public DateTime CreatedAt { get; private set; }

        //can be wrapped(assigned to other variable) but can't be modified
        public IReadOnlyCollection<InvoiceItem>Items=>_items.AsReadOnly();

        public Invoice(string invoiceNumber, Guid customerId,
            RequestSource orderSource, Guid createdBy, PaymentMethod paymentMethod)
        {
            Id = Guid.NewGuid();
            InvoiceNumber = invoiceNumber;
            CustomerId = customerId;
            OrderSource = orderSource;
            CreatedBy = createdBy;
            PaymentMethod = paymentMethod;
            CreatedAt = DateTime.UtcNow;
            Status = InvoiceStatus.Completed;
        }
        public void AddItem(Guid productId, int quantity, decimal unitPrice)
        {
            if (Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException($"Invoice is {Status}, can't add items to invoice!");

            _items.Add(new InvoiceItem(productId: productId,
                quantity: quantity,
                unitPrice: unitPrice));
        }
        public void Cancel()
        {
            if (Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException($"Invoice is already cancelled!");

            Status = InvoiceStatus.Cancelled;
        }
        public InvoiceItem RemoveItem(Guid productId)
        {
            if (Status != InvoiceStatus.Completed)
                throw new InvalidOperationException($"Invoice is {Status}, can't remove item from invoice!");

            var item = _items.FirstOrDefault(x => x.ProductId == productId);

            if(item == null)
                throw new InvalidOperationException($"Product {productId} not found in invoice");

            _items.Remove(item);

            return item;
        }
    }
   
    
}
