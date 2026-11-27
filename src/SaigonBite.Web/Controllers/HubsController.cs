using System;
using System.Web.Mvc;
using SaigonBite.Application.Hubs;
using SaigonBite.Infrastructure.Services;
using SaigonBite.Web.ViewModels;

namespace SaigonBite.Web.Controllers
{
    /// <summary>
    /// Các trang quản lý bếp (UC03, FR14). Ba nút bật/tắt cờ mưa, ngừng nhận đơn và xóa bếp gọi các action
    /// JSON bên dưới (POST, có chống giả mạo) qua Scripts/uc03.js.
    /// </summary>
    public class HubsController : Uc03Controller
    {
        private readonly HubService _hubs;
        private readonly RainSettingService _rain;

        public HubsController()
        {
            _hubs = new HubService(Db);
            _rain = new RainSettingService(Db);
        }

        /// <summary>GET /Hubs — danh sách bếp, tải hiện tại và cờ mưa (FR14.2, FR16).</summary>
        public ActionResult Index()
        {
            return View(new HubIndexVm { Hubs = _hubs.List(), Rain = _rain.Get() });
        }

        /// <summary>GET /Hubs/Details/5 — chi tiết một bếp.</summary>
        public ActionResult Details(int id)
        {
            var result = _hubs.Get(id);
            if (!result.IsSuccess) return HttpNotFound();
            return View(result.Value);
        }

        /// <summary>GET /Hubs/Create — form tạo bếp mới.</summary>
        [HttpGet]
        public ActionResult Create()
        {
            return View(new HubFormVm { Capacity = 20, AllBrands = _hubs.GetBrandOptions() });
        }

        /// <summary>POST /Hubs/Create — lưu bếp mới (FR14.1).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HubFormVm vm)
        {
            if (ModelState.IsValid)
            {
                var result = _hubs.Create(vm.ToInput());
                if (result.IsSuccess)
                {
                    TempData["Message"] = HubText.Localize("Đã tạo bếp mới.", English);
                    return RedirectToAction("Details", new { id = result.Value.Id });
                }
                AddErrors(result);
            }
            LocalizeModelState();
            vm.AllBrands = _hubs.GetBrandOptions();
            return View(vm);
        }

        /// <summary>GET /Hubs/Edit/5 — form sửa bếp.</summary>
        [HttpGet]
        public ActionResult Edit(int id)
        {
            var result = _hubs.Get(id);
            if (!result.IsSuccess) return HttpNotFound();
            var vm = HubFormVm.FromDto(result.Value);
            vm.AllBrands = _hubs.GetBrandOptions();
            return View(vm);
        }

        /// <summary>POST /Hubs/Edit/5 — lưu thay đổi, có kiểm tra rowversion (FR14.3).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, HubFormVm vm)
        {
            vm.Id = id;
            if (ModelState.IsValid)
            {
                var result = _hubs.Update(id, vm.ToInput());
                if (result.Status == ResultStatus.NotFound) return HttpNotFound();
                if (result.IsSuccess)
                {
                    TempData["Message"] = HubText.Localize("Đã lưu thay đổi.", English);
                    return RedirectToAction("Details", new { id });
                }
                AddErrors(result);
            }
            LocalizeModelState();
            vm.AllBrands = _hubs.GetBrandOptions();
            return View(vm);
        }

        /// <summary>POST /Hubs/SetRain — bật/tắt cờ mưa toàn hệ thống (FR16, BR5). Chỉ đơn đặt sau đó bị ảnh hưởng.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetRain(bool isHeavyRainOn, string rowVersion)
        {
            return JsonResultOf(_rain.Set(isHeavyRainOn, rowVersion),
                r => new { isHeavyRainOn = r.IsHeavyRainOn, rowVersion = r.RowVersion });
        }

        /// <summary>POST /Hubs/SetActive/5 — ngừng hoặc nhận lại đơn mới; đơn đang chạy vẫn xử lý tiếp (FR14.4).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetActive(int id, bool isActive, string rowVersion)
        {
            return JsonResultOf(_hubs.SetActive(id, isActive, rowVersion),
                h => new { id = h.Id, isActive = h.IsActive, rowVersion = h.RowVersion });
        }

        /// <summary>POST /Hubs/Delete/5 — xóa mềm bếp, giữ đơn và lịch sử (FR14.4).</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string rowVersion)
        {
            return JsonResultOf(_hubs.Delete(id, rowVersion), h => new { id = h.Id });
        }
    }
}
