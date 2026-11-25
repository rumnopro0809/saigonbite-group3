using System;
using System.Collections.Generic;
using SaigonBite.Domain;

namespace SaigonBite.Application.Payments
{
    /// <summary>
    /// Validates customer-group eligibility (BR13-BR14) and selects a registered strategy from a dictionary.
    /// </summary>
    public sealed class PaymentSelector
    {
        private readonly Dictionary<PaymentMethod, IPaymentStrategy> _strategies;

        public PaymentSelector(IEnumerable<IPaymentStrategy> strategies)
        {
            if (strategies == null) throw new ArgumentNullException("strategies");

            _strategies = new Dictionary<PaymentMethod, IPaymentStrategy>();
            foreach (IPaymentStrategy strategy in strategies)
            {
                if (strategy != null)
                {
                    _strategies[strategy.Method] = strategy;
                }
            }
        }

        public static PaymentSelector CreateDefault()
        {
            return new PaymentSelector(new IPaymentStrategy[]
            {
                new MoMoStrategy(),
                new VNPayStrategy(),
                new ApplePayStrategy(),
                new PayPalStrategy(),
                new CodStrategy()
            });
        }

        public IPaymentStrategy Select(CustomerType customerType, PaymentMethod method, decimal payableTotal)
        {
            PricingService.ValidatePayment(customerType, method, payableTotal);

            IPaymentStrategy strategy;
            if (!_strategies.TryGetValue(method, out strategy))
            {
                throw new BusinessRuleException("BR13", "No payment strategy registered for method " + method + ".");
            }
            return strategy;
        }

        public IPaymentStrategy SelectForRefund(PaymentMethod method)
        {
            IPaymentStrategy strategy;
            if (!_strategies.TryGetValue(method, out strategy))
            {
                throw new BusinessRuleException("BR13", "No refund strategy registered for method " + method + ".");
            }
            return strategy;
        }
    }
}
