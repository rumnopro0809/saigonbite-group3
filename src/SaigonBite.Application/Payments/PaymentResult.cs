using System;
using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    public sealed class PaymentResult
    {
        public bool Success { get; set; }
        public PaymentMethod Method { get; set; }
        public string Operation { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string CorrelationKey { get; set; }
        public DateTimeOffset ProcessedAt { get; set; }
    }
}
