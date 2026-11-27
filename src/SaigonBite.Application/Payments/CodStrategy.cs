using System;
using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    /// <summary>
    /// Cash on delivery strategy enforcing BR14 cap and requiring no online refund on cancellation.
    /// </summary>
    public sealed class CodStrategy : IPaymentStrategy
    {
        public PaymentMethod Method
        {
            get { return PaymentMethod.Cod; }
        }

        public bool IsOnline
        {
            get { return false; }
        }

        public PaymentResult Charge(PaymentRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.Amount <= 0m)
                throw new BusinessRuleException("PAYMENT", "Payment amount must be positive.");
            if (request.Amount > 1000000m)
                throw new BusinessRuleException("BR14", "COD exceeds VND 1,000,000.");

            string key = string.IsNullOrWhiteSpace(request.CorrelationKey)
                ? "COD-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                : request.CorrelationKey;

            return new PaymentResult
            {
                Success = true,
                Method = Method,
                Operation = "Charge",
                Amount = request.Amount,
                Status = "PendingCash",
                Message = "Cash on delivery pending shipper collection.",
                CorrelationKey = key,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }

        public PaymentResult Refund(PaymentRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            string key = string.IsNullOrWhiteSpace(request.CorrelationKey)
                ? "COD-REF-" + Guid.NewGuid().ToString("N").Substring(0, 8)
                : request.CorrelationKey;

            return new PaymentResult
            {
                Success = true,
                Method = Method,
                Operation = "Refund",
                Amount = 0m,
                Status = "NoRefundNeeded",
                Message = "COD order was not charged online; no refund needed.",
                CorrelationKey = key,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
