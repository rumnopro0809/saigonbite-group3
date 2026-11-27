using System;
using System.Configuration;
using System.Linq;
using System.Web;
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

        public static ActorRole ResolveRole(HttpContextBase httpContext, SaigonBiteDbContext db)
        {
            if (httpContext != null && httpContext.Session != null)
            {
                object rawUserId = httpContext.Session[UserIdSessionKey];
                if (rawUserId is int && db != null)
                {
                    int userId = (int)rawUserId;
                    User user = db.Users.FirstOrDefault(u => u.Id == userId && !u.IsDeleted && u.IsActive);
                    return user != null ? user.Role : ActorRole.Customer;
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

            return ActorRole.Customer;
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
