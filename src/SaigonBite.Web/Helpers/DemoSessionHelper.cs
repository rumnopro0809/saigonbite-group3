using System.Data.Entity;
using System.Linq;
using System.Web;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Web.Helpers
{
    public static class DemoSessionHelper
    {
        private const string SessionKey = "CurrentUserId";

        public static int? GetCurrentUserId(HttpSessionStateBase session)
        {
            if (session == null) return null;
            object raw = session[SessionKey];
            if (raw is int) return (int)raw;
            return null;
        }

        public static void SetCurrentUserId(HttpSessionStateBase session, int? userId)
        {
            if (session == null) return;
            if (userId.HasValue)
            {
                session[SessionKey] = userId.Value;
            }
            else
            {
                session.Remove(SessionKey);
            }
        }

        public static User GetCurrentUser(SaigonBiteDbContext db, HttpSessionStateBase session)
        {
            int? userId = GetCurrentUserId(session);
            if (!userId.HasValue) return null;

            return db.Users
                .Include(u => u.StaffProfile)
                .FirstOrDefault(u => u.Id == userId.Value && !u.IsDeleted && u.IsActive);
        }
    }
}
