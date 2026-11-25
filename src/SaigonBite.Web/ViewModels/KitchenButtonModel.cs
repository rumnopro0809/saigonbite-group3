using SaigonBite.Infrastructure.Services;

namespace SaigonBite.Web.ViewModels
{
    /// <summary>Dữ liệu của một nút thao tác trên thẻ đơn ở bảng bếp.</summary>
    public sealed class KitchenButtonModel
    {
        public KitchenOrderCard Order { get; set; }
        public KitchenAction Action { get; set; }
        public string Label { get; set; }
        public bool Primary { get; set; }
    }
}
