using System;
using System.Collections.Generic;
using System.Linq;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Menu
{
    /// <summary>
    /// UC02 Manage Menu and Brands business service (CRC06).
    /// Author: Duong Pham Thai (524H0189) | FR08-FR11, FR13, BR12, BR17, NFR02, NFR04.
    /// Catalog updates apply to future orders only and never mutate historical OrderLine snapshots.
    /// </summary>
    public sealed class MenuService
    {
        public const string DefaultImageUrl = "/Content/images/hero.jpg";

        private readonly IMenuRepository _repository;

        public MenuService(IMenuRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        // FR13 / NFR04: Enforce server-side System Admin authorization for all catalog write operations.
        private static void RequireAdmin(ActorRole actorRole)
        {
            if (actorRole != ActorRole.Admin)
            {
                throw new BusinessRuleException("FR13", "Only System Admin can manage brands and menu items.");
            }
        }

        public IList<Hub> GetActiveHubs()
        {
            return _repository.GetActiveHubs();
        }

        // FR09: Admin brand catalog listing (with optional soft-deleted visibility).
        public IList<Brand> GetAdminBrands(ActorRole actorRole, bool includeDeleted)
        {
            RequireAdmin(actorRole);
            return _repository.GetBrands(includeDeleted);
        }

        public Brand GetBrandDetails(ActorRole actorRole, int brandId)
        {
            RequireAdmin(actorRole);
            return _repository.GetBrandById(brandId);
        }

        // FR08: Create a new active virtual brand and optionally link it to serving hubs (HubBrand).
        public MenuResult<Brand> CreateBrand(ActorRole actorRole, BrandInput input)
        {
            RequireAdmin(actorRole);
            var errors = ValidateBrandInput(input);
            if (errors.Count > 0)
            {
                return MenuResult<Brand>.Fail("VALIDATION", errors);
            }

            var brand = new Brand
            {
                Name = input.Name.Trim(),
                Category = input.Category.Trim(),
                Description = NormalizeOptional(input.Description),
                IsActive = input.IsActive,
                IsDeleted = false
            };

            var validHubIds = FilterValidHubIds(input.HubIds);
            Brand created = _repository.AddBrand(brand, validHubIds);
            return MenuResult<Brand>.Ok(created);
        }

        // FR10: Update brand metadata, category (used by R1 Revenue Report), active state, and HubBrand links.
        public MenuResult<Brand> UpdateBrand(ActorRole actorRole, int brandId, BrandInput input)
        {
            RequireAdmin(actorRole);
            Brand existing = _repository.GetBrandById(brandId);
            if (existing == null || existing.IsDeleted)
            {
                return MenuResult<Brand>.Fail("NOT_FOUND", "Id", "Brand does not exist or has been soft-deleted.");
            }

            var errors = ValidateBrandInput(input);
            if (errors.Count > 0)
            {
                return MenuResult<Brand>.Fail("VALIDATION", errors);
            }

            existing.Name = input.Name.Trim();
            existing.Category = input.Category.Trim();
            existing.Description = NormalizeOptional(input.Description);
            existing.IsActive = input.IsActive;

            var validHubIds = FilterValidHubIds(input.HubIds);
            _repository.UpdateBrand(existing, validHubIds);
            return MenuResult<Brand>.Ok(existing);
        }

        // FR11 / Section 2E: Soft-delete brand so historical OrderLines and R1 Revenue Report remain intact.
        public MenuResult<bool> SoftDeleteBrand(ActorRole actorRole, int brandId)
        {
            RequireAdmin(actorRole);
            Brand existing = _repository.GetBrandById(brandId);
            if (existing == null)
            {
                return MenuResult<bool>.Fail("NOT_FOUND", "Id", "Brand was not found.");
            }

            _repository.SoftDeleteBrand(brandId);
            return MenuResult<bool>.Ok(true);
        }

        // FR09: Admin menu item listing with search/brand/category/availability/deleted filters.
        public IList<MenuItem> GetAdminMenuItems(ActorRole actorRole, MenuFilter filter)
        {
            RequireAdmin(actorRole);
            return _repository.GetMenuItems(filter ?? new MenuFilter { IncludeDeleted = true });
        }

        public MenuItem GetMenuItemDetails(ActorRole actorRole, int menuItemId)
        {
            RequireAdmin(actorRole);
            return _repository.GetMenuItemById(menuItemId);
        }

        // FR09 / BR12: Customer catalog scoped strictly to active brands served by the chosen hub.
        public IList<MenuItem> GetCustomerMenuByHub(int hubId)
        {
            if (hubId <= 0) return new List<MenuItem>();
            return _repository.GetCustomerMenuByHub(hubId);
        }

        // BR12: Authoritative server-side eligibility check for OrderService (UC01) at placement time.
        public bool IsItemOrderableAtHub(int hubId, int menuItemId)
        {
            if (hubId <= 0 || menuItemId <= 0) return false;
            return _repository.IsItemOrderableAtHub(hubId, menuItemId);
        }

        public void EnsureItemOrderableAtHub(int hubId, int menuItemId)
        {
            if (!IsItemOrderableAtHub(hubId, menuItemId))
            {
                throw new BusinessRuleException("BR12", "Menu item is unavailable, soft-deleted, or not served by the selected hub.");
            }
        }

        // FR08: Create menu item linked to an existing non-deleted brand.
        public MenuResult<MenuItem> CreateMenuItem(ActorRole actorRole, MenuItemInput input)
        {
            RequireAdmin(actorRole);
            var errors = ValidateMenuItemInput(input);
            if (errors.Count > 0)
            {
                return MenuResult<MenuItem>.Fail("VALIDATION", errors);
            }

            var item = new MenuItem
            {
                BrandId = input.BrandId,
                Name = input.Name.Trim(),
                Description = NormalizeOptional(input.Description),
                Price = input.Price,
                ImageUrl = NormalizeImageUrl(input.ImageUrl),
                IsAvailable = input.IsAvailable,
                IsDeleted = false
            };

            MenuItem created = _repository.AddMenuItem(item);
            return MenuResult<MenuItem>.Ok(created);
        }

        // FR10 / BR17: Update item metadata, unit price, or availability for future orders only.
        public MenuResult<MenuItem> UpdateMenuItem(ActorRole actorRole, int menuItemId, MenuItemInput input)
        {
            RequireAdmin(actorRole);
            MenuItem existing = _repository.GetMenuItemById(menuItemId);
            if (existing == null || existing.IsDeleted)
            {
                return MenuResult<MenuItem>.Fail("NOT_FOUND", "Id", "Menu item does not exist or has been soft-deleted.");
            }

            var errors = ValidateMenuItemInput(input);
            if (errors.Count > 0)
            {
                return MenuResult<MenuItem>.Fail("VALIDATION", errors);
            }

            existing.BrandId = input.BrandId;
            existing.Name = input.Name.Trim();
            existing.Description = NormalizeOptional(input.Description);
            existing.Price = input.Price;
            existing.ImageUrl = NormalizeImageUrl(input.ImageUrl);
            existing.IsAvailable = input.IsAvailable;

            _repository.UpdateMenuItem(existing);
            return MenuResult<MenuItem>.Ok(existing);
        }

        // FR10 / BR12: Toggle item availability without deleting the item.
        public MenuResult<MenuItem> SetMenuItemAvailability(ActorRole actorRole, int menuItemId, bool isAvailable)
        {
            RequireAdmin(actorRole);
            MenuItem existing = _repository.GetMenuItemById(menuItemId);
            if (existing == null || existing.IsDeleted)
            {
                return MenuResult<MenuItem>.Fail("NOT_FOUND", "Id", "Menu item does not exist or has been soft-deleted.");
            }

            existing.IsAvailable = isAvailable;
            _repository.UpdateMenuItem(existing);
            return MenuResult<MenuItem>.Ok(existing);
        }

        // FR11 / Section 2E: Soft-delete menu item while preserving OrderLine foreign keys and snapshots.
        public MenuResult<bool> SoftDeleteMenuItem(ActorRole actorRole, int menuItemId)
        {
            RequireAdmin(actorRole);
            MenuItem existing = _repository.GetMenuItemById(menuItemId);
            if (existing == null)
            {
                return MenuResult<bool>.Fail("NOT_FOUND", "Id", "Menu item was not found.");
            }

            _repository.SoftDeleteMenuItem(menuItemId);
            return MenuResult<bool>.Ok(true);
        }

        private IDictionary<string, string> ValidateBrandInput(BrandInput input)
        {
            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input == null)
            {
                errors[string.Empty] = "Brand data is required.";
                return errors;
            }

            string name = (input.Name ?? string.Empty).Trim();
            string category = (input.Category ?? string.Empty).Trim();

            if (name.Length == 0) errors["Name"] = "Brand name is required.";
            else if (name.Length > 120) errors["Name"] = "Brand name must be at most 120 characters.";

            if (category.Length == 0) errors["Category"] = "Brand category is required for R1 reporting.";
            else if (category.Length > 80) errors["Category"] = "Brand category must be at most 80 characters.";

            if (input.Description != null && input.Description.Trim().Length > 500)
            {
                errors["Description"] = "Description must be at most 500 characters.";
            }

            return errors;
        }

        private IDictionary<string, string> ValidateMenuItemInput(MenuItemInput input)
        {
            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input == null)
            {
                errors[string.Empty] = "Menu item data is required.";
                return errors;
            }

            string name = (input.Name ?? string.Empty).Trim();
            if (name.Length == 0) errors["Name"] = "Menu item name is required.";
            else if (name.Length > 120) errors["Name"] = "Menu item name must be at most 120 characters.";

            if (input.Price < 0m) errors["Price"] = "Unit price cannot be negative.";

            if (input.Description != null && input.Description.Trim().Length > 500)
            {
                errors["Description"] = "Description must be at most 500 characters.";
            }

            if (input.ImageUrl != null && input.ImageUrl.Trim().Length > 500)
            {
                errors["ImageUrl"] = "Image URL must be at most 500 characters.";
            }

            Brand brand = _repository.GetBrandById(input.BrandId);
            if (brand == null || brand.IsDeleted)
            {
                errors["BrandId"] = "Selected brand does not exist or has been soft-deleted.";
            }

            return errors;
        }

        private IList<int> FilterValidHubIds(IEnumerable<int> requestedHubIds)
        {
            if (requestedHubIds == null) return new List<int>();
            var activeHubIds = new HashSet<int>(_repository.GetActiveHubs().Select(h => h.Id));
            return requestedHubIds.Where(id => activeHubIds.Contains(id)).Distinct().ToList();
        }

        private static string NormalizeOptional(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim();
        }

        private static string NormalizeImageUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DefaultImageUrl;
            return value.Trim();
        }
    }
}
