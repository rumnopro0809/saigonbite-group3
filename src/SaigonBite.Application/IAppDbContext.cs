using System;
using System.Data.Entity;
using SaigonBite.Domain;

namespace SaigonBite.Application
{
    /// <summary>
    /// Cửa vào database mà service dùng. Lớp Infrastructure sẽ cài đặt bằng EF6.
    /// Tách ra như vậy để service không phụ thuộc vào cách tạo kết nối.
    /// </summary>
    public interface IAppDbContext : IDisposable
    {
        IDbSet<Hub> Hubs { get; }
        IDbSet<HubBrand> HubBrands { get; }
        IDbSet<Brand> Brands { get; }
        IDbSet<SystemSetting> SystemSettings { get; }
        IDbSet<Order> Orders { get; }
        IDbSet<OrderStatusHistory> OrderStatusHistories { get; }

        /// <summary>Lưu mọi thay đổi trong một transaction.</summary>
        int SaveChanges();

        /// <summary>
        /// Báo cho EF biết phiên bản rowversion mà người dùng đã thấy.
        /// Nếu dữ liệu đã bị người khác sửa thì SaveChanges sẽ báo lỗi xung đột.
        /// </summary>
        void SetOriginalRowVersion<T>(T entity, byte[] rowVersion) where T : class;
    }
}
