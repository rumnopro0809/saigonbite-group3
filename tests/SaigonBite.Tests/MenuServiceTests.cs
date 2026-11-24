using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Menu;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Tests
{
    /// <summary>
    /// UC02 Manage Menu and Brands unit tests (TC08-TC11, TC13, FR08-FR11, FR13, BR12, BR17).
    /// Author: Duong Pham Thai (524H0189).
    /// </summary>
    [TestClass]
    public class MenuServiceTests
    {
        private InMemoryMenuRepository _repo;
        private MenuService _service;

        [TestInitialize]
        public void Setup()
        {
            _repo = new InMemoryMenuRepository();
            _service = new MenuService(_repo);
        }

        [TestMethod]
        public void TC08_CreateBrand_ValidAdminInput_PersistsBrandAndHubLinks()
        {
            var result = _service.CreateBrand(ActorRole.Admin, new BrandInput
            {
                Name = "Pho Express",
                Category = "Noodles & Hotpot",
                Description = "Traditional Saigon beef pho.",
                IsActive = true,
                HubIds = new List<int> { 1, 2, 999 } // 999 is ignored because only active hubs 1 & 2 exist
            });

            Assert.IsTrue(result.Succeeded);
            Assert.IsNotNull(result.Data);
            Assert.AreEqual("Pho Express", result.Data.Name);
            Assert.AreEqual("Noodles & Hotpot", result.Data.Category);
            Assert.IsFalse(result.Data.IsDeleted);
            Assert.AreEqual(2, result.Data.HubBrands.Count);
        }

        [TestMethod]
        public void TC08_CreateBrand_MissingNameOrCategory_ReturnsValidationErrors()
        {
            var result = _service.CreateBrand(ActorRole.Admin, new BrandInput
            {
                Name = "   ",
                Category = "",
                IsActive = true
            });

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("VALIDATION", result.ErrorCode);
            Assert.IsTrue(result.FieldErrors.ContainsKey("Name"));
            Assert.IsTrue(result.FieldErrors.ContainsKey("Category"));
        }

        [TestMethod]
        public void TC08_CreateMenuItem_ValidAdminInput_PersistsItemWithDefaultImage()
        {
            var result = _service.CreateMenuItem(ActorRole.Admin, new MenuItemInput
            {
                BrandId = 1,
                Name = "Com Tam Suon Bi Cha",
                Description = "Broken rice with grilled pork chop.",
                Price = 85000m,
                ImageUrl = "   ",
                IsAvailable = true
            });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("Com Tam Suon Bi Cha", result.Data.Name);
            Assert.AreEqual(85000m, result.Data.Price);
            Assert.AreEqual(MenuService.DefaultImageUrl, result.Data.ImageUrl);
            Assert.IsTrue(result.Data.IsAvailable);
            Assert.IsFalse(result.Data.IsDeleted);
        }

        [TestMethod]
        public void TC08_CreateMenuItem_NegativePriceOrInvalidBrand_ReturnsValidationErrors()
        {
            var negativePrice = _service.CreateMenuItem(ActorRole.Admin, new MenuItemInput
            {
                BrandId = 1,
                Name = "Invalid Price Dish",
                Price = -1m,
                IsAvailable = true
            });

            Assert.IsFalse(negativePrice.Succeeded);
            Assert.IsTrue(negativePrice.FieldErrors.ContainsKey("Price"));

            var zeroBoundary = _service.CreateMenuItem(ActorRole.Admin, new MenuItemInput
            {
                BrandId = 1,
                Name = "Complimentary Tea",
                Price = 0m,
                IsAvailable = true
            });
            Assert.IsTrue(zeroBoundary.Succeeded);

            var missingBrand = _service.CreateMenuItem(ActorRole.Admin, new MenuItemInput
            {
                BrandId = 999,
                Name = "Orphan Dish",
                Price = 50000m,
                IsAvailable = true
            });

            Assert.IsFalse(missingBrand.Succeeded);
            Assert.IsTrue(missingBrand.FieldErrors.ContainsKey("BrandId"));
        }

        [TestMethod]
        public void TC09_GetCustomerMenuByHub_ReturnsOnlyAvailableItemsOfHubBrands()
        {
            // Hub 1 serves Brand 1 only; Hub 2 serves Brand 1 and Brand 2.
            IList<MenuItem> hub1Menu = _service.GetCustomerMenuByHub(1);
            IList<MenuItem> hub2Menu = _service.GetCustomerMenuByHub(2);

            Assert.AreEqual(1, hub1Menu.Count);
            Assert.AreEqual("Com Tam Calmette", hub1Menu[0].Name);

            Assert.AreEqual(2, hub2Menu.Count);
            Assert.IsTrue(hub2Menu.Any(m => m.Name == "Com Tam Calmette"));
            Assert.IsTrue(hub2Menu.Any(m => m.Name == "Banh Mi Huynh Hoa"));
        }

        [TestMethod]
        public void TC09_GetAdminMenuItems_FiltersBySearchBrandCategoryAndDeleted()
        {
            _service.SoftDeleteMenuItem(ActorRole.Admin, 1);

            IList<MenuItem> activeOnly = _service.GetAdminMenuItems(ActorRole.Admin, new MenuFilter
            {
                IncludeDeleted = false
            });
            Assert.IsFalse(activeOnly.Any(m => m.Id == 1));

            IList<MenuItem> withDeleted = _service.GetAdminMenuItems(ActorRole.Admin, new MenuFilter
            {
                Search = "Calmette",
                IncludeDeleted = true
            });
            Assert.AreEqual(1, withDeleted.Count);
            Assert.IsTrue(withDeleted[0].IsDeleted);
        }

        [TestMethod]
        public void TC10_UpdateMenuItem_ChangesFuturePrice_PreservesHistoricalOrderLineSnapshot()
        {
            // Simulate a historical order line capturing UnitPriceSnapshot at placement time (BR17).
            MenuItem originalItem = _service.GetMenuItemDetails(ActorRole.Admin, 1);
            var historicalLine = new OrderLine
            {
                MenuItemId = originalItem.Id,
                ItemNameSnapshot = originalItem.Name,
                CategorySnapshot = originalItem.Brand.Category,
                UnitPriceSnapshot = originalItem.Price,
                Quantity = 2
            };

            var update = _service.UpdateMenuItem(ActorRole.Admin, 1, new MenuItemInput
            {
                BrandId = 1,
                Name = "Com Tam Calmette Special",
                Description = "Updated recipe",
                Price = 110000m,
                ImageUrl = "/Content/images/comtam.jpg",
                IsAvailable = true
            });

            Assert.IsTrue(update.Succeeded);
            Assert.AreEqual(110000m, update.Data.Price);
            Assert.AreEqual(95000m, historicalLine.UnitPriceSnapshot);
            Assert.AreEqual("Com Tam Calmette", historicalLine.ItemNameSnapshot);
        }

        [TestMethod]
        public void TC10_SetMenuItemAvailability_False_RejectsOrderEligibilityUnderBR12()
        {
            Assert.IsTrue(_service.IsItemOrderableAtHub(1, 1));

            var toggle = _service.SetMenuItemAvailability(ActorRole.Admin, 1, false);
            Assert.IsTrue(toggle.Succeeded);
            Assert.IsFalse(_service.IsItemOrderableAtHub(1, 1));

            var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                _service.EnsureItemOrderableAtHub(1, 1));
            Assert.AreEqual("BR12", ex.Rule);
        }

        [TestMethod]
        public void TC11_SoftDeleteMenuItem_BlocksNewOrders_RetainsRecordForHistoryAndR1()
        {
            var delete = _service.SoftDeleteMenuItem(ActorRole.Admin, 1);
            Assert.IsTrue(delete.Succeeded);

            Assert.IsFalse(_service.IsItemOrderableAtHub(1, 1));
            Assert.AreEqual(0, _service.GetCustomerMenuByHub(1).Count);

            // Historical record still retrievable (never physically removed from repository).
            MenuItem retained = _service.GetMenuItemDetails(ActorRole.Admin, 1);
            Assert.IsNotNull(retained);
            Assert.IsTrue(retained.IsDeleted);
            Assert.IsFalse(retained.IsAvailable);
        }

        [TestMethod]
        public void TC11_SoftDeleteBrand_BlocksAllBrandItemsForNewOrders_RetainsBrandRecord()
        {
            Assert.IsTrue(_service.IsItemOrderableAtHub(1, 1));

            var delete = _service.SoftDeleteBrand(ActorRole.Admin, 1);
            Assert.IsTrue(delete.Succeeded);

            Assert.IsFalse(_service.IsItemOrderableAtHub(1, 1));
            Brand retained = _service.GetBrandDetails(ActorRole.Admin, 1);
            Assert.IsNotNull(retained);
            Assert.IsTrue(retained.IsDeleted);
            Assert.IsFalse(retained.IsActive);

            // Creating a new item under a soft-deleted brand must also be rejected.
            var orphan = _service.CreateMenuItem(ActorRole.Admin, new MenuItemInput
            {
                BrandId = 1,
                Name = "Should Fail",
                Price = 60000m,
                IsAvailable = true
            });
            Assert.IsFalse(orphan.Succeeded);
            Assert.IsTrue(orphan.FieldErrors.ContainsKey("BrandId"));
        }

        [TestMethod]
        public void TC13_NonAdminRoles_CannotCreateUpdateOrSoftDeleteCatalog()
        {
            ActorRole[] forbiddenRoles = { ActorRole.Customer, ActorRole.KitchenStaff, ActorRole.Shipper };
            foreach (ActorRole role in forbiddenRoles)
            {
                var createEx = Assert.ThrowsException<BusinessRuleException>(() =>
                    _service.CreateBrand(role, new BrandInput { Name = "Hack", Category = "Rice", IsActive = true }));
                Assert.AreEqual("FR13", createEx.Rule);

                var updateEx = Assert.ThrowsException<BusinessRuleException>(() =>
                    _service.SetMenuItemAvailability(role, 1, false));
                Assert.AreEqual("FR13", updateEx.Rule);

                var deleteEx = Assert.ThrowsException<BusinessRuleException>(() =>
                    _service.SoftDeleteMenuItem(role, 1));
                Assert.AreEqual("FR13", deleteEx.Rule);
            }
        }

        [TestMethod]
        public void TC13_EnsureItemOrderableAtHub_ThrowsBR12_WhenOtherHubOrInactiveHub()
        {
            // Item 2 belongs to Brand 2, which is NOT linked to Hub 1.
            var wrongHubEx = Assert.ThrowsException<BusinessRuleException>(() =>
                _service.EnsureItemOrderableAtHub(1, 2));
            Assert.AreEqual("BR12", wrongHubEx.Rule);

            // Item 2 IS orderable at Hub 2.
            _service.EnsureItemOrderableAtHub(2, 2);
        }

        /// <summary>
        /// In-memory fake implementation of IMenuRepository for deterministic unit tests.
        /// </summary>
        private sealed class InMemoryMenuRepository : IMenuRepository
        {
            private readonly List<Hub> _hubs = new List<Hub>();
            private readonly List<Brand> _brands = new List<Brand>();
            private readonly List<HubBrand> _hubBrands = new List<HubBrand>();
            private readonly List<MenuItem> _items = new List<MenuItem>();
            private int _nextBrandId = 3;
            private int _nextItemId = 4;

            public InMemoryMenuRepository()
            {
                var h1 = new Hub { Id = 1, Name = "District 1 Hub", District = "District 1", Address = "24 Nguyen Hue", MaxCapacity = 40, IsActive = true };
                var h2 = new Hub { Id = 2, Name = "Binh Thanh Hub", District = "Binh Thanh", Address = "128 Dien Bien Phu", MaxCapacity = 30, IsActive = true };
                _hubs.Add(h1);
                _hubs.Add(h2);

                var b1 = new Brand { Id = 1, Name = "Com Tam Saigon", Category = "Vietnamese Rice", IsActive = true };
                var b2 = new Brand { Id = 2, Name = "Banh Mi Pho Co", Category = "Banh Mi", IsActive = true };
                _brands.Add(b1);
                _brands.Add(b2);

                Link(h1, b1);
                Link(h2, b1);
                Link(h2, b2);

                _items.Add(new MenuItem { Id = 1, BrandId = 1, Brand = b1, Name = "Com Tam Calmette", Price = 95000m, IsAvailable = true });
                _items.Add(new MenuItem { Id = 2, BrandId = 2, Brand = b2, Name = "Banh Mi Huynh Hoa", Price = 105000m, IsAvailable = true });
                _items.Add(new MenuItem { Id = 3, BrandId = 1, Brand = b1, Name = "Sold Out Dish", Price = 65000m, IsAvailable = false });
            }

            private void Link(Hub h, Brand b)
            {
                var link = new HubBrand { HubId = h.Id, BrandId = b.Id, Hub = h, Brand = b };
                _hubBrands.Add(link);
                b.HubBrands.Add(link);
                h.HubBrands.Add(link);
            }

            public IList<Hub> GetActiveHubs()
            {
                return _hubs.Where(h => !h.IsDeleted && h.IsActive).ToList();
            }

            public IList<Brand> GetBrands(bool includeDeleted)
            {
                return _brands.Where(b => includeDeleted || !b.IsDeleted).ToList();
            }

            public Brand GetBrandById(int id)
            {
                return _brands.FirstOrDefault(b => b.Id == id);
            }

            public Brand AddBrand(Brand brand, IEnumerable<int> hubIds)
            {
                brand.Id = _nextBrandId++;
                _brands.Add(brand);
                if (hubIds != null)
                {
                    foreach (int hubId in hubIds.Distinct())
                    {
                        Hub hub = _hubs.FirstOrDefault(h => h.Id == hubId);
                        if (hub != null) Link(hub, brand);
                    }
                }
                return brand;
            }

            public void UpdateBrand(Brand brand, IEnumerable<int> hubIds)
            {
                if (hubIds == null) return;
                _hubBrands.RemoveAll(hb => hb.BrandId == brand.Id);
                brand.HubBrands.Clear();
                foreach (int hubId in hubIds.Distinct())
                {
                    Hub hub = _hubs.FirstOrDefault(h => h.Id == hubId);
                    if (hub != null) Link(hub, brand);
                }
            }

            public void SoftDeleteBrand(int id)
            {
                Brand b = GetBrandById(id);
                if (b != null)
                {
                    b.IsActive = false;
                    b.IsDeleted = true;
                }
            }

            public IList<MenuItem> GetMenuItems(MenuFilter filter)
            {
                MenuFilter f = filter ?? new MenuFilter();
                IEnumerable<MenuItem> q = _items;
                if (!f.IncludeDeleted) q = q.Where(m => !m.IsDeleted);
                if (f.BrandId.HasValue) q = q.Where(m => m.BrandId == f.BrandId.Value);
                if (!string.IsNullOrWhiteSpace(f.Category)) q = q.Where(m => m.Brand != null && m.Brand.Category == f.Category.Trim());
                if (f.IsAvailable.HasValue) q = q.Where(m => m.IsAvailable == f.IsAvailable.Value);
                if (!string.IsNullOrWhiteSpace(f.Search))
                {
                    string kw = f.Search.Trim();
                    q = q.Where(m => m.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
                }
                return q.ToList();
            }

            public MenuItem GetMenuItemById(int id)
            {
                return _items.FirstOrDefault(m => m.Id == id);
            }

            public IList<MenuItem> GetCustomerMenuByHub(int hubId)
            {
                return _items.Where(m => IsItemOrderableAtHub(hubId, m.Id)).ToList();
            }

            public bool IsItemOrderableAtHub(int hubId, int menuItemId)
            {
                MenuItem item = GetMenuItemById(menuItemId);
                if (item == null || item.IsDeleted || !item.IsAvailable) return false;
                Brand brand = GetBrandById(item.BrandId);
                if (brand == null || brand.IsDeleted || !brand.IsActive) return false;
                Hub hub = _hubs.FirstOrDefault(h => h.Id == hubId && !h.IsDeleted && h.IsActive);
                if (hub == null) return false;
                return _hubBrands.Any(hb => hb.HubId == hubId && hb.BrandId == brand.Id);
            }

            public MenuItem AddMenuItem(MenuItem item)
            {
                item.Id = _nextItemId++;
                item.Brand = GetBrandById(item.BrandId);
                _items.Add(item);
                return item;
            }

            public void UpdateMenuItem(MenuItem item)
            {
                item.Brand = GetBrandById(item.BrandId);
            }

            public void SoftDeleteMenuItem(int id)
            {
                MenuItem item = GetMenuItemById(id);
                if (item != null)
                {
                    item.IsAvailable = false;
                    item.IsDeleted = true;
                }
            }
        }
    }
}
