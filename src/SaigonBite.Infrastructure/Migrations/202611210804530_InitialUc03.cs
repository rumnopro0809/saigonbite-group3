namespace SaigonBite.Infrastructure.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialUc03 : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Brands",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
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
                        Name = c.String(nullable: false, maxLength: 100),
                        Location = c.String(nullable: false, maxLength: 200),
                        Capacity = c.Int(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        IsDeleted = c.Boolean(nullable: false),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        HubId = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        PlacedAt = c.DateTimeOffset(nullable: false, precision: 7),
                        QuietDiscountGranted = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Hubs", t => t.HubId)
                .Index(t => new { t.HubId, t.Status }, name: "IX_Orders_Hub_Status");
            
            CreateTable(
                "dbo.OrderStatusHistories",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        From = c.Int(nullable: false),
                        To = c.Int(nullable: false),
                        ChangedAt = c.DateTimeOffset(nullable: false, precision: 7),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Orders", t => t.OrderId)
                .Index(t => t.OrderId);
            
            CreateTable(
                "dbo.SystemSettings",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        IsHeavyRainOn = c.Boolean(nullable: false),
                        UpdatedAt = c.DateTimeOffset(nullable: false, precision: 7),
                        RowVersion = c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"),
                    })
                .PrimaryKey(t => t.Id);
            
            // Ràng buộc ở mức DB: công suất bếp phải lớn hơn 0 (BR8).
            Sql("ALTER TABLE dbo.Hubs ADD CONSTRAINT CK_Hubs_Capacity_Positive CHECK (Capacity > 0)");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderStatusHistories", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Orders", "HubId", "dbo.Hubs");
            DropForeignKey("dbo.HubBrands", "HubId", "dbo.Hubs");
            DropForeignKey("dbo.HubBrands", "BrandId", "dbo.Brands");
            DropIndex("dbo.OrderStatusHistories", new[] { "OrderId" });
            DropIndex("dbo.Orders", "IX_Orders_Hub_Status");
            DropIndex("dbo.HubBrands", new[] { "BrandId" });
            DropIndex("dbo.HubBrands", new[] { "HubId" });
            Sql("ALTER TABLE dbo.Hubs DROP CONSTRAINT CK_Hubs_Capacity_Positive");
            DropTable("dbo.SystemSettings");
            DropTable("dbo.OrderStatusHistories");
            DropTable("dbo.Orders");
            DropTable("dbo.Hubs");
            DropTable("dbo.HubBrands");
            DropTable("dbo.Brands");
        }
    }
}
