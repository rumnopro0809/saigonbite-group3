using System.Data.Entity;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Migrations;

namespace SaigonBite.Web
{
    /// <summary>Điểm khởi động của ứng dụng web.</summary>
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            // Khi chạy lần đầu, tự áp dụng mọi migration còn thiếu (và seed dữ liệu mẫu).
            // Cách này cho kết quả giống lệnh Update-Database.
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
