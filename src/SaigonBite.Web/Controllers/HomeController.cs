using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SaigonBite.Domain.Entities;
using SaigonBite.Web.Helpers;
using SaigonBite.Web.Models;

namespace SaigonBite.Web.Controllers
{
    public class HomeController : BaseController
    {
        public ActionResult Index()
        {
            var setting = Db.SystemSettings.FirstOrDefault();
            var hubs = Db.Hubs
                .Where(h => !h.IsDeleted && h.IsActive)
                .OrderBy(h => h.Id)
                .ToList();

            var items = Db.MenuItems
                .Include(m => m.Brand)
                .Where(m => !m.IsDeleted && m.IsAvailable)
                .OrderBy(m => m.Id)
                .ToList();

            var model = new HomeViewModel
            {
                Lang = CurrentLang,
                IsHeavyRainOn = setting != null && setting.IsHeavyRainOn,
                Hubs = hubs,
                MenuItems = items
            };

            return View(model);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SwitchDemoUser(int? userId, string returnUrl)
        {
            if (!userId.HasValue)
            {
                DemoSessionHelper.SetCurrentUserId(Session, null);
            }
            else
            {
                User user = Db.Users.FirstOrDefault(u => u.Id == userId.Value && !u.IsDeleted && u.IsActive);
                if (user != null)
                {
                    DemoSessionHelper.SetCurrentUserId(Session, user.Id);
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index");
        }
    }
}
