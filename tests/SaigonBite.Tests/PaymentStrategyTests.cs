using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Payments;
using SaigonBite.Domain;

namespace SaigonBite.Tests
{
    [TestClass]
    public class PaymentStrategyTests
    {
        private readonly PaymentSelector _selector = PaymentSelector.CreateDefault();

        [TestMethod]
        public void Select_LocalCustomer_AllowsMoMoVNPayAndCod()
        {
            foreach (PaymentMethod method in new[] { PaymentMethod.MoMo, PaymentMethod.VNPay })
            {
                IPaymentStrategy strategy = _selector.Select(CustomerType.Local, method, 205000m);
                PaymentResult result = strategy.Charge(new PaymentRequest
                {
                    OrderCode = "SB-LOC-1",
                    Amount = 205000m,
                    CustomerType = CustomerType.Local
                });

                Assert.IsTrue(result.Success);
                Assert.IsTrue(strategy.IsOnline);
                Assert.AreEqual("Paid", result.Status);
                Assert.AreEqual(205000m, result.Amount);
            }

            IPaymentStrategy cod = _selector.Select(CustomerType.Local, PaymentMethod.Cod, 205000m);
            PaymentResult codResult = cod.Charge(new PaymentRequest
            {
                OrderCode = "SB-LOC-2",
                Amount = 205000m,
                CustomerType = CustomerType.Local
            });

            Assert.IsFalse(cod.IsOnline);
            Assert.AreEqual("PendingCash", codResult.Status);
        }

        [TestMethod]
        public void Select_TouristCustomer_AllowsApplePayPayPalAndCod()
        {
            foreach (PaymentMethod method in new[] { PaymentMethod.ApplePay, PaymentMethod.PayPal })
            {
                IPaymentStrategy strategy = _selector.Select(CustomerType.Tourist, method, 320000m);
                PaymentResult result = strategy.Charge(new PaymentRequest
                {
                    OrderCode = "SB-TUR-1",
                    Amount = 320000m,
                    CustomerType = CustomerType.Tourist
                });

                Assert.IsTrue(result.Success);
                Assert.IsTrue(strategy.IsOnline);
                Assert.AreEqual("Paid", result.Status);
            }

            IPaymentStrategy cod = _selector.Select(CustomerType.Tourist, PaymentMethod.Cod, 320000m);
            PaymentResult codResult = cod.Charge(new PaymentRequest
            {
                OrderCode = "SB-TUR-2",
                Amount = 320000m,
                CustomerType = CustomerType.Tourist
            });
            Assert.AreEqual("PendingCash", codResult.Status);
        }

        [TestMethod]
        public void Select_WrongCustomerGroup_ThrowsBR13()
        {
            foreach (PaymentMethod disallowedForLocal in new[] { PaymentMethod.ApplePay, PaymentMethod.PayPal })
            {
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    _selector.Select(CustomerType.Local, disallowedForLocal, 150000m));
                Assert.AreEqual("BR13", ex.Rule);
            }

            foreach (PaymentMethod disallowedForTourist in new[] { PaymentMethod.MoMo, PaymentMethod.VNPay })
            {
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    _selector.Select(CustomerType.Tourist, disallowedForTourist, 150000m));
                Assert.AreEqual("BR13", ex.Rule);
            }
        }

        [TestMethod]
        public void Cod_BoundaryAtOneMillion_AcceptsExactLimitAndRejectsAbove()
        {
            IPaymentStrategy exactStrategy = _selector.Select(CustomerType.Local, PaymentMethod.Cod, 1000000m);
            PaymentResult exactResult = exactStrategy.Charge(new PaymentRequest
            {
                OrderCode = "SB-COD-MAX",
                Amount = 1000000m,
                CustomerType = CustomerType.Local
            });
            Assert.AreEqual("PendingCash", exactResult.Status);

            var selectorEx = Assert.ThrowsException<BusinessRuleException>(() =>
                _selector.Select(CustomerType.Local, PaymentMethod.Cod, 1001000m));
            Assert.AreEqual("BR14", selectorEx.Rule);

            var directCod = new CodStrategy();
            var codEx = Assert.ThrowsException<BusinessRuleException>(() =>
                directCod.Charge(new PaymentRequest { OrderCode = "SB-COD-OVER", Amount = 1001000m }));
            Assert.AreEqual("BR14", codEx.Rule);
        }

        [TestMethod]
        public void Refund_OnlineReturnsFullAmount_CodReturnsZero()
        {
            foreach (PaymentMethod onlineMethod in new[]
            {
                PaymentMethod.MoMo, PaymentMethod.VNPay, PaymentMethod.ApplePay, PaymentMethod.PayPal
            })
            {
                IPaymentStrategy strategy = _selector.SelectForRefund(onlineMethod);
                PaymentResult refund = strategy.Refund(new PaymentRequest
                {
                    OrderCode = "SB-REF-1",
                    Amount = 205000m,
                    Reason = "Customer cancelled"
                });

                Assert.IsTrue(refund.Success);
                Assert.AreEqual("Refunded", refund.Status);
                Assert.AreEqual(205000m, refund.Amount);
            }

            IPaymentStrategy cod = _selector.SelectForRefund(PaymentMethod.Cod);
            PaymentResult codRefund = cod.Refund(new PaymentRequest
            {
                OrderCode = "SB-REF-2",
                Amount = 205000m,
                Reason = "Customer cancelled"
            });

            Assert.AreEqual("NoRefundNeeded", codRefund.Status);
            Assert.AreEqual(0m, codRefund.Amount);
        }

        [TestMethod]
        public void PaymentSelector_InjectedCustomStrategy_ResolvesWithoutEditingExistingAdapters()
        {
            var custom = new StubCustomMoMoStrategy();
            var customSelector = new PaymentSelector(new IPaymentStrategy[] { custom });

            IPaymentStrategy resolved = customSelector.Select(CustomerType.Local, PaymentMethod.MoMo, 100000m);
            PaymentResult result = resolved.Charge(new PaymentRequest
            {
                OrderCode = "SB-OCP",
                Amount = 100000m,
                CustomerType = CustomerType.Local
            });

            Assert.AreSame(custom, resolved);
            Assert.AreEqual("STUB-OCP", result.CorrelationKey);
        }

        private sealed class StubCustomMoMoStrategy : IPaymentStrategy
        {
            public PaymentMethod Method { get { return PaymentMethod.MoMo; } }
            public bool IsOnline { get { return true; } }

            public PaymentResult Charge(PaymentRequest request)
            {
                return new PaymentResult
                {
                    Success = true,
                    Method = Method,
                    Operation = "Charge",
                    Amount = request.Amount,
                    Status = "Paid",
                    CorrelationKey = "STUB-OCP",
                    ProcessedAt = DateTimeOffset.UtcNow
                };
            }

            public PaymentResult Refund(PaymentRequest request)
            {
                return new PaymentResult
                {
                    Success = true,
                    Method = Method,
                    Operation = "Refund",
                    Amount = request.Amount,
                    Status = "Refunded",
                    CorrelationKey = "STUB-OCP",
                    ProcessedAt = DateTimeOffset.UtcNow
                };
            }
        }
    }
}
