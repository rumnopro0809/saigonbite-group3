using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    public sealed class PayPalStrategy : SimulatedOnlinePaymentStrategy
    {
        public override PaymentMethod Method
        {
            get { return PaymentMethod.PayPal; }
        }

        protected override string GatewayName
        {
            get { return "PayPal"; }
        }
    }
}
