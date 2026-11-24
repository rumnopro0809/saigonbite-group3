using System;
using System.Collections.Generic;
using System.Linq;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Security;

namespace SaigonBite.Infrastructure.Data
{
    /// <summary>
    /// Idempotent starter data matching Part 2 specifications and Figma V2 Saigon dishes.
    /// </summary>
    public static class SaigonBiteSeeder
    {
        public static void Seed(SaigonBiteDbContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            if (!context.SystemSettings.Any())
            {
                context.SystemSettings.Add(new SystemSetting
                {
                    IsHeavyRainOn = false,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                context.SaveChanges();
            }

            Hub hub1;
            if (!context.Hubs.Any())
            {
                hub1 = new Hub
                {
                    Name = "District 1 Central Hub",
                    Address = "24 Nguyen Hue, Ben Nghe Ward, District 1, HCMC",
                    District = "District 1",
                    MaxCapacity = 40,
                    IsActive = true
                };
                var hub2 = new Hub
                {
                    Name = "Binh Thanh Kitchen Hub",
                    Address = "128 Dien Bien Phu, Ward 17, Binh Thanh, HCMC",
                    District = "Binh Thanh",
                    MaxCapacity = 30,
                    IsActive = true
                };
                var hub3 = new Hub
                {
                    Name = "Thu Duc Campus Hub",
                    Address = "19 Vo Van Ngan, Linh Chieu, Thu Duc, HCMC",
                    District = "Thu Duc",
                    MaxCapacity = 25,
                    IsActive = true
                };

                context.Hubs.AddRange(new[] { hub1, hub2, hub3 });
                context.SaveChanges();
            }
            else
            {
                hub1 = context.Hubs.OrderBy(h => h.Id).First();
            }

            if (!context.Brands.Any())
            {
                var b1 = new Brand
                {
                    Name = "Com Tam Saigon",
                    Category = "Vietnamese Rice",
                    Description = "Charcoal-grilled pork ribs and Saigon broken rice."
                };
                var b2 = new Brand
                {
                    Name = "Banh Mi Pho Co",
                    Category = "Banh Mi",
                    Description = "Crispy baguettes with traditional cold cuts and roast pork."
                };
                var b3 = new Brand
                {
                    Name = "Ga Ran & Fast Food",
                    Category = "Fast Food",
                    Description = "Crispy fried chicken combos prepared fresh per order."
                };
                var b4 = new Brand
                {
                    Name = "Bep Viet & Lau",
                    Category = "Noodles & Hotpot",
                    Description = "Fresh spring rolls, Thai hotpot, and crab glass noodles."
                };

                context.Brands.AddRange(new[] { b1, b2, b3, b4 });
                context.SaveChanges();

                var allHubs = context.Hubs.ToList();
                var allBrands = new[] { b1, b2, b3, b4 };
                foreach (Hub hub in allHubs)
                {
                    foreach (Brand brand in allBrands)
                    {
                        context.HubBrands.Add(new HubBrand
                        {
                            HubId = hub.Id,
                            BrandId = brand.Id
                        });
                    }
                }
                context.SaveChanges();

                var dishes = new List<MenuItem>
                {
                    new MenuItem
                    {
                        BrandId = b1.Id,
                        Name = "Com Tam Calmette",
                        Description = "Grilled pork chop, shredded pork skin, steamed egg meatloaf.",
                        Price = 95000m,
                        ImageUrl = "/Content/images/comtam.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b2.Id,
                        Name = "Banh Mi Huynh Hoa",
                        Description = "Signature Saigon baguette packed with pate, butter, and cold cuts.",
                        Price = 105000m,
                        ImageUrl = "/Content/images/banhmi.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b3.Id,
                        Name = "Combo Ga Ran KFC",
                        Description = "Two crispy chicken pieces with seasoned fries and soft drink.",
                        Price = 89000m,
                        ImageUrl = "/Content/images/hero.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b4.Id,
                        Name = "Goi Cuon Tay Son",
                        Description = "Fresh prawn and pork spring rolls with rich peanut sauce.",
                        Price = 59000m,
                        ImageUrl = "/Content/images/goicuon.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b1.Id,
                        Name = "Com Tam De Tham",
                        Description = "Broken rice with honey-glazed pork chop and pickled vegetables.",
                        Price = 75000m,
                        ImageUrl = "/Content/images/detham.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b2.Id,
                        Name = "Banh Mi Heo Quay",
                        Description = "Crispy-skin roast pork baguette with scallions and chili soy sauce.",
                        Price = 45000m,
                        ImageUrl = "/Content/images/heoquay.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b4.Id,
                        Name = "Lau Thai",
                        Description = "Individual spicy & sour Thai seafood hotpot cup with noodles.",
                        Price = 69000m,
                        ImageUrl = "/Content/images/lauthai.jpg",
                        IsAvailable = true
                    },
                    new MenuItem
                    {
                        BrandId = b4.Id,
                        Name = "Mien Xao Cua",
                        Description = "Wok-tossed glass noodles with sweet crab meat and herbs.",
                        Price = 115000m,
                        ImageUrl = "/Content/images/miencua.jpg",
                        IsAvailable = true
                    }
                };
                context.MenuItems.AddRange(dishes);
                context.SaveChanges();
            }

            if (!context.Vouchers.Any())
            {
                var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(7));
                var end = new DateTimeOffset(2027, 12, 31, 23, 59, 59, TimeSpan.FromHours(7));

                context.Vouchers.AddRange(new[]
                {
                    new Voucher
                    {
                        Code = "SAIGON15",
                        Type = VoucherType.Percent,
                        Value = 15m,
                        Cap = 40000m,
                        MinimumSubtotal = 100000m,
                        ValidFrom = start,
                        ValidTo = end,
                        IsActive = true
                    },
                    new Voucher
                    {
                        Code = "WELCOME20K",
                        Type = VoucherType.Fixed,
                        Value = 20000m,
                        Cap = 20000m,
                        MinimumSubtotal = 80000m,
                        ValidFrom = start,
                        ValidTo = end,
                        IsActive = true
                    }
                });
                context.SaveChanges();
            }

            if (!context.Users.Any())
            {
                string defaultHash = PasswordHasher.Hash("Saigon123!");

                var localCustomer = new User
                {
                    Identifier = "local@saigonbite.vn",
                    PasswordHash = defaultHash,
                    FullName = "Nguyen Minh Anh",
                    Role = ActorRole.Customer,
                    CustomerType = CustomerType.Local,
                    IsActive = true,
                    LocalProfile = new LocalProfile
                    {
                        Phone = "0901234567"
                    }
                };

                var touristCustomer = new User
                {
                    Identifier = "tourist@saigonbite.vn",
                    PasswordHash = defaultHash,
                    FullName = "Alex Johnson",
                    Role = ActorRole.Customer,
                    CustomerType = CustomerType.Tourist,
                    IsActive = true,
                    TouristProfile = new TouristProfile
                    {
                        PassportNumber = "P8821940",
                        Nationality = "Australia",
                        Email = "tourist@saigonbite.vn"
                    }
                };

                var kitchenStaff = new User
                {
                    Identifier = "kitchen.d1@saigonbite.vn",
                    PasswordHash = defaultHash,
                    FullName = "Tran Kitchen D1",
                    Role = ActorRole.KitchenStaff,
                    IsActive = true,
                    StaffProfile = new StaffProfile
                    {
                        HubId = hub1.Id
                    }
                };

                var shipper = new User
                {
                    Identifier = "shipper1@saigonbite.vn",
                    PasswordHash = defaultHash,
                    FullName = "Le Shipper 01",
                    Role = ActorRole.Shipper,
                    IsActive = true,
                    StaffProfile = new StaffProfile
                    {
                        HubId = hub1.Id
                    }
                };

                var admin = new User
                {
                    Identifier = "admin@saigonbite.vn",
                    PasswordHash = defaultHash,
                    FullName = "Admin SaigonBite",
                    Role = ActorRole.Admin,
                    IsActive = true
                };

                context.Users.AddRange(new[]
                {
                    localCustomer,
                    touristCustomer,
                    kitchenStaff,
                    shipper,
                    admin
                });
                context.SaveChanges();
            }
        }
    }
}
