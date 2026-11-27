using System.Collections.Generic;
using System.Linq;
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
    // UC02 Manage Menu and Brands — Menu CRUD and Hub-scoped Customer browse (FR08-FR11, FR13, BR12).
    // Author: 524H0189.
    public class MenuController : BaseController
    {
        public ActionResult Index(string search, int? brandId, string category, bool? isAvailable, bool includeDeleted = false)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new MenuService(new EfMenuRepository(db));
                IList<Brand> brands = service.GetAdminBrands(role, includeDeleted: true);
                var filter = new MenuFilter
                {
                    Search = search,
                    BrandId = brandId,
                    Category = category,
                    IsAvailable = isAvailable,
                    IncludeDeleted = includeDeleted
                };

                var model = new MenuIndexViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    Search = search,
                    BrandId = brandId,
                    Category = category,
                    IsAvailable = isAvailable,
                    IncludeDeleted = includeDeleted,
                    Brands = brands,
                    Categories = brands
                        .Where(b => !string.IsNullOrWhiteSpace(b.Category))
                        .Select(b => b.Category)
                        .Distinct()
                        .OrderBy(c => c)
                        .ToList(),
                    Items = service.GetAdminMenuItems(role, filter)
                };

                return View(model);
            }
        }

        // Customer read flow scoped by HubBrand and BR12 (open to all roles).
        public ActionResult Browse(int? hubId)
        {
            using (var db = new SaigonBiteDbContext())
            {
                var service = new MenuService(new EfMenuRepository(db));
                IList<Hub> hubs = service.GetActiveHubs();
                int selectedId = hubId.HasValue && hubs.Any(h => h.Id == hubId.Value)
                    ? hubId.Value
                    : (hubs.Count > 0 ? hubs[0].Id : 0);

                var model = new CustomerHubMenuViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    Hubs = hubs,
                    SelectedHubId = selectedId,
                    SelectedHub = hubs.FirstOrDefault(h => h.Id == selectedId),
                    Items = selectedId > 0 ? service.GetCustomerMenuByHub(selectedId) : new List<MenuItem>()
                };

                return View(model);
            }
        }

        public ActionResult Details(int id)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new MenuService(new EfMenuRepository(db));
                MenuItem item = service.GetMenuItemDetails(role, id);
                if (item == null) return HttpNotFound();

                ViewData["Lang"] = CurrentActorHelper.ResolveLanguage(Request);
                return View(item);
            }
        }

        public ActionResult Create()
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new MenuService(new EfMenuRepository(db));
                var model = new MenuItemFormViewModel
                {
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    IsAvailable = true,
                    ImageUrl = MenuService.DefaultImageUrl,
                    Brands = service.GetAdminBrands(role, includeDeleted: false)
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(MenuItemFormViewModel model)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new MenuService(new EfMenuRepository(db));
                model.Lang = CurrentActorHelper.ResolveLanguage(Request);
                model.Brands = service.GetAdminBrands(role, includeDeleted: false);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = service.CreateMenuItem(role, new MenuItemInput
                {
                    BrandId = model.BrandId,
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUrl = model.ImageUrl,
                    IsAvailable = model.IsAvailable
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
                    ? "Đã thêm món mới vào thực đơn."
                    : "Menu item created successfully.";
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

                var service = new MenuService(new EfMenuRepository(db));
                MenuItem item = service.GetMenuItemDetails(role, id);
                if (item == null || item.IsDeleted) return HttpNotFound();

                var model = new MenuItemFormViewModel
                {
                    Id = item.Id,
                    Lang = CurrentActorHelper.ResolveLanguage(Request),
                    BrandId = item.BrandId,
                    Name = item.Name,
                    Description = item.Description,
                    Price = item.Price,
                    ImageUrl = item.ImageUrl,
                    IsAvailable = item.IsAvailable,
                    Brands = service.GetAdminBrands(role, includeDeleted: false)
                };
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(MenuItemFormViewModel model)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                var service = new MenuService(new EfMenuRepository(db));
                model.Lang = CurrentActorHelper.ResolveLanguage(Request);
                model.Brands = service.GetAdminBrands(role, includeDeleted: false);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = service.UpdateMenuItem(role, model.Id, new MenuItemInput
                {
                    BrandId = model.BrandId,
                    Name = model.Name,
                    Description = model.Description,
                    Price = model.Price,
                    ImageUrl = model.ImageUrl,
                    IsAvailable = model.IsAvailable
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
                    ? "Đã cập nhật món ăn (giá và trạng thái áp dụng cho đơn tạo sau)."
                    : "Menu item updated (price and availability apply to future orders only).";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleAvailability(int id, bool isAvailable)
        {
            using (var db = new SaigonBiteDbContext())
            {
                ActorRole role;
                ActionResult denied = CurrentActorHelper.RequireAdmin(this, db, out role);
                if (denied != null) return denied;

                string lang = CurrentActorHelper.ResolveLanguage(Request);
                var service = new MenuService(new EfMenuRepository(db));
                var result = service.SetMenuItemAvailability(role, id, isAvailable);
                if (!result.Succeeded) return HttpNotFound();

                TempData["StatusMessage"] = lang == "vi"
                    ? (isAvailable ? "Đã mở bán lại món ăn." : "Đã tạm ngưng phục vụ món ăn (BR12).")
                    : (isAvailable ? "Menu item marked Available." : "Menu item marked Unavailable (BR12).");
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
                var service = new MenuService(new EfMenuRepository(db));
                var result = service.SoftDeleteMenuItem(role, id);
                if (!result.Succeeded) return HttpNotFound();

                TempData["StatusMessage"] = lang == "vi"
                    ? "Đã ngừng bán (soft-delete) món ăn; lịch sử đơn cũ và báo cáo R1 vẫn được bảo toàn."
                    : "Menu item soft-deleted; historical orders and R1 revenue report remain intact.";
                return RedirectToAction("Index");
            }
        }
    }
}
