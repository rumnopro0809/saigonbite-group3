using SaigonBite.Domain;

namespace SaigonBite.Application.Orders
{
    public sealed class TransitionOrderRequest
    {
        public int OrderId { get; set; }
        public int ActorId { get; set; }
        public OrderStatus TargetStatus { get; set; }
        public string Reason { get; set; }
        public bool CashCollected { get; set; }
        public bool CustomerUnreachable { get; set; }
        public byte[] ExpectedRowVersion { get; set; }
    }
}
