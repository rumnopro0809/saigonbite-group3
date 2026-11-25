using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Orders;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Migrations;
using SaigonBite.Infrastructure.Services;

namespace SaigonBite.Tests
{
    /// <summary>HT03, FR17, BR14-BR16, AMB01: gán shipper và giao hàng.</summary>
    [TestClass]
    public class ShipperServiceTests
    {
        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());
            using (var db = new AppDbContext()) db.Database.Initialize(force: false);
        }

        private static void AssertRule(string rule, Action action)
        {
            var ex = Assert.ThrowsException<BusinessRuleException>(action);
            Assert.AreEqual(rule, ex.Rule);
        }

        // ---------- ListShippers ----------

        [TestMethod]
        public void ListShippers_ReturnsOnlyActiveNotDeletedShippers()
        {
            using (var f = new Fixture())
            {
                IList<ShipperInfo> list = new ShipperService(f.Db).ListShippers();
                int[] ids = list.Select(s => s.UserId).ToArray();

                CollectionAssert.Contains(ids, f.Shipper.Id);
                CollectionAssert.Contains(ids, f.OtherShipper.Id);
                CollectionAssert.DoesNotContain(ids, f.InactiveShipper.Id);
                CollectionAssert.DoesNotContain(ids, f.DeletedShipper.Id);
                CollectionAssert.DoesNotContain(ids, f.Kitchen.Id);
                CollectionAssert.DoesNotContain(ids, f.Admin.Id);
                CollectionAssert.DoesNotContain(ids, f.Customer.Id);

                ShipperInfo info = list.Single(s => s.UserId == f.Shipper.Id);
                Assert.AreEqual(f.Shipper.Identifier, info.Identifier);
                Assert.AreEqual(f.Shipper.FullName, info.FullName);
            }
        }

        // ---------- AssignShipper ----------

        [TestMethod]
        public void AssignShipper_ReadyOrder_SetsShipperIdAndKeepsStatus()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready);

                Order result = new ShipperService(f.Db).AssignShipper(f.Admin.Id, order.Id, f.Shipper.Id, f.RowVersion(order.Id));

                Assert.AreEqual(f.Shipper.Id, result.ShipperId);
                f.AssertState(order.Id, OrderStatus.Ready, f.Shipper.Id);
            }
        }

        [TestMethod]
        public void AssignShipper_AlreadyAssignedReadyOrder_CanBeReassigned()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready, shipperId: f.Shipper.Id);

                new ShipperService(f.Db).AssignShipper(f.Admin.Id, order.Id, f.OtherShipper.Id, f.RowVersion(order.Id));

                f.AssertState(order.Id, OrderStatus.Ready, f.OtherShipper.Id);
            }
        }

        [TestMethod]
        public void AssignShipper_NonReadyOrders_ThrowBr15AndKeepShipperUnchanged()
        {
            using (var f = new Fixture())
            {
                var service = new ShipperService(f.Db);
                foreach (OrderStatus status in new[]
                {
                    OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Preparing,
                    OrderStatus.OutForDelivery, OrderStatus.Delivered
                })
                {
                    Order order = f.CreateOrder(status, shipperId: f.Shipper.Id);
                    int orderId = order.Id;
                    AssertRule("BR15", () => service.AssignShipper(f.Admin.Id, orderId, f.OtherShipper.Id, null));
                    f.AssertState(orderId, status, f.Shipper.Id);
                }

                Order unassigned = f.CreateOrder(OrderStatus.Preparing);
                int unassignedId = unassigned.Id;
                AssertRule("BR15", () => service.AssignShipper(f.Admin.Id, unassignedId, f.Shipper.Id, null));
                f.AssertState(unassignedId, OrderStatus.Preparing, null);
            }
        }

        [TestMethod]
        public void AssignShipper_UnknownOrder_ThrowsNotFound()
        {
            using (var f = new Fixture())
                AssertRule("NOT_FOUND", () => new ShipperService(f.Db).AssignShipper(f.Admin.Id, int.MaxValue, f.Shipper.Id, null));
        }

        [TestMethod]
        public void AssignShipper_NonAdminCallers_ThrowUnauthorizedAndKeepShipperUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready);
                var service = new ShipperService(f.Db);
                foreach (int callerId in new[] { f.Customer.Id, f.Kitchen.Id, f.Shipper.Id, f.InactiveAdmin.Id, int.MaxValue })
                {
                    int id = callerId;
                    AssertRule("UNAUTHORIZED", () => service.AssignShipper(id, order.Id, f.Shipper.Id, null));
                }
                f.AssertState(order.Id, OrderStatus.Ready, null);
            }
        }

        [TestMethod]
        public void AssignShipper_TargetNotActiveShipper_ThrowsShipperNotFound()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready);
                var service = new ShipperService(f.Db);
                foreach (int targetId in new[]
                {
                    f.Customer.Id, f.Kitchen.Id, f.Admin.Id, f.InactiveShipper.Id, f.DeletedShipper.Id, int.MaxValue
                })
                {
                    int id = targetId;
                    AssertRule("SHIPPER_NOT_FOUND", () => service.AssignShipper(f.Admin.Id, order.Id, id, null));
                }
                f.AssertState(order.Id, OrderStatus.Ready, null);
            }
        }

        [TestMethod]
        public void AssignShipper_StaleRowVersion_ThrowsConcurrencyAndKeepsFirstAssignment()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready);
                var service = new ShipperService(f.Db);
                byte[] stale = f.RowVersion(order.Id);

                service.AssignShipper(f.Admin.Id, order.Id, f.Shipper.Id, stale);

                AssertRule("CONCURRENCY", () => service.AssignShipper(f.Admin.Id, order.Id, f.OtherShipper.Id, stale));
                f.AssertState(order.Id, OrderStatus.Ready, f.Shipper.Id);
            }
        }

        // ---------- ReadyOrders ----------

        [TestMethod]
        public void ReadyOrders_ReturnsOnlyReadyOrdersOldestFirstWithShipperName()
        {
            using (var f = new Fixture())
            {
                DateTimeOffset t0 = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.FromHours(7));
                Order assigned = f.CreateOrder(OrderStatus.Ready, placedAt: t0.AddMinutes(10), shipperId: f.Shipper.Id);
                Order unassigned = f.CreateOrder(OrderStatus.Ready, placedAt: t0, method: PaymentMethod.Cod, paymentStatus: "Pending");
                Order notReady = f.CreateOrder(OrderStatus.Preparing, placedAt: t0.AddMinutes(1));
                Order outForDelivery = f.CreateOrder(OrderStatus.OutForDelivery, placedAt: t0.AddMinutes(2), shipperId: f.Shipper.Id);
                Order delivered = f.CreateOrder(OrderStatus.Delivered, placedAt: t0.AddMinutes(3), shipperId: f.Shipper.Id);

                IList<DispatchOrderCard> cards = new ShipperService(f.Db).ReadyOrders(f.Admin.Id);
                int[] mine = { assigned.Id, unassigned.Id, notReady.Id, outForDelivery.Id, delivered.Id };
                int[] ownIds = cards.Select(c => c.OrderId).Where(id => mine.Contains(id)).ToArray();

                CollectionAssert.AreEqual(new[] { unassigned.Id, assigned.Id }, ownIds);

                DispatchOrderCard assignedCard = cards.Single(c => c.OrderId == assigned.Id);
                Assert.AreEqual(f.Shipper.Id, assignedCard.ShipperId);
                Assert.AreEqual(f.Shipper.FullName, assignedCard.ShipperName);
                Assert.AreEqual(assigned.OrderCode, assignedCard.OrderCode);
                Assert.AreEqual(f.Hub.Name, assignedCard.HubName);
                Assert.AreEqual(assigned.DeliveryAddress, assignedCard.DeliveryAddress);
                Assert.AreEqual(PaymentMethod.MoMo, assignedCard.PaymentMethod);
                Assert.AreEqual(assigned.PayableTotal, assignedCard.PayableTotal);
                Assert.IsFalse(string.IsNullOrEmpty(assignedCard.RowVersion));

                DispatchOrderCard unassignedCard = cards.Single(c => c.OrderId == unassigned.Id);
                Assert.IsNull(unassignedCard.ShipperId);
                Assert.IsNull(unassignedCard.ShipperName);
                Assert.AreEqual(PaymentMethod.Cod, unassignedCard.PaymentMethod);
            }
        }

        [TestMethod]
        public void ReadyOrders_NonAdminCallers_ThrowUnauthorized()
        {
            using (var f = new Fixture())
            {
                var service = new ShipperService(f.Db);
                foreach (int callerId in new[] { f.Customer.Id, f.Kitchen.Id, f.Shipper.Id, f.InactiveAdmin.Id, int.MaxValue })
                {
                    int id = callerId;
                    AssertRule("UNAUTHORIZED", () => service.ReadyOrders(id));
                }
            }
        }

        // ---------- GetMyOrders ----------

        [TestMethod]
        public void GetMyOrders_ReturnsOnlyOwnReadyAndOutForDeliveryOrdersWithLines()
        {
            using (var f = new Fixture())
            {
                DateTimeOffset t0 = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.FromHours(7));
                Order outForDelivery = f.CreateOrder(OrderStatus.OutForDelivery, placedAt: t0.AddMinutes(5), shipperId: f.Shipper.Id);
                Order ready = f.CreateOrder(OrderStatus.Ready, placedAt: t0, shipperId: f.Shipper.Id,
                    method: PaymentMethod.Cod, paymentStatus: "Pending");
                Order foreign = f.CreateOrder(OrderStatus.Ready, placedAt: t0.AddMinutes(1), shipperId: f.OtherShipper.Id);
                Order delivered = f.CreateOrder(OrderStatus.Delivered, placedAt: t0.AddMinutes(2), shipperId: f.Shipper.Id);
                Order failed = f.CreateOrder(OrderStatus.Failed, placedAt: t0.AddMinutes(3), shipperId: f.Shipper.Id);
                Order unassigned = f.CreateOrder(OrderStatus.Ready, placedAt: t0.AddMinutes(4));
                Order preparing = f.CreateOrder(OrderStatus.Preparing, placedAt: t0.AddMinutes(6), shipperId: f.Shipper.Id);

                IList<ShipperOrderCard> cards = new ShipperService(f.Db).GetMyOrders(f.Shipper.Id);

                CollectionAssert.AreEqual(
                    new[] { ready.Id, outForDelivery.Id },
                    cards.Select(c => c.OrderId).ToArray());
                foreach (Order other in new[] { foreign, delivered, failed, unassigned, preparing })
                    Assert.IsFalse(cards.Any(c => c.OrderId == other.Id));

                ShipperOrderCard card = cards.Single(c => c.OrderId == ready.Id);
                Assert.AreEqual(OrderStatus.Ready, card.Status);
                Assert.AreEqual(ready.OrderCode, card.OrderCode);
                Assert.AreEqual(f.Hub.Name, card.HubName);
                Assert.AreEqual(ready.DeliveryAddress, card.DeliveryAddress);
                Assert.AreEqual(PaymentMethod.Cod, card.PaymentMethod);
                Assert.AreEqual("Pending", card.PaymentStatus);
                Assert.AreEqual(ready.PayableTotal, card.PayableTotal);
                Assert.IsFalse(string.IsNullOrEmpty(card.RowVersion));
                Assert.AreEqual(1, card.Lines.Count);
                Assert.AreEqual(2, card.Lines[0].Quantity);
                Assert.AreEqual(f.ItemName, card.Lines[0].Name);
            }
        }

        [TestMethod]
        public void GetMyOrders_NonShipperCallers_ThrowUnauthorized()
        {
            using (var f = new Fixture())
            {
                var service = new ShipperService(f.Db);
                foreach (int callerId in new[]
                {
                    f.Customer.Id, f.Kitchen.Id, f.Admin.Id, f.InactiveShipper.Id, f.DeletedShipper.Id, int.MaxValue
                })
                {
                    int id = callerId;
                    AssertRule("UNAUTHORIZED", () => service.GetMyOrders(id));
                }
            }
        }

        // ---------- Act: authorization ----------

        [TestMethod]
        public void Act_NonShipperCallers_ThrowUnauthorizedAndLeaveOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready, shipperId: f.Shipper.Id);
                var service = new ShipperService(f.Db);
                foreach (int callerId in new[]
                {
                    f.Customer.Id, f.Kitchen.Id, f.Admin.Id, f.InactiveShipper.Id, f.DeletedShipper.Id, int.MaxValue
                })
                {
                    int id = callerId;
                    AssertRule("UNAUTHORIZED", () => service.Act(id, order.Id, ShipperAction.PickUp, false, null, null));
                }
                f.AssertState(order.Id, OrderStatus.Ready, f.Shipper.Id);
                Assert.AreEqual(0, f.HistoryCount(order.Id));
            }
        }

        // ---------- Act: PickUp ----------

        [TestMethod]
        public void Act_PickUpAssignedReadyOrder_MovesToOutForDeliveryAndWritesHistory()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready, shipperId: f.Shipper.Id);
                DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(-1);

                Order result = new ShipperService(f.Db).Act(f.Shipper.Id, order.Id, ShipperAction.PickUp, false, null, f.RowVersion(order.Id));

                Assert.AreEqual(OrderStatus.OutForDelivery, result.Status);
                f.AssertState(order.Id, OrderStatus.OutForDelivery, f.Shipper.Id);
                OrderStatusHistory h = f.History(order.Id).Single();
                Assert.AreEqual(f.Shipper.Id, h.ActorId);
                Assert.AreEqual(OrderStatus.Ready, h.From);
                Assert.AreEqual(OrderStatus.OutForDelivery, h.To);
                Assert.AreEqual(OrderStatus.Ready, h.FromStatus);
                Assert.AreEqual(OrderStatus.OutForDelivery, h.ToStatus);
                Assert.IsFalse(h.CashCollected);
                Assert.IsTrue(h.ChangedAt >= before);
                Assert.IsTrue(h.ChangedAt <= DateTimeOffset.UtcNow.AddMinutes(1));
            }
        }

        [TestMethod]
        public void Act_PickUpUnassignedOrAnotherShippersOrder_ThrowsBr15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order unassigned = f.CreateOrder(OrderStatus.Ready);
                Order foreign = f.CreateOrder(OrderStatus.Ready, shipperId: f.OtherShipper.Id);
                var service = new ShipperService(f.Db);

                AssertRule("BR15", () => service.Act(f.Shipper.Id, unassigned.Id, ShipperAction.PickUp, false, null, null));
                AssertRule("BR15", () => service.Act(f.Shipper.Id, foreign.Id, ShipperAction.PickUp, false, null, null));

                f.AssertState(unassigned.Id, OrderStatus.Ready, null);
                f.AssertState(foreign.Id, OrderStatus.Ready, f.OtherShipper.Id);
                Assert.AreEqual(0, f.HistoryCount(unassigned.Id));
                Assert.AreEqual(0, f.HistoryCount(foreign.Id));
            }
        }

        [TestMethod]
        public void Act_PickUpNonReadyOrders_ThrowsBr15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                var service = new ShipperService(f.Db);
                foreach (OrderStatus status in new[]
                {
                    OrderStatus.Placed, OrderStatus.Confirmed, OrderStatus.Preparing,
                    OrderStatus.OutForDelivery, OrderStatus.Delivered, OrderStatus.Failed, OrderStatus.Cancelled
                })
                {
                    Order order = f.CreateOrder(status, shipperId: f.Shipper.Id);
                    int orderId = order.Id;
                    AssertRule("BR15", () => service.Act(f.Shipper.Id, orderId, ShipperAction.PickUp, false, null, null));
                    f.AssertState(orderId, status, f.Shipper.Id);
                    Assert.AreEqual(0, f.HistoryCount(orderId));
                }
            }
        }

        // ---------- Act: Deliver ----------

        [TestMethod]
        public void Act_DeliverCodWithoutCashCollected_ThrowsBr15AndRecordsNoPayment()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.OutForDelivery, method: PaymentMethod.Cod,
                    paymentStatus: "Pending", shipperId: f.Shipper.Id);

                AssertRule("BR15", () =>
                    new ShipperService(f.Db).Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, false, null, null));

                f.AssertState(order.Id, OrderStatus.OutForDelivery, f.Shipper.Id);
                Assert.AreEqual(0, f.HistoryCount(order.Id));
                Assert.AreEqual(0, f.CodCollectedCount(order.Id));
                Assert.AreEqual("Pending", f.Reload(order.Id).PaymentStatus);
            }
        }

        [TestMethod]
        public void Act_DeliverCodWithCashCollected_DeliversMarksPaidAndRecordsCodCollection()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.OutForDelivery, method: PaymentMethod.Cod,
                    paymentStatus: "Pending", shipperId: f.Shipper.Id);
                DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(-1);

                Order result = new ShipperService(f.Db).Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, true, null, f.RowVersion(order.Id));

                Assert.AreEqual(OrderStatus.Delivered, result.Status);
                Order stored = f.Reload(order.Id);
                Assert.AreEqual(OrderStatus.Delivered, stored.Status);
                Assert.AreEqual("Paid", stored.PaymentStatus);
                PaymentRecord payment = f.Payments(order.Id).Single(p => p.Operation == "CodCollected");
                Assert.AreEqual(PaymentMethod.Cod, payment.Method);
                Assert.AreEqual(order.PayableTotal, payment.Amount);
                Assert.AreEqual("Paid", payment.Result);

                OrderStatusHistory h = f.History(order.Id).Single();
                Assert.AreEqual(f.Shipper.Id, h.ActorId);
                Assert.AreEqual(OrderStatus.OutForDelivery, h.From);
                Assert.AreEqual(OrderStatus.Delivered, h.To);
                Assert.IsTrue(h.CashCollected);
                Assert.IsTrue(h.ChangedAt >= before);
            }
        }

        [TestMethod]
        public void Act_DeliverOnlinePaidOrderWithoutCashCollected_Delivers()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.OutForDelivery, method: PaymentMethod.MoMo,
                    paymentStatus: "Paid", shipperId: f.Shipper.Id);

                Order result = new ShipperService(f.Db).Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, false, null, null);

                Assert.AreEqual(OrderStatus.Delivered, result.Status);
                Order stored = f.Reload(order.Id);
                Assert.AreEqual(OrderStatus.Delivered, stored.Status);
                Assert.AreEqual("Paid", stored.PaymentStatus);
                Assert.AreEqual(0, f.CodCollectedCount(order.Id));
                Assert.IsFalse(f.History(order.Id).Single().CashCollected);
            }
        }

        [TestMethod]
        public void Act_DeliverAnotherShippersOrder_ThrowsBr15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.OutForDelivery, shipperId: f.OtherShipper.Id);

                AssertRule("BR15", () =>
                    new ShipperService(f.Db).Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, true, null, null));

                f.AssertState(order.Id, OrderStatus.OutForDelivery, f.OtherShipper.Id);
                Assert.AreEqual(0, f.HistoryCount(order.Id));
            }
        }

        // ---------- Act: Fail ----------

        [TestMethod]
        public void Act_FailWithNullOrBlankReason_ThrowsBr15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.OutForDelivery, shipperId: f.Shipper.Id);
                var service = new ShipperService(f.Db);

                foreach (string reason in new string[] { null, "", "   ", "\t \r\n" })
                {
                    string r = reason;
                    AssertRule("BR15", () => service.Act(f.Shipper.Id, order.Id, ShipperAction.Fail, false, r, null));
                }

                f.AssertState(order.Id, OrderStatus.OutForDelivery, f.Shipper.Id);
                Assert.AreEqual(0, f.HistoryCount(order.Id));
            }
        }

        [TestMethod]
        public void Act_FailWithReason_MarksFailedAndStoresTrimmedReasonInHistory()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.OutForDelivery, method: PaymentMethod.Cod,
                    paymentStatus: "Pending", shipperId: f.Shipper.Id);
                DateTimeOffset before = DateTimeOffset.UtcNow.AddMinutes(-1);

                Order result = new ShipperService(f.Db).Act(f.Shipper.Id, order.Id, ShipperAction.Fail, true,
                    "   Customer unreachable at gate  ", f.RowVersion(order.Id));

                Assert.AreEqual(OrderStatus.Failed, result.Status);
                f.AssertState(order.Id, OrderStatus.Failed, f.Shipper.Id);
                Assert.AreEqual("Pending", f.Reload(order.Id).PaymentStatus);
                Assert.AreEqual(0, f.CodCollectedCount(order.Id));

                OrderStatusHistory h = f.History(order.Id).Single();
                Assert.AreEqual(f.Shipper.Id, h.ActorId);
                Assert.AreEqual(OrderStatus.OutForDelivery, h.From);
                Assert.AreEqual(OrderStatus.Failed, h.To);
                Assert.AreEqual("Customer unreachable at gate", h.Reason);
                Assert.IsFalse(h.CashCollected);
                Assert.IsTrue(h.ChangedAt >= before);
            }
        }

        // ---------- Act: invalid states ----------

        [TestMethod]
        public void Act_DeliverOrFailOnReadyOrderNotPickedUp_ThrowsBr15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready, method: PaymentMethod.Cod,
                    paymentStatus: "Pending", shipperId: f.Shipper.Id);
                var service = new ShipperService(f.Db);

                AssertRule("BR15", () => service.Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, true, null, null));
                AssertRule("BR15", () => service.Act(f.Shipper.Id, order.Id, ShipperAction.Fail, false, "No answer", null));

                f.AssertState(order.Id, OrderStatus.Ready, f.Shipper.Id);
                Assert.AreEqual(0, f.HistoryCount(order.Id));
                Assert.AreEqual(0, f.CodCollectedCount(order.Id));
            }
        }

        [TestMethod]
        public void Act_AnyActionOnDeliveredOrFailedOrder_ThrowsBr15AndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                var service = new ShipperService(f.Db);
                foreach (OrderStatus status in new[] { OrderStatus.Delivered, OrderStatus.Failed })
                {
                    Order order = f.CreateOrder(status, shipperId: f.Shipper.Id);
                    int orderId = order.Id;
                    AssertRule("BR15", () => service.Act(f.Shipper.Id, orderId, ShipperAction.PickUp, false, null, null));
                    AssertRule("BR15", () => service.Act(f.Shipper.Id, orderId, ShipperAction.Deliver, true, null, null));
                    AssertRule("BR15", () => service.Act(f.Shipper.Id, orderId, ShipperAction.Fail, false, "Too late", null));
                    f.AssertState(orderId, status, f.Shipper.Id);
                    Assert.AreEqual(0, f.HistoryCount(orderId));
                    Assert.AreEqual(0, f.CodCollectedCount(orderId));
                }
            }
        }

        [TestMethod]
        public void Act_StaleRowVersion_ThrowsConcurrencyAndLeavesOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready, shipperId: f.Shipper.Id);
                var service = new ShipperService(f.Db);
                byte[] stale = f.RowVersion(order.Id);

                service.Act(f.Shipper.Id, order.Id, ShipperAction.PickUp, false, null, stale);

                AssertRule("CONCURRENCY", () =>
                    service.Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, true, null, stale));
                f.AssertState(order.Id, OrderStatus.OutForDelivery, f.Shipper.Id);
                Assert.AreEqual(1, f.HistoryCount(order.Id));
            }
        }

        [TestMethod]
        public void Act_UnknownOrder_ThrowsNotFound()
        {
            using (var f = new Fixture())
                AssertRule("NOT_FOUND", () =>
                    new ShipperService(f.Db).Act(f.Shipper.Id, int.MaxValue, ShipperAction.PickUp, false, null, null));
        }

        // ---------- End to end ----------

        [TestMethod]
        public void FullDispatchFlow_AssignPickUpDeliver_ReachesDeliveredWithTwoHistoryRows()
        {
            using (var f = new Fixture())
            {
                Order order = f.CreateOrder(OrderStatus.Ready, method: PaymentMethod.Cod, paymentStatus: "Pending");
                var service = new ShipperService(f.Db);

                Assert.IsFalse(service.GetMyOrders(f.Shipper.Id).Any(c => c.OrderId == order.Id));
                DispatchOrderCard card = service.ReadyOrders(f.Admin.Id).Single(c => c.OrderId == order.Id);
                Assert.IsNull(card.ShipperId);

                service.AssignShipper(f.Admin.Id, order.Id, f.Shipper.Id, Convert.FromBase64String(card.RowVersion));
                ShipperOrderCard mine = service.GetMyOrders(f.Shipper.Id).Single(c => c.OrderId == order.Id);
                Assert.AreEqual(OrderStatus.Ready, mine.Status);

                service.Act(f.Shipper.Id, order.Id, ShipperAction.PickUp, false, null, Convert.FromBase64String(mine.RowVersion));
                mine = service.GetMyOrders(f.Shipper.Id).Single(c => c.OrderId == order.Id);
                Assert.AreEqual(OrderStatus.OutForDelivery, mine.Status);
                Assert.IsFalse(service.ReadyOrders(f.Admin.Id).Any(c => c.OrderId == order.Id));

                service.Act(f.Shipper.Id, order.Id, ShipperAction.Deliver, true, null, Convert.FromBase64String(mine.RowVersion));

                Assert.IsFalse(service.GetMyOrders(f.Shipper.Id).Any(c => c.OrderId == order.Id));
                f.AssertState(order.Id, OrderStatus.Delivered, f.Shipper.Id);
                Assert.AreEqual("Paid", f.Reload(order.Id).PaymentStatus);
                Assert.AreEqual(1, f.CodCollectedCount(order.Id));
                List<OrderStatusHistory> history = f.History(order.Id).OrderBy(h => h.Id).ToList();
                Assert.AreEqual(2, history.Count);
                Assert.AreEqual(OrderStatus.OutForDelivery, history[0].To);
                Assert.AreEqual(OrderStatus.Delivered, history[1].To);
                Assert.IsTrue(history.All(h => h.ActorId == f.Shipper.Id));
            }
        }

        // ---------- Lifecycle guard ----------

        [TestMethod]
        public void Transition_KitchenStaffOrOtherRolesOnShipperTransitions_ThrowBr15AndLeaveOrderUnchanged()
        {
            using (var f = new Fixture())
            {
                Order ready = f.CreateOrder(OrderStatus.Ready, shipperId: f.Shipper.Id);
                Order onRoad = f.CreateOrder(OrderStatus.OutForDelivery, method: PaymentMethod.MoMo,
                    paymentStatus: "Paid", shipperId: f.Shipper.Id);
                var transitions = new OrderTransitionService(f.Db);

                foreach (int actorId in new[] { f.Kitchen.Id, f.Admin.Id, f.Customer.Id, f.OtherShipper.Id })
                {
                    int actor = actorId;
                    AssertRule("BR15", () => transitions.Transition(new TransitionOrderRequest
                    {
                        OrderId = ready.Id, ActorId = actor, TargetStatus = OrderStatus.OutForDelivery
                    }));
                    AssertRule("BR15", () => transitions.Transition(new TransitionOrderRequest
                    {
                        OrderId = onRoad.Id, ActorId = actor, TargetStatus = OrderStatus.Delivered, CashCollected = true
                    }));
                    AssertRule("BR15", () => transitions.Transition(new TransitionOrderRequest
                    {
                        OrderId = onRoad.Id, ActorId = actor, TargetStatus = OrderStatus.Failed,
                        Reason = "Not mine", CustomerUnreachable = true
                    }));
                }

                f.AssertState(ready.Id, OrderStatus.Ready, f.Shipper.Id);
                f.AssertState(onRoad.Id, OrderStatus.OutForDelivery, f.Shipper.Id);
                Assert.AreEqual(0, f.HistoryCount(ready.Id));
                Assert.AreEqual(0, f.HistoryCount(onRoad.Id));
            }
        }

        // ---------- Fixture ----------

        private sealed class Fixture : IDisposable
        {
            private readonly string _suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            private readonly string _codePrefix;
            private int _orderCounter;
            private readonly Brand _brand;
            private readonly MenuItem _item;

            public AppDbContext Db { get; private set; }
            public Hub Hub { get; private set; }
            public User Customer { get; private set; }
            public User Admin { get; private set; }
            public User InactiveAdmin { get; private set; }
            public User Kitchen { get; private set; }
            public User Shipper { get; private set; }
            public User OtherShipper { get; private set; }
            public User InactiveShipper { get; private set; }
            public User DeletedShipper { get; private set; }
            public string ItemName { get { return _item.Name; } }

            public Fixture()
            {
                Db = new AppDbContext();
                _codePrefix = "SB-S-" + _suffix + "-";
                _brand = new Brand { Name = "Shipper test " + _suffix, Category = "Test" };
                Hub = new Hub { Name = "Shipper test hub " + _suffix, Location = "Test kitchen address", Capacity = 10, IsActive = true };
                Db.Brands.Add(_brand);
                Db.Hubs.Add(Hub);
                Db.SaveChanges();

                _item = new MenuItem
                {
                    BrandId = _brand.Id,
                    Name = "Shipper test item " + _suffix,
                    Price = 100000m,
                    IsAvailable = true
                };
                Customer = NewUser("customer", ActorRole.Customer, true, CustomerType.Local);
                Admin = NewUser("admin", ActorRole.Admin, true);
                InactiveAdmin = NewUser("inactiveadmin", ActorRole.Admin, false);
                Kitchen = NewUser("kitchen", ActorRole.KitchenStaff, true);
                Shipper = NewUser("shipper", ActorRole.Shipper, true);
                OtherShipper = NewUser("othershipper", ActorRole.Shipper, true);
                InactiveShipper = NewUser("inactiveshipper", ActorRole.Shipper, false);
                DeletedShipper = NewUser("deletedshipper", ActorRole.Shipper, true);
                DeletedShipper.IsDeleted = true;
                Db.MenuItems.Add(_item);
                foreach (User user in AllUsers()) Db.Users.Add(user);
                Db.SaveChanges();

                Db.StaffProfiles.Add(new StaffProfile { UserId = Kitchen.Id, HubId = Hub.Id });
                Db.SaveChanges();
            }

            private User[] AllUsers()
            {
                return new[] { Customer, Admin, InactiveAdmin, Kitchen, Shipper, OtherShipper, InactiveShipper, DeletedShipper };
            }

            private User NewUser(string name, ActorRole role, bool active, CustomerType? customerType = null)
            {
                return new User
                {
                    Identifier = name + "." + _suffix + "@saigonbite.test",
                    PasswordHash = "test-only-not-for-login",
                    FullName = name + " test actor " + _suffix,
                    Role = role,
                    CustomerType = customerType,
                    IsActive = active
                };
            }

            public Order CreateOrder(OrderStatus status, PaymentMethod method = PaymentMethod.MoMo,
                string paymentStatus = "Paid", DateTimeOffset? placedAt = null, int? shipperId = null)
            {
                _orderCounter++;
                var order = new Order
                {
                    OrderCode = _codePrefix + _orderCounter,
                    CustomerId = Customer.Id,
                    HubId = Hub.Id,
                    ShipperId = shipperId,
                    Status = status,
                    DeliveryAddress = "24 Nguyen Hue, District 1, HCMC " + _orderCounter,
                    DistanceKm = 3.2m,
                    PlacedAt = placedAt ?? new DateTimeOffset(2026, 11, 25, 14, 0, 0, TimeSpan.FromHours(7)).AddMinutes(_orderCounter),
                    HubLoadSnapshot = 0.20m,
                    HeavyRainSnapshot = false,
                    Subtotal = 200000m,
                    QuietDiscount = 20000m,
                    QuietDiscountGranted = true,
                    FoodTotal = 180000m,
                    DeliveryFee = 25000m,
                    RawTotal = 205000m,
                    PayableTotal = 205000m,
                    PaymentMethod = method,
                    PaymentStatus = paymentStatus
                };
                order.Lines.Add(new OrderLine
                {
                    MenuItemId = _item.Id,
                    Quantity = 2,
                    UnitPriceSnapshot = 100000m,
                    ItemNameSnapshot = _item.Name,
                    CategorySnapshot = "Test"
                });
                Db.Orders.Add(order);
                Db.SaveChanges();
                return order;
            }

            public byte[] RowVersion(int orderId)
            {
                using (var db = new AppDbContext())
                    return db.Orders.AsNoTracking().Single(o => o.Id == orderId).RowVersion;
            }

            public Order Reload(int orderId)
            {
                using (var db = new AppDbContext())
                    return db.Orders.AsNoTracking().Single(o => o.Id == orderId);
            }

            public List<OrderStatusHistory> History(int orderId)
            {
                using (var db = new AppDbContext())
                    return db.OrderStatusHistories.AsNoTracking().Where(h => h.OrderId == orderId).ToList();
            }

            public int HistoryCount(int orderId)
            {
                using (var db = new AppDbContext())
                    return db.OrderStatusHistories.Count(h => h.OrderId == orderId);
            }

            public List<PaymentRecord> Payments(int orderId)
            {
                using (var db = new AppDbContext())
                    return db.PaymentRecords.AsNoTracking().Where(p => p.OrderId == orderId).ToList();
            }

            public int CodCollectedCount(int orderId)
            {
                using (var db = new AppDbContext())
                    return db.PaymentRecords.Count(p => p.OrderId == orderId && p.Operation == "CodCollected");
            }

            public void AssertState(int orderId, OrderStatus expectedStatus, int? expectedShipperId)
            {
                Order stored = Reload(orderId);
                Assert.AreEqual(expectedStatus, stored.Status);
                Assert.AreEqual(expectedShipperId, stored.ShipperId);
            }

            public void Dispose()
            {
                try
                {
                    var orders = Db.Orders.Where(o => o.OrderCode.StartsWith(_codePrefix)).ToList();
                    foreach (Order order in orders)
                    {
                        Db.Set<OrderStatusHistory>().RemoveRange(Db.OrderStatusHistories.Where(h => h.OrderId == order.Id));
                        Db.Set<PaymentRecord>().RemoveRange(Db.PaymentRecords.Where(p => p.OrderId == order.Id));
                        Db.Set<OrderLine>().RemoveRange(Db.OrderLines.Where(l => l.OrderId == order.Id));
                        Db.Orders.Remove(order);
                    }
                    Db.SaveChanges();
                    Db.Set<StaffProfile>().RemoveRange(Db.StaffProfiles.Where(s => s.HubId == Hub.Id));
                    Db.MenuItems.Remove(_item);
                    Db.Set<User>().RemoveRange(AllUsers());
                    Db.Hubs.Remove(Hub);
                    Db.Brands.Remove(_brand);
                    Db.SaveChanges();
                }
                finally
                {
                    Db.Dispose();
                }
            }
        }
    }
}
