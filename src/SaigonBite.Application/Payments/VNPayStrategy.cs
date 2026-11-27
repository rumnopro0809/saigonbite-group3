using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    public sealed class VNPayStrategy : SimulatedOnlinePaymentStrategy
    {
        public override PaymentMethod Method
        {
            get { return PaymentMethod.VNPay; }
        }

        protected override string GatewayName
        {
            get { return "VNPay"; }
        }
    }
}
