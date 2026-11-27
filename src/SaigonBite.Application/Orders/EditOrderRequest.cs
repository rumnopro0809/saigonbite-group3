using System.Collections.Generic;

namespace SaigonBite.Application.Orders
{
    public sealed class EditOrderRequest
    {
        public EditOrderRequest()
        {
            KeepVoucher = true;
            Items = new List<OrderItemRequest>();
        }

        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public byte[] ExpectedRowVersion { get; set; }
        public bool KeepVoucher { get; set; }
        public string Notes { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }
}
