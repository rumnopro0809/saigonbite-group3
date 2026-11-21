using System.Collections.Generic;
using System.Linq;
using SaigonBite.Domain;

namespace SaigonBite.Application
{
    /// <summary>
    /// Đếm số đơn đang xử lý của từng bếp để tính tải (FR14.2, BR8).
    /// UC01 sẽ gọi lớp này khi đặt hàng để biết bếp còn nhận đơn hay không.
    /// </summary>
    public sealed class HubLoadService
    {
        private readonly IAppDbContext _db;

        public HubLoadService(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>Số đơn đang xử lý của mọi bếp. Bếp không có đơn thì không có trong kết quả.</summary>
        public IDictionary<int, int> CountActiveOrders()
        {
            // BR8: chỉ Confirmed và Preparing được tính (cùng quy tắc với HubLoad.CountsTowardLoad).
            // Viết thẳng điều kiện ở đây vì EF không dịch được lời gọi hàm C# sang SQL.
            return _db.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing)
                .GroupBy(o => o.HubId)
                .Select(g => new { HubId = g.Key, Count = g.Count() })
                .ToDictionary(x => x.HubId, x => x.Count);
        }

        /// <summary>Số đơn đang xử lý của một bếp.</summary>
        public int CountActiveOrders(int hubId)
        {
            return _db.Orders.Count(o => o.HubId == hubId
                && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing));
        }
    }
}
