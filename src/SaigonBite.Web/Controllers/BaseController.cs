using System.Linq;
using System.Web;
using System.Web.Mvc;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Web.Helpers;

namespace SaigonBite.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly SaigonBiteDbContext Db = new SaigonBiteDbContext();

        protected User CurrentUser { get; private set; }
        protected string CurrentLang { get; private set; }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            HttpCookie langCookie = Request.Cookies["sb_lang"];
            CurrentLang = (langCookie != null && langCookie.Value == "en") ? "en" : "vi";
            CurrentUser = DemoSessionHelper.GetCurrentUser(Db, Session);

            ViewBag.Lang = CurrentLang;
            ViewBag.CurrentUser = CurrentUser;
            ViewBag.DemoUsers = Db.Users
                .Where(u => !u.IsDeleted && u.IsActive)
                .OrderBy(u => u.Id)
                .ToList();

            base.OnActionExecuting(filterContext);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
