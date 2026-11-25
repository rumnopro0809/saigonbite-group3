using System;
using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    /// <summary>
    /// Shared simulated behavior for online payment gateways (MoMo, VNPay, Apple Pay, PayPal).
    /// </summary>
    public abstract class SimulatedOnlinePaymentStrategy : IPaymentStrategy
    {
        public abstract PaymentMethod Method { get; }
        protected abstract string GatewayName { get; }

        public bool IsOnline
        {
            get { return true; }
        }

        public PaymentResult Charge(PaymentRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.Amount <= 0m)
                throw new BusinessRuleException("PAYMENT", "Payment amount must be positive.");

            return new PaymentResult
            {
                Success = true,
                Method = Method,
                Operation = "Charge",
                Amount = request.Amount,
                Status = "Paid",
                Message = "Simulated " + GatewayName + " charge approved.",
                CorrelationKey = BuildCorrelationKey(request.CorrelationKey),
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }

        public PaymentResult Refund(PaymentRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");
            if (request.Amount < 0m)
                throw new BusinessRuleException("PAYMENT", "Refund amount cannot be negative.");

            return new PaymentResult
            {
                Success = true,
                Method = Method,
                Operation = "Refund",
                Amount = request.Amount,
                Status = "Refunded",
                Message = "Simulated full refund processed via " + GatewayName + ".",
                CorrelationKey = BuildCorrelationKey(request.CorrelationKey),
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }

        private string BuildCorrelationKey(string provided)
        {
            if (!string.IsNullOrWhiteSpace(provided)) return provided;
            return Method.ToString().ToUpperInvariant() + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
