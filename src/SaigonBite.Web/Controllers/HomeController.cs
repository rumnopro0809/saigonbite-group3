using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Web.Models;

namespace SaigonBite.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            string lang = Request.Cookies["sb_lang"]?.Value == "en" ? "en" : "vi";

            using (var db = new SaigonBiteDbContext())
            {
                var setting = db.SystemSettings.FirstOrDefault();
                var hubs = db.Hubs
                    .Where(h => !h.IsDeleted && h.IsActive)
                    .OrderBy(h => h.Id)
                    .ToList();

                var items = db.MenuItems
                    .Include(m => m.Brand)
                    .Where(m => !m.IsDeleted && m.IsAvailable)
                    .OrderBy(m => m.Id)
                    .ToList();

                var model = new HomeViewModel
                {
                    Lang = lang,
                    IsHeavyRainOn = setting != null && setting.IsHeavyRainOn,
                    Hubs = hubs,
                    MenuItems = items
                };

                return View(model);
            }
        }

        public ActionResult SetLanguage(string lang, string returnUrl)
        {
            string normalized = string.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "vi";
            Response.Cookies.Add(new HttpCookie("sb_lang", normalized)
            {
                Expires = DateTime.UtcNow.AddDays(30)
            });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }
    }
}
