using System.Collections.Generic;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Orders
{
    public interface IOrderService
    {
        OrderQuoteResult Quote(CreateOrderRequest request);
        Order PlaceOrder(CreateOrderRequest request);
        List<Order> GetOrdersForActor(int actorId);
        Order GetOrderDetails(int orderId, int actorId);
    }
}
