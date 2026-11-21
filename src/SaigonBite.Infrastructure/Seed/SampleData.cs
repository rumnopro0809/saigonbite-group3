using System;
using System.Linq;
using SaigonBite.Domain;

namespace SaigonBite.Infrastructure
{
    /// <summary>
    /// Dữ liệu mẫu của UC03. Đây là dữ liệu giả để chạy thử giao diện, không phải dữ liệu thật.
    /// Seed chạy nhiều lần vẫn an toàn: chỉ thêm cái còn thiếu.
    /// </summary>
    public static class SampleData
    {
        private static readonly TimeSpan Vn = TimeSpan.FromHours(7);

        public static void Apply(AppDbContext context)
        {
            SeedBrands(context);
            SeedHubs(context);
            SeedRainSetting(context);
            SeedOrders(context);
        }

        // TẠM (UC02): brand mẫu để chọn khi tạo bếp. Thay bằng dữ liệu của UC02 khi có.
        private static void SeedBrands(AppDbContext context)
        {
            foreach (string name in new[] { "Phở", "Bún", "Bánh mì", "Cơm tấm", "Cà phê" })
            {
                if (!context.Brands.Any(b => b.Name == name))
                    context.Brands.Add(new Brand { Name = name });
            }
            context.SaveChanges();
        }

        // Ba bếp mẫu giống màn hình Figma F06: tải 20%, 90%, 20%.
        private static void SeedHubs(AppDbContext context)
        {
            AddHub(context, "Bếp Quận 1", "12 Lê Lợi, Quận 1", 20, new[] { "Phở", "Bún", "Bánh mì" });
            AddHub(context, "Bếp Bình Thạnh", "45 Điện Biên Phủ, Bình Thạnh", 20, new[] { "Phở", "Cơm tấm" });
            AddHub(context, "Bếp Thủ Đức", "8 Võ Văn Ngân, Thủ Đức", 25, new[] { "Bánh mì", "Cơm tấm", "Cà phê" });
            context.SaveChanges();
        }

        private static void AddHub(AppDbContext context, string name, string location, int capacity, string[] brandNames)
        {
            if (context.Hubs.Any(h => h.Name == name)) return;
            var hub = new Hub { Name = name, Location = location, Capacity = capacity, IsActive = true };
            foreach (string brandName in brandNames)
            {
                var brand = context.Brands.First(b => b.Name == brandName);
                hub.Brands.Add(new HubBrand { Brand = brand });
            }
            context.Hubs.Add(hub);
        }

        // FR16: cờ mưa mặc định là tắt.
        private static void SeedRainSetting(AppDbContext context)
        {
            if (context.SystemSettings.Any(s => s.Id == SystemSetting.SingleId)) return;
            context.SystemSettings.Add(new SystemSetting
            {
                Id = SystemSetting.SingleId,
                IsHeavyRainOn = false,
                UpdatedAt = new DateTimeOffset(2026, 11, 1, 0, 0, 0, Vn)
            });
            context.SaveChanges();
        }

        // TẠM (UC01, HT03): đơn mẫu để tính tải và báo cáo R2. Chỉ tạo khi chưa có đơn nào.
        private static void SeedOrders(AppDbContext context)
        {
            if (context.Orders.Any()) return;
            // Số liệu giống màn hình Figma F10: đã xử lý, phút chuẩn bị, lượt Quiet, đơn đang xử lý.
            AddOrders(context, "Bếp Quận 1", 42, 13, 16, 4);
            AddOrders(context, "Bếp Bình Thạnh", 24, 18, 2, 18);
            AddOrders(context, "Bếp Thủ Đức", 10, 11, 7, 5);
            context.SaveChanges();
        }

        private static void AddOrders(AppDbContext context, string hubName, int handled, int prepMinutes, int quiet, int active)
        {
            var hub = context.Hubs.First(h => h.Name == hubName);
            var start = new DateTimeOffset(2026, 11, 1, 8, 0, 0, Vn);
            int step = 9000 / handled; // rải đều các đơn trong khoảng 01 đến 07/11/2026

            for (int i = 0; i < handled; i++)
            {
                var placed = start.AddMinutes(i * step);
                var confirmed = placed.AddMinutes(2);

                // Đơn đầu tiên bị hủy sau khi được xác nhận: vẫn tính "đã xử lý" và vẫn tính lượt Quiet (AMB06).
                bool cancelled = i == 0;
                var order = new Order
                {
                    Hub = hub,
                    PlacedAt = placed,
                    Status = cancelled ? OrderStatus.Cancelled : OrderStatus.Delivered,
                    QuietDiscountGranted = i < quiet
                };
                context.Orders.Add(order);
                AddHistory(context, order, OrderStatus.Placed, OrderStatus.Confirmed, confirmed);
                if (cancelled)
                {
                    AddHistory(context, order, OrderStatus.Confirmed, OrderStatus.Cancelled, confirmed.AddMinutes(1));
                    continue;
                }
                // Thời gian chuẩn bị dao động quanh giá trị trung bình (-1, 0, +1 phút).
                int prep = prepMinutes + (i % 3 - 1);
                AddHistory(context, order, OrderStatus.Confirmed, OrderStatus.Preparing, confirmed.AddMinutes(1));
                AddHistory(context, order, OrderStatus.Preparing, OrderStatus.Ready, confirmed.AddMinutes(prep));
            }

            // Đơn đang xử lý (Confirmed hoặc Preparing) quyết định tải hiện tại (BR8).
            for (int k = 0; k < active; k++)
            {
                var placed = new DateTimeOffset(2026, 11, 20, 10, 0, 0, Vn).AddMinutes(k * 5);
                bool preparing = k % 2 == 1;
                var order = new Order
                {
                    Hub = hub,
                    PlacedAt = placed,
                    Status = preparing ? OrderStatus.Preparing : OrderStatus.Confirmed
                };
                context.Orders.Add(order);
                AddHistory(context, order, OrderStatus.Placed, OrderStatus.Confirmed, placed.AddMinutes(2));
                if (preparing)
                    AddHistory(context, order, OrderStatus.Confirmed, OrderStatus.Preparing, placed.AddMinutes(4));
            }
        }

        private static void AddHistory(AppDbContext context, Order order, OrderStatus from, OrderStatus to, DateTimeOffset at)
        {
            context.OrderStatusHistories.Add(new OrderStatusHistory { Order = order, From = from, To = to, ChangedAt = at });
        }
    }
}
