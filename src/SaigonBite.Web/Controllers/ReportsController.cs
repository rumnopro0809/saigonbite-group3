using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using SaigonBite.Application;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>Trang báo cáo R2 — hiệu suất sử dụng bếp (FR19). Chỉ trả về giao diện, dữ liệu lấy từ HubReportService.</summary>
    public class ReportsController : Uc03Controller
    {
        private readonly HubReportService _report;

        public ReportsController()
        {
            _report = new HubReportService(Db);
        }

        /// <summary>
        /// GET /Reports/HubUtilization?from=2026-11-01&amp;to=2026-11-07
        /// Nếu không chọn ngày thì dùng khoảng mẫu đầu tiên (01 đến 07/11/2026).
        /// </summary>
        public ActionResult HubUtilization(DateTime? from, DateTime? to)
        {
            // TẠM: khoảng ngày mặc định khớp với dữ liệu mẫu. Sau này đổi thành "7 ngày gần nhất".
            DateTime start = from ?? new DateTime(2026, 11, 1);
            DateTime end = to ?? new DateTime(2026, 11, 7);

            var vm = new HubReportVm
            {
                From = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                To = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Presets = new List<DatePreset>
                {
                    new DatePreset { Label = "01–07/11/2026", From = "2026-11-01", To = "2026-11-07" },
                    new DatePreset { Label = "08–14/11/2026", From = "2026-11-08", To = "2026-11-14" },
                    new DatePreset { Label = "01–07/10/2026", From = "2026-10-01", To = "2026-10-07" }
                }
            };

            var result = _report.Build(start, end);
            if (result.IsSuccess) vm.Report = result.Value;
            else vm.ErrorMessage = result.Errors.Count > 0 ? result.Errors[0].Message : "Không tạo được báo cáo.";
            return View(vm);
        }
    }
}
