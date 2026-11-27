using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaigonBite.Application.Menu;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Repositories
{
    /// <summary>
    /// EF6 Code First repository implementing IMenuRepository for UC02 (FR08-FR11, BR12).
    /// Soft-delete only: never executes physical DELETE on Brands or MenuItems so OrderLines stay intact.
    /// </summary>
    public sealed class EfMenuRepository : IMenuRepository
    {
        private readonly SaigonBiteDbContext _db;

        public EfMenuRepository(SaigonBiteDbContext db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
        }

        public IList<Hub> GetActiveHubs()
        {
            return _db.Hubs
                .Where(h => !h.IsDeleted && h.IsActive)
                .OrderBy(h => h.Id)
                .ToList();
        }

        public IList<Brand> GetBrands(bool includeDeleted)
        {
            IQueryable<Brand> query = _db.Brands
                .Include(b => b.HubBrands)
                .Include(b => b.MenuItems);

            if (!includeDeleted)
            {
                query = query.Where(b => !b.IsDeleted);
            }

            return query
                .OrderBy(b => b.IsDeleted)
                .ThenBy(b => b.Name)
                .ToList();
        }

        public Brand GetBrandById(int id)
        {
            return _db.Brands
                .Include(b => b.HubBrands)
                .Include(b => b.MenuItems)
                .FirstOrDefault(b => b.Id == id);
        }

        public Brand AddBrand(Brand brand, IEnumerable<int> hubIds)
        {
            if (brand == null) throw new ArgumentNullException("brand");

            using (var tx = _db.Database.BeginTransaction())
            {
                _db.Brands.Add(brand);
                _db.SaveChanges();

                if (hubIds != null)
                {
                    foreach (int hubId in hubIds.Distinct())
                    {
                        _db.HubBrands.Add(new HubBrand
                        {
                            HubId = hubId,
                            BrandId = brand.Id
                        });
                    }
                    _db.SaveChanges();
                }

                tx.Commit();
            }

            return GetBrandById(brand.Id);
        }

        public void UpdateBrand(Brand brand, IEnumerable<int> hubIds)
        {
            if (brand == null) throw new ArgumentNullException("brand");

            using (var tx = _db.Database.BeginTransaction())
            {
                Brand tracked = _db.Brands
                    .Include(b => b.HubBrands)
                    .FirstOrDefault(b => b.Id == brand.Id);

                if (tracked == null)
                {
                    throw new InvalidOperationException("Brand not found.");
                }

                tracked.Name = brand.Name;
                tracked.Category = brand.Category;
                tracked.Description = brand.Description;
                tracked.IsActive = brand.IsActive;

                if (hubIds != null)
                {
                    var desired = new HashSet<int>(hubIds.Distinct());
                    var currentLinks = _db.HubBrands.Where(hb => hb.BrandId == tracked.Id).ToList();

                    foreach (HubBrand existingLink in currentLinks)
                    {
                        if (!desired.Contains(existingLink.HubId))
                        {
                            _db.HubBrands.Remove(existingLink);
                        }
                    }

                    var currentHubIds = new HashSet<int>(currentLinks.Select(hb => hb.HubId));
                    foreach (int hubId in desired)
                    {
                        if (!currentHubIds.Contains(hubId))
                        {
                            _db.HubBrands.Add(new HubBrand
                            {
                                HubId = hubId,
                                BrandId = tracked.Id
                            });
                        }
                    }
                }

                _db.SaveChanges();
                tx.Commit();
            }
        }

        public void SoftDeleteBrand(int id)
        {
            Brand tracked = _db.Brands.FirstOrDefault(b => b.Id == id);
            if (tracked == null) return;

            // Section 2E / FR11: Soft-delete only so OrderLine foreign keys and R1 remain valid.
            tracked.IsActive = false;
            tracked.IsDeleted = true;
            _db.SaveChanges();
        }

        public IList<MenuItem> GetMenuItems(MenuFilter filter)
        {
            MenuFilter effective = filter ?? new MenuFilter();

            IQueryable<MenuItem> query = _db.MenuItems
                .Include(m => m.Brand);

            if (!effective.IncludeDeleted)
            {
                query = query.Where(m => !m.IsDeleted);
            }

            if (effective.BrandId.HasValue && effective.BrandId.Value > 0)
            {
                int brandId = effective.BrandId.Value;
                query = query.Where(m => m.BrandId == brandId);
            }

            if (!string.IsNullOrWhiteSpace(effective.Category))
            {
                string category = effective.Category.Trim();
                query = query.Where(m => m.Brand != null && m.Brand.Category == category);
            }

            if (effective.IsAvailable.HasValue)
            {
                bool avail = effective.IsAvailable.Value;
                query = query.Where(m => m.IsAvailable == avail);
            }

            if (!string.IsNullOrWhiteSpace(effective.Search))
            {
                string keyword = effective.Search.Trim();
                query = query.Where(m =>
                    m.Name.Contains(keyword) ||
                    (m.Description != null && m.Description.Contains(keyword)) ||
                    (m.Brand != null && m.Brand.Name.Contains(keyword)));
            }

            return query
                .OrderBy(m => m.IsDeleted)
                .ThenBy(m => m.BrandId)
                .ThenBy(m => m.Name)
                .ToList();
        }

        public MenuItem GetMenuItemById(int id)
        {
            return _db.MenuItems
                .Include(m => m.Brand)
                .FirstOrDefault(m => m.Id == id);
        }

        public IList<MenuItem> GetCustomerMenuByHub(int hubId)
        {
            // FR09 / BR12: Only active, non-deleted, available items whose brand is linked to this active hub.
            return _db.MenuItems
                .Include(m => m.Brand)
                .Where(m =>
                    !m.IsDeleted &&
                    m.IsAvailable &&
                    m.Brand != null &&
                    !m.Brand.IsDeleted &&
                    m.Brand.IsActive &&
                    _db.HubBrands.Any(hb =>
                        hb.HubId == hubId &&
                        hb.BrandId == m.BrandId &&
                        !hb.Hub.IsDeleted &&
                        hb.Hub.IsActive))
                .OrderBy(m => m.Brand.Name)
                .ThenBy(m => m.Name)
                .ToList();
        }

        public bool IsItemOrderableAtHub(int hubId, int menuItemId)
        {
            return _db.MenuItems.Any(m =>
                m.Id == menuItemId &&
                !m.IsDeleted &&
                m.IsAvailable &&
                m.Brand != null &&
                !m.Brand.IsDeleted &&
                m.Brand.IsActive &&
                _db.HubBrands.Any(hb =>
                    hb.HubId == hubId &&
                    hb.BrandId == m.BrandId &&
                    !hb.Hub.IsDeleted &&
                    hb.Hub.IsActive));
        }

        public MenuItem AddMenuItem(MenuItem item)
        {
            if (item == null) throw new ArgumentNullException("item");

            _db.MenuItems.Add(item);
            _db.SaveChanges();
            return GetMenuItemById(item.Id);
        }

        public void UpdateMenuItem(MenuItem item)
        {
            if (item == null) throw new ArgumentNullException("item");

            MenuItem tracked = _db.MenuItems.FirstOrDefault(m => m.Id == item.Id);
            if (tracked == null)
            {
                throw new InvalidOperationException("Menu item not found.");
            }

            // FR10 / BR17: Never touches OrderLines; price changes affect future orders only.
            tracked.BrandId = item.BrandId;
            tracked.Name = item.Name;
            tracked.Description = item.Description;
            tracked.Price = item.Price;
            tracked.ImageUrl = item.ImageUrl;
            tracked.IsAvailable = item.IsAvailable;
            _db.SaveChanges();
        }

        public void SoftDeleteMenuItem(int id)
        {
            MenuItem tracked = _db.MenuItems.FirstOrDefault(m => m.Id == id);
            if (tracked == null) return;

            // Section 2E / FR11: Soft-delete only so OrderLine foreign keys remain valid.
            tracked.IsAvailable = false;
            tracked.IsDeleted = true;
            _db.SaveChanges();
        }
    }
}
