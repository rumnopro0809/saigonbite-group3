using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Web.Infrastructure
{
    // Resolves the current user role from Session (FR13 / NFR04).
    // Supports both CurrentUserId (shared with UC01 demo switcher) and direct role key.
    public static class CurrentActorHelper
    {
        public const string UserIdSessionKey = "CurrentUserId";
        public const string RoleSessionKey = "SaigonBite.ActorRole";

        public static ActorRole? TryResolveRole(HttpContextBase httpContext, SaigonBiteDbContext db)
        {
            if (httpContext != null && httpContext.Session != null)
            {
                object rawUserId = httpContext.Session[UserIdSessionKey];
                if (rawUserId is int && db != null)
                {
                    int userId = (int)rawUserId;
                    User user = db.Users.FirstOrDefault(u => u.Id == userId && !u.IsDeleted && u.IsActive);
                    return user != null ? (ActorRole?)user.Role : null;
                }

                object rawRole = httpContext.Session[RoleSessionKey];
                if (rawRole is ActorRole)
                {
                    return (ActorRole)rawRole;
                }

                ActorRole parsed;
                if (rawRole is string && Enum.TryParse((string)rawRole, true, out parsed))
                {
                    return parsed;
                }
            }

            string devFallback = ConfigurationManager.AppSettings["Uc02:DevAdminFallback"];
            if (string.Equals(devFallback, "true", StringComparison.OrdinalIgnoreCase))
            {
                return ActorRole.Admin;
            }

            return null;
        }

        public static ActorRole ResolveRole(HttpContextBase httpContext, SaigonBiteDbContext db)
        {
            return TryResolveRole(httpContext, db) ?? ActorRole.Customer;
        }

        // Unauthenticated users are redirected to /Account/Login; signed-in non-Admins receive 403 (FR13 / NFR04).
        public static ActionResult RequireAdmin(Controller controller, SaigonBiteDbContext db, out ActorRole role)
        {
            ActorRole? resolved = TryResolveRole(controller != null ? controller.HttpContext : null, db);
            if (!resolved.HasValue)
            {
                role = ActorRole.Customer;
                return new RedirectToRouteResult(new RouteValueDictionary(new
                {
                    controller = "Account",
                    action = "Login"
                }));
            }

            role = resolved.Value;
            if (role != ActorRole.Admin)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required.");
            }

            return null;
        }

        public static string ResolveLanguage(HttpRequestBase request)
        {
            if (request == null) return "vi";
            HttpCookie cookie = request.Cookies["sb_lang"];
            return (cookie != null && string.Equals(cookie.Value, "en", StringComparison.OrdinalIgnoreCase))
                ? "en"
                : "vi";
        }
    }
}
