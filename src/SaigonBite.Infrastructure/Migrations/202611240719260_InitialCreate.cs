namespace SaigonBite.Infrastructure.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Brands",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 120),
                        Category = c.String(nullable: false, maxLength: 80),
                        Description = c.String(maxLength: 500),
                        IsActive = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.HubBrands",
                c => new
                    {
                        HubId = c.Int(nullable: false),
                        BrandId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.HubId, t.BrandId })
                .ForeignKey("dbo.Brands", t => t.BrandId)
                .ForeignKey("dbo.Hubs", t => t.HubId)
                .Index(t => t.HubId)
                .Index(t => t.BrandId);
            
            CreateTable(
                "dbo.Hubs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 120),
                        Address = c.String(nullable: false, maxLength: 250),
                        District = c.String(nullable: false, maxLength: 80),
                        MaxCapacity = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.StaffProfiles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        HubId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Hubs", t => t.HubId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.HubId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Identifier = c.String(nullable: false, maxLength: 120),
                        PasswordHash = c.String(nullable: false, maxLength: 128),
                        FullName = c.String(nullable: false, maxLength: 120),
                        Role = c.Int(nullable: false),
                        CustomerType = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        CreatedAt = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Identifier, unique: true, name: "IX_Users_Identifier");
            
            CreateTable(
                "dbo.LocalProfiles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        Phone = c.String(nullable: false, maxLength: 30),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.TouristProfiles",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        PassportNumber = c.String(nullable: false, maxLength: 40),
                        Nationality = c.String(nullable: false, maxLength: 80),
                        Email = c.String(nullable: false, maxLength: 120),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.MenuItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        BrandId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 120),
                        Description = c.String(maxLength: 500),
                        Price = c.Decimal(nullable: false, precision: 19, scale: 4),
                        ImageUrl = c.String(maxLength: 500),
                        IsAvailable = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Brands", t => t.BrandId)
                .Index(t => t.BrandId);
            
            CreateTable(
                "dbo.OrderLines",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        MenuItemId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        UnitPriceSnapshot = c.Decimal(nullable: false, precision: 19, scale: 4),
                        ItemNameSnapshot = c.String(nullable: false, maxLength: 120),
                        CategorySnapshot = c.String(nullable: false, maxLength: 80),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.MenuItems", t => t.MenuItemId)
                .ForeignKey("dbo.Orders", t => t.OrderId)
                .Index(t => t.OrderId)
                .Index(t => t.MenuItemId);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderCode = c.String(nullable: false, maxLength: 30),
                        CustomerId = c.Int(nullable: false),
                        HubId = c.Int(nullable: false),
                        VoucherId = c.Int(),
                        ShipperId = c.Int(),
                        Status = c.Int(nullable: false),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                        RequestKey = c.String(maxLength: 64),
                        DeliveryAddress = c.String(nullable: false, maxLength: 250),
                        DistanceKm = c.Decimal(nullable: false, precision: 19, scale: 4),
                        PlacedAt = c.DateTimeOffset(nullable: false, precision: 7),
                        HubLoadSnapshot = c.Decimal(nullable: false, precision: 19, scale: 4),
                        HeavyRainSnapshot = c.Boolean(nullable: false),
                        Subtotal = c.Decimal(nullable: false, precision: 19, scale: 4),
                        QuietDiscount = c.Decimal(nullable: false, precision: 19, scale: 4),
                        VoucherDiscount = c.Decimal(nullable: false, precision: 19, scale: 4),
                        FoodTotal = c.Decimal(nullable: false, precision: 19, scale: 4),
                        DeliveryFee = c.Decimal(nullable: false, precision: 19, scale: 4),
                        RushFee = c.Decimal(nullable: false, precision: 19, scale: 4),
                        RainFee = c.Decimal(nullable: false, precision: 19, scale: 4),
                        RawTotal = c.Decimal(nullable: false, precision: 19, scale: 4),
                        PayableTotal = c.Decimal(nullable: false, precision: 19, scale: 4),
                        PaymentMethod = c.Int(nullable: false),
                        PaymentStatus = c.String(nullable: false, maxLength: 40),
                        Notes = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.CustomerId)
                .ForeignKey("dbo.Hubs", t => t.HubId)
                .ForeignKey("dbo.Users", t => t.ShipperId)
                .ForeignKey("dbo.Vouchers", t => t.VoucherId)
                .Index(t => t.OrderCode, unique: true, name: "IX_Orders_OrderCode")
                .Index(t => t.CustomerId)
                .Index(t => t.HubId)
                .Index(t => t.VoucherId)
                .Index(t => t.ShipperId);
            
            CreateTable(
                "dbo.PaymentRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        Method = c.Int(nullable: false),
                        Operation = c.String(nullable: false, maxLength: 40),
                        Amount = c.Decimal(nullable: false, precision: 19, scale: 4),
                        Result = c.String(nullable: false, maxLength: 40),
                        CorrelationKey = c.String(maxLength: 64),
                        CreatedAt = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Orders", t => t.OrderId)
                .Index(t => t.OrderId);
            
            CreateTable(
                "dbo.OrderStatusHistories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        ActorId = c.Int(nullable: false),
                        FromStatus = c.Int(),
                        ToStatus = c.Int(nullable: false),
                        Reason = c.String(maxLength: 300),
                        CashCollected = c.Boolean(nullable: false),
                        ChangedAt = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.ActorId)
                .ForeignKey("dbo.Orders", t => t.OrderId)
                .Index(t => t.OrderId)
                .Index(t => t.ActorId);
            
            CreateTable(
                "dbo.Vouchers",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Code = c.String(nullable: false, maxLength: 40),
                        Type = c.Int(nullable: false),
                        Value = c.Decimal(nullable: false, precision: 19, scale: 4),
                        Cap = c.Decimal(nullable: false, precision: 19, scale: 4),
                        MinimumSubtotal = c.Decimal(nullable: false, precision: 19, scale: 4),
                        ValidFrom = c.DateTimeOffset(nullable: false, precision: 7),
                        ValidTo = c.DateTimeOffset(nullable: false, precision: 7),
                        IsActive = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Code, unique: true, name: "IX_Vouchers_Code");
            
            CreateTable(
                "dbo.SystemSettings",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        IsHeavyRainOn = c.Boolean(nullable: false),
                        UpdatedAt = c.DateTimeOffset(nullable: false, precision: 7),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderLines", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Orders", "VoucherId", "dbo.Vouchers");
            DropForeignKey("dbo.OrderStatusHistories", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.OrderStatusHistories", "ActorId", "dbo.Users");
            DropForeignKey("dbo.Orders", "ShipperId", "dbo.Users");
            DropForeignKey("dbo.PaymentRecords", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Orders", "HubId", "dbo.Hubs");
            DropForeignKey("dbo.Orders", "CustomerId", "dbo.Users");
            DropForeignKey("dbo.OrderLines", "MenuItemId", "dbo.MenuItems");
            DropForeignKey("dbo.MenuItems", "BrandId", "dbo.Brands");
            DropForeignKey("dbo.HubBrands", "HubId", "dbo.Hubs");
            DropForeignKey("dbo.TouristProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.StaffProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.LocalProfiles", "UserId", "dbo.Users");
            DropForeignKey("dbo.StaffProfiles", "HubId", "dbo.Hubs");
            DropForeignKey("dbo.HubBrands", "BrandId", "dbo.Brands");
            DropIndex("dbo.Vouchers", "IX_Vouchers_Code");
            DropIndex("dbo.OrderStatusHistories", new[] { "ActorId" });
            DropIndex("dbo.OrderStatusHistories", new[] { "OrderId" });
            DropIndex("dbo.PaymentRecords", new[] { "OrderId" });
            DropIndex("dbo.Orders", new[] { "ShipperId" });
            DropIndex("dbo.Orders", new[] { "VoucherId" });
            DropIndex("dbo.Orders", new[] { "HubId" });
            DropIndex("dbo.Orders", new[] { "CustomerId" });
            DropIndex("dbo.Orders", "IX_Orders_OrderCode");
            DropIndex("dbo.OrderLines", new[] { "MenuItemId" });
            DropIndex("dbo.OrderLines", new[] { "OrderId" });
            DropIndex("dbo.MenuItems", new[] { "BrandId" });
            DropIndex("dbo.TouristProfiles", new[] { "UserId" });
            DropIndex("dbo.LocalProfiles", new[] { "UserId" });
            DropIndex("dbo.Users", "IX_Users_Identifier");
            DropIndex("dbo.StaffProfiles", new[] { "HubId" });
            DropIndex("dbo.StaffProfiles", new[] { "UserId" });
            DropIndex("dbo.HubBrands", new[] { "BrandId" });
            DropIndex("dbo.HubBrands", new[] { "HubId" });
            DropTable("dbo.SystemSettings");
            DropTable("dbo.Vouchers");
            DropTable("dbo.OrderStatusHistories");
            DropTable("dbo.PaymentRecords");
            DropTable("dbo.Orders");
            DropTable("dbo.OrderLines");
            DropTable("dbo.MenuItems");
            DropTable("dbo.TouristProfiles");
            DropTable("dbo.LocalProfiles");
            DropTable("dbo.Users");
            DropTable("dbo.StaffProfiles");
            DropTable("dbo.Hubs");
            DropTable("dbo.HubBrands");
            DropTable("dbo.Brands");
        }
    }
}
