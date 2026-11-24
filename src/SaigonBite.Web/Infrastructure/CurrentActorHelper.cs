using System;
using System.Configuration;
using System.Web;
using SaigonBite.Domain;

namespace SaigonBite.Web.Infrastructure
{
    /// <summary>
    /// Resolves the authenticated actor role from server Session (FR13 / NFR04).
    /// Supports an explicit Web.config dev fallback flag until HT01 LoginController merges.
    /// </summary>
    public static class CurrentActorHelper
    {
        public const string RoleSessionKey = "SaigonBite.ActorRole";

        public static ActorRole ResolveRole(HttpContextBase httpContext)
        {
            if (httpContext != null && httpContext.Session != null)
            {
                object raw = httpContext.Session[RoleSessionKey];
                if (raw is ActorRole)
                {
                    return (ActorRole)raw;
                }

                ActorRole parsed;
                if (raw is string && Enum.TryParse((string)raw, true, out parsed))
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
