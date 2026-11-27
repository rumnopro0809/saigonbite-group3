using System;
using System.Net;
using System.Web.Mvc;
using SaigonBite.Application.Reports;
using SaigonBite.Domain;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Repositories;
using SaigonBite.Web.Infrastructure;

namespace SaigonBite.Web.Controllers
{
    // HT04 Admin Reports controller — R1 Revenue Report (FR18, AMB04). Author: 524H0189.
    public class ReportsController : Controller
    {
        public ActionResult Revenue(DateTime? fromDate, DateTime? toDate, bool deliveredOnly = true)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                DateTime end = (toDate ?? DateTime.Today).Date;
                DateTime start = (fromDate ?? end.AddDays(-30)).Date;

                var service = new RevenueReportService(new EfRevenueReportRepository(db));
                RevenueReportResult result = service.BuildReport(role, start, end, deliveredOnly);

                ViewData["Lang"] = CurrentActorHelper.ResolveLanguage(Request);
                return View(result);
            }
        }
    }
}
