using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    /// <summary>
    /// Strategy contract for simulated payment charging and full cancellation refunds.
    /// </summary>
    public interface IPaymentStrategy
    {
        PaymentMethod Method { get; }
        bool IsOnline { get; }
        PaymentResult Charge(PaymentRequest request);
        PaymentResult Refund(PaymentRequest request);
    }
}
