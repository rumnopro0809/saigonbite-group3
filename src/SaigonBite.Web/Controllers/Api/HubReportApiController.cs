using System;
using System.Web.Http;
using SaigonBite.Application;

namespace SaigonBite.Web.Controllers.Api
{
    /// <summary>REST API báo cáo R2 — hiệu suất sử dụng bếp (FR19).</summary>
    [RoutePrefix("api/reports/hub-utilization")]
    public class HubReportApiController : Uc03ApiController
    {
        private readonly HubReportService _report;

        public HubReportApiController()
        {
            _report = new HubReportService(Db);
        }

        /// <summary>GET /api/reports/hub-utilization?from=2026-11-01&amp;to=2026-11-07 — cả hai ngày đều bắt buộc.</summary>
        [HttpGet, Route("")]
        public IHttpActionResult Get(DateTime? from = null, DateTime? to = null)
        {
            if (from == null || to == null) return BadRequestMessage("Cần có cả hai tham số from và to (định dạng yyyy-MM-dd).");
            return FromResult(_report.Build(from.Value, to.Value));
        }
    }
}
