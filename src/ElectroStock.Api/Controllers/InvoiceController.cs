using ElectroStock.API.Helpers;
using ElectroStock.Application.Dtos;
using ElectroStock.Application.Helpers;
using ElectroStock.Application.Interfaces;
using ElectroStock.Application.Services;
using ElectroStock.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElectroStock.API.Controllers
{
    /// <summary>
    /// Handles invoice-related operations including creation, retrieval, and management.
    /// </summary>
    /// <remarks>
    /// This controller provides endpoints for cashiers and administrators to manage invoices.
    /// All endpoints require authentication with Cashier or Admin role.
    /// </remarks>
    /// 
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles= "Cashier,Admin")]
    [ApiConventionType(typeof(DefaultApiConventions))]

    public class InvoicesController:ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }
      
        /// <summary>
        /// Creates a new invoice.
        /// </summary>
        /// <remarks>
        /// Creates a new completed invoice with the specified items.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/invoices
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        /// 
        /// {
        ///   "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///   "orderSource": "InStore",
        ///   "paymentMethod": "Cash",
        ///   "items": [
        ///     {
        ///       "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///       "quantity": 2,
        ///       "serialNumber": null
        ///     }
        ///   ]
        /// }
        /// </remarks>
        /// <param name="invoiceDto">The invoice creation details including customer, items, and payment method</param>
        /// <returns>The newly created invoice with calculated totals and status</returns>
        [HttpPost]
        public async Task<ActionResult<InvoiceDto>>CreateInvoiceAsync([FromBody]CreateInvoiceDto invoiceDto)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);

            var invoice = await  _invoiceService.CreateInvoiceAsync(invoiceDto, performedBy);
                return Ok(invoice);
        }

        /// <summary>
        /// Retrieves an invoice by its invoice number.
        /// </summary>
        /// <remarks>
        /// Gets detailed information about a specific invoice including all items and totals.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/invoices/INV-20250724-0001
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="invoiceNumber">The unique invoice number (e.g., INV-20250724-0001)</param>
        /// <returns>Detailed invoice information including items, totals, and status</returns>
        [HttpGet("{invoiceNumber}")]
        public async Task<ActionResult<InvoiceDto>>GetInvoiceByNumberAsync([FromRoute]string invoiceNumber)
        {
                var invoice = await _invoiceService.GetInvoiceByNumberAsync(invoiceNumber);
                return Ok(invoice);
        }

        /// <summary>
        /// Retrieves all invoices for a specific customer.
        /// </summary>
        /// <remarks>
        /// Gets a list of all invoices belonging to a customer, ordered by creation date.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// GET /api/invoices/customer/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="customerId">The unique identifier (GUID) of the customer</param>
        /// <returns>List of invoices associated with the customer</returns>
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>>GetInvoicesByCustomerAsync([FromRoute]Guid customerId)
        {
                var customerInvoices = await _invoiceService.GetInvoicesByCustomerAsync(customerId);
                return Ok(customerInvoices);
        }

        /// <summary>
        /// Removes an item from a pending invoice.
        /// </summary>
        /// <remarks>
        /// Removes a specific product from the invoice and refunds the sold stock back to inventory.
        /// Only pending invoices can be modified. 
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// PATCH /api/invoices/INV-20250724-0001/items/3fa85f64-5717-4562-b3fc-2c963f66afa6
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="invoiceNumber">The invoice number of the pending invoice</param>
        /// <param name="productId">The unique identifier of the product to remove</param>
        /// <returns>The updated invoice with recalculated totals</returns>
        [HttpPatch("{invoiceNumber}/items/{productId}")]
        public async Task<ActionResult<InvoiceDto>> RemoveItemFromInvoiceAsync([FromRoute]string invoiceNumber,[FromRoute]Guid productId)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            var invoiceDto = await _invoiceService.RemoveItemFromInvoiceAsync(invoiceNumber, productId, performedBy);
                return Ok(invoiceDto);
        }

        /// <summary>
        /// Cancels a completed invoice.
        /// </summary>
        /// <remarks>
        /// Cancels a complete invoice, refunding all sold stock back to inventory.
        /// Cashiers and Admins are authorized to perform this action.
        /// 
        /// Sample request:
        /// POST /api/invoices/INV-20250724-0001/cancel
        /// Authorization: Bearer {token}
        /// </remarks>
        /// <param name="invoiceNumber">The invoice number of the completed invoice to cancel</param>
        /// <returns>Success message confirming the cancellation</returns>
        [HttpPost("{invoiceNumber}/cancel")]
        public async Task<IActionResult> CancelInvoiceAsync([FromRoute] string invoiceNumber)
        {
            var performedBy = CurrentUserHelper.GetCurrentUserId(User);
            await _invoiceService.CancelInvoiceAsync(invoiceNumber,performedBy);
                return Ok(new { 
                    message=$"Cancelled invoice with number {invoiceNumber} successfully"
                });
        }

    }
}
