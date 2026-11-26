using System;
using System.Collections.Generic;
using SaigonBite.Domain;

namespace SaigonBite.Application.Orders
{
    public sealed class CreateOrderRequest
    {
        public CreateOrderRequest()
        {
            Items = new List<OrderItemRequest>();
        }

        public int CustomerId { get; set; }
        public int HubId { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal DistanceKm { get; set; }
        public string VoucherCode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string RequestKey { get; set; }
        public string Notes { get; set; }
        public decimal? ExpectedPayableTotal { get; set; }
        public DateTimeOffset? PlacedAtOverride { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }
}
