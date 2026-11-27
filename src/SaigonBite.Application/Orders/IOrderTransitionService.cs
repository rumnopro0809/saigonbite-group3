using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Orders
{
    public interface IOrderTransitionService
    {
        Order Transition(TransitionOrderRequest request);
    }
}
