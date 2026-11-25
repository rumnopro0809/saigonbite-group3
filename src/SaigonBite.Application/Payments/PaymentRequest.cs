using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    public sealed class PaymentRequest
    {
        public string OrderCode { get; set; }
        public decimal Amount { get; set; }
        public CustomerType CustomerType { get; set; }
        public string CorrelationKey { get; set; }
        public string Reason { get; set; }
    }
}
