namespace SaigonBite.Domain
{
    public enum OrderStatus { Placed, Confirmed, Preparing, Ready, OutForDelivery, Delivered, Cancelled, Failed }
    public enum ActorRole { Customer, KitchenStaff, Shipper, Admin }

    public sealed class TransitionContext
    {
        public ActorRole Role { get; set; }
        public bool OwnsOrder { get; set; }
        public bool OwnHub { get; set; }
        public bool AssignedShipper { get; set; }
        public bool HasRejectionReason { get; set; }
        public bool IsCod { get; set; }
        public bool CashCollected { get; set; }
        public bool CustomerUnreachable { get; set; }
    }

    // FR17 / BR14-BR16: default deny. Callers persist successful state/history atomically.
    public static class OrderLifecycle
    {
        public static bool CanTransition(OrderStatus from, OrderStatus to, TransitionContext c)
        {
            if (c == null) return false;
            bool kitchen = c.Role == ActorRole.KitchenStaff && c.OwnHub;
            bool customer = c.Role == ActorRole.Customer && c.OwnsOrder;
            bool shipper = c.Role == ActorRole.Shipper && c.AssignedShipper;
            if (from == OrderStatus.Placed && to == OrderStatus.Confirmed) return kitchen;
            if (from == OrderStatus.Placed && to == OrderStatus.Cancelled) return customer || (kitchen && c.HasRejectionReason);
            if (from == OrderStatus.Confirmed && to == OrderStatus.Preparing) return kitchen;
            if (from == OrderStatus.Confirmed && to == OrderStatus.Cancelled) return customer || c.Role == ActorRole.Admin;
            if (from == OrderStatus.Preparing && to == OrderStatus.Ready) return kitchen;
            if (from == OrderStatus.Ready && to == OrderStatus.OutForDelivery) return shipper;
            if (from == OrderStatus.OutForDelivery && to == OrderStatus.Delivered) return shipper && (!c.IsCod || c.CashCollected);
            if (from == OrderStatus.OutForDelivery && to == OrderStatus.Failed) return shipper && c.CustomerUnreachable;
            return false;
        }
    }
}
