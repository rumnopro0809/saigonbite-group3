using System.Web.Mvc;
using System.Web.Routing;

namespace SaigonBite.Web
{
    /// <summary>Đường dẫn của các trang MVC (trả về giao diện HTML).</summary>
    public static class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Các đường dẫn /api/... thuộc Web API, đã đăng ký riêng trong WebApiConfig.
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Hubs", action = "Index", id = UrlParameter.Optional });
        }
    }
}
