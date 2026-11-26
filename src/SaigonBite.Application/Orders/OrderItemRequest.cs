namespace SaigonBite.Application.Orders
{
    public sealed class OrderItemRequest
    {
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
    }
}
