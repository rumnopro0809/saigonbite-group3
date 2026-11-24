using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Migrations;

namespace SaigonBite.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<SaigonBiteDbContext, Configuration>());

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
