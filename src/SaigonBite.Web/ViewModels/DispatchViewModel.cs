using System.Collections.Generic;
using SaigonBite.Infrastructure.Services;

namespace SaigonBite.Web.ViewModels
{
    /// <summary>Dữ liệu trang Admin gán shipper: đơn Ready và danh sách shipper đang hoạt động.</summary>
    public sealed class DispatchViewModel
    {
        public IList<DispatchOrderCard> Orders { get; set; }
        public IList<ShipperInfo> Shippers { get; set; }
    }
}
