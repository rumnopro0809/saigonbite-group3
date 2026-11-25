using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    public sealed class ApplePayStrategy : SimulatedOnlinePaymentStrategy
    {
        public override PaymentMethod Method
        {
            get { return PaymentMethod.ApplePay; }
        }

        protected override string GatewayName
        {
            get { return "Apple Pay"; }
        }
    }
}
