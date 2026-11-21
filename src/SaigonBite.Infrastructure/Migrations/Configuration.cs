using System.Data.Entity.Migrations;

namespace SaigonBite.Infrastructure.Migrations
{
    /// <summary>
    /// Cấu hình migration. Tắt migration tự động: mọi thay đổi schema phải có một file migration được commit vào Git.
    /// </summary>
    public sealed class Configuration : DbMigrationsConfiguration<AppDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "SaigonBite.Infrastructure.AppDbContext";
        }

        /// <summary>Chạy sau mỗi lần Update-Database. Chỉ thêm dữ liệu còn thiếu, không ghi đè dữ liệu đã sửa.</summary>
        protected override void Seed(AppDbContext context)
        {
            SampleData.Apply(context);
        }
    }
}
