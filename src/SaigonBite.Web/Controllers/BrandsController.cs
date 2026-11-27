using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using SaigonBite.Application.Menu;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Repositories;
using SaigonBite.Web.Infrastructure;
using SaigonBite.Web.Models;

namespace SaigonBite.Web.Controllers
{
    // UC02 Manage Menu and Brands — Brand CRUD (FR08-FR11, FR13). Author: 524H0189.
    public class BrandsController : Controller
    {
        public ActionResult Index(bool includeDeleted = false)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                var service = new MenuService(new EfMenuRepository(db));
                var model = new BrandIndexViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    IncludeDeleted = includeDeleted,
                    Brands = service.GetAdminBrands(role, includeDeleted),
                    Hubs = service.GetActiveHubs()
                };
                return View(model);
            }
        }

        public ActionResult Details(int id)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                var service = new MenuService(new EfMenuRepository(db));
                Brand brand = service.GetBrandDetails(role, id);
                if (brand == null) return HttpNotFound();

                ViewData["Lang"] = CurrentActorHelper.ResolveLanguage(Request);
                ViewData["Hubs"] = service.GetActiveHubs();
                return View(brand);
            }
        }

        public ActionResult Create()
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                var service = new MenuService(new EfMenuRepository(db));
                IList<Hub> hubs = service.GetActiveHubs();
                var model = new BrandFormViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    IsActive = true,
                    AllHubs = hubs,
                    SelectedHubIds = hubs.Select(h => h.Id).ToList()
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BrandFormViewModel model)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                var service = new MenuService(new EfMenuRepository(db));
                model.Lang = CurrentActorHelper.ResolveLanguage(Request);
                model.AllHubs = service.GetActiveHubs();

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = service.CreateBrand(role, new BrandInput
                {
                    Name = model.Name,
                    Category = model.Category,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    HubIds = model.SelectedHubIds ?? new List<int>()
                });

                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đã tạo thương hiệu mới thành công."
                    : "Brand created successfully.";
                return RedirectToAction("Index");
            }
        }

        public ActionResult Edit(int id)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                var service = new MenuService(new EfMenuRepository(db));
                Brand brand = service.GetBrandDetails(role, id);
                if (brand == null || brand.IsDeleted) return HttpNotFound();

                var model = new BrandFormViewModel
                {
                    Id = brand.Id,
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    Name = brand.Name,
                    Category = brand.Category,
                    Description = brand.Description,
                    IsActive = brand.IsActive,
                    AllHubs = service.GetActiveHubs(),
                    SelectedHubIds = brand.HubBrands != null
                        ? brand.HubBrands.Select(hb => hb.HubId).ToList()
                        : new List<int>()
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(BrandFormViewModel model)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                var service = new MenuService(new EfMenuRepository(db));
                model.Lang = CurrentActorHelper.ResolveLanguage(Request);
                model.AllHubs = service.GetActiveHubs();

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = service.UpdateBrand(role, model.Id, new BrandInput
                {
                    Name = model.Name,
                    Category = model.Category,
                    Description = model.Description,
                    IsActive = model.IsActive,
                    HubIds = model.SelectedHubIds ?? new List<int>()
                });

                if (!result.Succeeded)
                {
                    foreach (var err in result.FieldErrors)
                    {
                        ModelState.AddModelError(err.Key, err.Value);
                    }
                    return View(model);
                }

                TempData["StatusMessage"] = model.Lang == "vi"
                    ? "Đã cập nhật thương hiệu (áp dụng cho đơn tạo sau)."
                    : "Brand updated (applies to future orders only).";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SoftDelete(int id)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role = CurrentActorHelper.ResolveRole(HttpContext, db);
                if (role != ActorRole.Admin)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Admin role required (FR13).");
                }

                string lang = CurrentActorHelper.ResolveLanguage(Request);
                var service = new MenuService(new EfMenuRepository(db));
                var result = service.SoftDeleteBrand(role, id);
                if (!result.Succeeded) return HttpNotFound();

                TempData["StatusMessage"] = lang == "vi"
                    ? "Đã ngừng hoạt động (soft-delete) thương hiệu; lịch sử đơn và báo cáo R1 vẫn được bảo toàn."
                    : "Brand deactivated (soft-deleted); order history and R1 report data remain preserved.";
                return RedirectToAction("Index");
            }
        }
    }
}
