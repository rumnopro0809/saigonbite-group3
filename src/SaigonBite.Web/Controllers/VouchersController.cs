using System;
using System.Web.Mvc;
using SaigonBite.Application.Vouchers;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Repositories;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Models;

namespace SaigonBite.Web.Controllers
{
    // HT02 Admin Voucher CRUD controller (FR15, FR13, BR9). Author: 524H0189.
    public class VouchersController : BaseController
    {
        private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

        public ActionResult Index(bool includeDeleted = false)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new VoucherService(new EfVoucherRepository(db));
                var model = new VoucherIndexViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    IncludeDeleted = includeDeleted,
                    Vouchers = service.GetAdminVouchers(role, includeDeleted)
                };
                return View(model);
            }
        }

        public ActionResult Create()
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var model = new VoucherEditViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    Type = VoucherType.Percent,
                    Value = 10m,
                    Cap = 30000m,
                    MinimumSubtotal = 100000m,
                    ValidFromDate = DateTime.Today,
                    ValidToDate = DateTime.Today.AddMonths(1),
                    IsActive = true
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(VoucherEditViewModel model)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                model.Lang = CurrentActorHelper.ResolveLanguage(Request);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var service = new VoucherService(new EfVoucherRepository(db));
                var result = service.Create(role, ToInput(model));
                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đã tạo voucher mới thành công."
                    : "Voucher created successfully.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Edit(int id)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new VoucherService(new EfVoucherRepository(db));
                Voucher v = service.GetById(role, id);
                if (v == null || v.IsDeleted) return HttpNotFound();

                var model = new VoucherEditViewModel
                {
                    Id = v.Id,
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    Code = v.Code,
                    Type = v.Type,
                    Value = v.Value,
                    Cap = v.Cap,
                    MinimumSubtotal = v.MinimumSubtotal,
                    ValidFromDate = v.ValidFrom.ToOffset(VietnamOffset).Date,
                    ValidToDate = v.ValidTo.ToOffset(VietnamOffset).Date,
                    IsActive = v.IsActive
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(VoucherEditViewModel model)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                model.Lang = CurrentActorHelper.ResolveLanguage(Request);
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var service = new VoucherService(new EfVoucherRepository(db));
                var result = service.Update(role, model.Id, ToInput(model));
                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đã cập nhật voucher."
                    : "Voucher updated.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SoftDelete(int id)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                string lang = CurrentActorHelper.ResolveLanguage(Request);
                var service = new VoucherService(new EfVoucherRepository(db));
                var result = service.SoftDelete(role, id);
                if (!result.Succeeded) return HttpNotFound();

                TempData["StatusMessage"] = lang == "vi"
                    ? "Đã ngưng hiệu lực (soft-delete) voucher; các đơn cũ vẫn giữ nguyên giá trị giảm giá."
                    : "Voucher soft-deleted; applied values on historical orders remain intact.";
                return RedirectToAction("Index");
            }
        }

        private static VoucherFormInput ToInput(VoucherEditViewModel model)
        {
            DateTime fromDate = model.ValidFromDate.Date;
            DateTime toDate = model.ValidToDate.Date;

            return new VoucherFormInput
            {
                Code = model.Code,
                Type = model.Type,
                Value = model.Value,
                Cap = model.Cap,
                MinimumSubtotal = model.MinimumSubtotal,
                ValidFrom = new DateTimeOffset(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, VietnamOffset),
                ValidTo = new DateTimeOffset(toDate.Year, toDate.Month, toDate.Day, 23, 59, 59, VietnamOffset),
                IsActive = model.IsActive
            };
        }
    }
}
