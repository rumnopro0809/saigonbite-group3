using System.Collections.Generic;
using SaigonBite.Domain;

namespace SaigonBite.Application.Orders
{
    public sealed class ResolvedOrderLineDto
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }

    public sealed class OrderQuoteResult
    {
        public OrderQuoteResult()
        {
            ResolvedItems = new List<ResolvedOrderLineDto>();
        }

        public int HubId { get; set; }
        public string HubName { get; set; }
        public decimal HubLoad { get; set; }
        public bool IsHeavyRainOn { get; set; }
        public int? VoucherId { get; set; }
        public string VoucherCode { get; set; }
        public CustomerType CustomerType { get; set; }
        public PriceBreakdown Breakdown { get; set; }
        public List<ResolvedOrderLineDto> ResolvedItems { get; set; }
    }
}
