using System;
using System.Collections.Generic;
using SaigonBite.Domain;

namespace SaigonBite.Application.Orders
{
    public sealed class EditOrderQuoteResult
    {
        public EditOrderQuoteResult()
        {
            ResolvedItems = new List<ResolvedOrderLineDto>();
        }

        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public decimal OriginalPayableTotal { get; set; }
        public decimal NewPayableTotal { get; set; }
        public decimal Difference { get; set; }
        public DateTimeOffset OriginalPlacedAt { get; set; }
        public decimal OriginalHubLoad { get; set; }
        public bool OriginalHeavyRain { get; set; }
        public bool HasOnlineAdjustment { get; set; }
        public string AdjustmentType { get; set; }
        public PriceBreakdown Breakdown { get; set; }
        public List<ResolvedOrderLineDto> ResolvedItems { get; set; }
    }
}
