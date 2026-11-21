using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.Annotations;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.ComponentModel.DataAnnotations.Schema;
using SaigonBite.Application;
using SaigonBite.Domain;

namespace SaigonBite.Infrastructure
{
    /// <summary>
    /// DbContext của EF6 Code First. Chứa phần schema của UC03 (Hub, HubBrand, SystemSetting)
    /// và các bảng tạm của UC khác (Brand, Order, OrderStatusHistory) để UC03 chạy được.
    /// </summary>
    public class AppDbContext : DbContext, IAppDbContext
    {
        /// <summary>Tên chuỗi kết nối trong Web.config.</summary>
        public const string ConnectionName = "SaigonBiteDb";

        public AppDbContext() : base("name=" + ConnectionName) { }

        public AppDbContext(string nameOrConnectionString) : base(nameOrConnectionString) { }

        public IDbSet<Hub> Hubs { get; set; }
        public IDbSet<HubBrand> HubBrands { get; set; }
        public IDbSet<Brand> Brands { get; set; }
        public IDbSet<SystemSetting> SystemSettings { get; set; }
        public IDbSet<Order> Orders { get; set; }
        public IDbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

        public void SetOriginalRowVersion<T>(T entity, byte[] rowVersion) where T : class
        {
            Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Tắt xóa dây chuyền: không để xóa bếp kéo theo xóa đơn hàng (lịch sử phải được giữ).
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
            modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();

            // Hub (FR14)
            modelBuilder.Entity<Hub>().Property(h => h.Name).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Hub>().Property(h => h.Location).IsRequired().HasMaxLength(200);
            modelBuilder.Entity<Hub>().Property(h => h.RowVersion).IsRowVersion();

            // HubBrand: khóa chính ghép (HubId, BrandId)
            modelBuilder.Entity<HubBrand>().HasKey(x => new { x.HubId, x.BrandId });
            modelBuilder.Entity<HubBrand>().HasRequired(x => x.Hub).WithMany(h => h.Brands).HasForeignKey(x => x.HubId);
            modelBuilder.Entity<HubBrand>().HasRequired(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId);

            modelBuilder.Entity<Brand>().Property(b => b.Name).IsRequired().HasMaxLength(100);

            // SystemSetting: chỉ một dòng, Id đặt tay (không tự tăng)
            modelBuilder.Entity<SystemSetting>().Property(s => s.Id).HasDatabaseGeneratedOption(DatabaseGeneratedOption.None);
            modelBuilder.Entity<SystemSetting>().Property(s => s.RowVersion).IsRowVersion();

            // Order tạm: index (HubId, Status) để đếm tải nhanh (BR8)
            modelBuilder.Entity<Order>().HasRequired(o => o.Hub).WithMany().HasForeignKey(o => o.HubId);
            modelBuilder.Entity<Order>().Property(o => o.HubId)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("IX_Orders_Hub_Status", 1)));
            modelBuilder.Entity<Order>().Property(o => o.Status)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName, new IndexAnnotation(new IndexAttribute("IX_Orders_Hub_Status", 2)));

            modelBuilder.Entity<OrderStatusHistory>().HasRequired(h => h.Order).WithMany().HasForeignKey(h => h.OrderId);
        }
    }
}
