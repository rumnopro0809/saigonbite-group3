using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    public sealed class MoMoStrategy : SimulatedOnlinePaymentStrategy
    {
        public override PaymentMethod Method
        {
            get { return PaymentMethod.MoMo; }
        }

        protected override string GatewayName
        {
            get { return "MoMo"; }
        }
    }
}
