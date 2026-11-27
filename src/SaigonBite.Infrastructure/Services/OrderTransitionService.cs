using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SaigonBite.Application.Orders;
using SaigonBite.Application.Payments;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Services
{
    /// <summary>
    /// Enforces BR15-BR16 state transitions, audit history, and simulated refunds atomically.
    /// </summary>
    public sealed class OrderTransitionService : IOrderTransitionService
    {
        private readonly SaigonBiteDbContext _db;
        private readonly PaymentSelector _paymentSelector;

        public OrderTransitionService(SaigonBiteDbContext db, PaymentSelector paymentSelector = null)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
            _paymentSelector = paymentSelector ?? PaymentSelector.CreateDefault();
        }

        public Order Transition(TransitionOrderRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            Order order = _db.Orders
                .Include(o => o.Payments)
                .Include(o => o.StatusHistory)
                .FirstOrDefault(o => o.Id == request.OrderId);

            if (order == null)
                throw new BusinessRuleException("NOT_FOUND", "Order not found.");

            User actor = _db.Users
                .Include(u => u.StaffProfile)
                .FirstOrDefault(u => u.Id == request.ActorId && !u.IsDeleted && u.IsActive);

            if (actor == null)
                throw new BusinessRuleException("UNAUTHORIZED", "Actor account not found or inactive.");

            if (request.ExpectedRowVersion != null &&
                (order.RowVersion == null || !order.RowVersion.SequenceEqual(request.ExpectedRowVersion)))
            {
                throw new BusinessRuleException(
                    "CONCURRENCY",
                    "Order was modified by another request. Please refresh.");
            }

            var context = new TransitionContext
            {
                Role = actor.Role,
                OwnsOrder = order.CustomerId == actor.Id,
                OwnHub = actor.StaffProfile != null && actor.StaffProfile.HubId == order.HubId,
                AssignedShipper = order.ShipperId.HasValue && order.ShipperId.Value == actor.Id,
                HasRejectionReason = !string.IsNullOrWhiteSpace(request.Reason),
                IsCod = order.PaymentMethod == PaymentMethod.Cod,
                CashCollected = request.CashCollected,
                CustomerUnreachable = request.CustomerUnreachable
            };

            if (!OrderLifecycle.CanTransition(order.Status, request.TargetStatus, context))
            {
                throw new BusinessRuleException(
                    "BR15",
                    "Transition from " + order.Status + " to " + request.TargetStatus + " is not allowed.");
            }

            using (var tx = _db.Database.BeginTransaction())
            {
                try
                {
                    OrderStatus previousStatus = order.Status;
                    order.Status = request.TargetStatus;

                    _db.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        ActorId = actor.Id,
                        FromStatus = previousStatus,
                        ToStatus = request.TargetStatus,
                        Reason = request.Reason,
                        CashCollected = request.CashCollected,
                        ChangedAt = DateTimeOffset.UtcNow
                    });

                    if (request.TargetStatus == OrderStatus.Delivered &&
                        order.PaymentMethod == PaymentMethod.Cod)
                    {
                        order.PaymentStatus = "Paid";
                        _db.PaymentRecords.Add(new PaymentRecord
                        {
                            OrderId = order.Id,
                            Method = PaymentMethod.Cod,
                            Operation = "CodCollected",
                            Amount = order.PayableTotal,
                            Result = "Paid",
                            CorrelationKey = "COD-COL-" + order.OrderCode,
                            CreatedAt = DateTimeOffset.UtcNow
                        });
                    }

                    if (request.TargetStatus == OrderStatus.Cancelled &&
                        string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
                    {
                        IPaymentStrategy strategy = _paymentSelector.SelectForRefund(order.PaymentMethod);
                        if (strategy.IsOnline)
                        {
                            PaymentResult refund = strategy.Refund(new PaymentRequest
                            {
                                OrderCode = order.OrderCode,
                                Amount = order.PayableTotal,
                                Reason = request.Reason
                            });

                            order.PaymentStatus = "Refunded";
                            _db.PaymentRecords.Add(new PaymentRecord
                            {
                                OrderId = order.Id,
                                Method = order.PaymentMethod,
                                Operation = "Refund",
                                Amount = refund.Amount,
                                Result = refund.Status,
                                CorrelationKey = refund.CorrelationKey,
                                CreatedAt = refund.ProcessedAt
                            });
                        }
                    }

                    _db.SaveChanges();
                    tx.Commit();
                    return order;
                }
                catch (DbUpdateConcurrencyException)
                {
                    tx.Rollback();
                    throw new BusinessRuleException(
                        "CONCURRENCY",
                        "Order was modified concurrently. Please refresh and try again.");
                }
            }
        }
    }
}
