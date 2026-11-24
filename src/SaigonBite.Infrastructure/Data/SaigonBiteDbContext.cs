using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Infrastructure.Data
{
    /// <summary>
    /// EF6 Code First DbContext mapping the 14 SaigonBite database tables.
    /// </summary>
    public class SaigonBiteDbContext : DbContext
    {
        // Keep EntityFramework.SqlServer loaded when deployed to Web or test bin folders.
        private static readonly Type EnsureSqlProviderLoaded = typeof(System.Data.Entity.SqlServer.SqlProviderServices);

        public SaigonBiteDbContext() : base("name=SaigonBiteDb")
        {
        }

        public SaigonBiteDbContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<LocalProfile> LocalProfiles { get; set; }
        public DbSet<TouristProfile> TouristProfiles { get; set; }
        public DbSet<StaffProfile> StaffProfiles { get; set; }
        public DbSet<Hub> Hubs { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<HubBrand> HubBrands { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }
        public DbSet<PaymentRecord> PaymentRecords { get; set; }
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique indexes
            modelBuilder.Entity<User>()
                .Property(u => u.Identifier)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Users_Identifier") { IsUnique = true }));

            modelBuilder.Entity<Voucher>()
                .Property(v => v.Code)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Vouchers_Code") { IsUnique = true }));

            modelBuilder.Entity<Order>()
                .Property(o => o.OrderCode)
                .HasColumnAnnotation(IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Orders_OrderCode") { IsUnique = true }));

            // Shared-PK 1:0..1 profile relationships
            modelBuilder.Entity<User>()
                .HasOptional(u => u.LocalProfile)
                .WithRequired(p => p.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasOptional(u => u.TouristProfile)
                .WithRequired(p => p.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<User>()
                .HasOptional(u => u.StaffProfile)
                .WithRequired(p => p.User)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<StaffProfile>()
                .HasRequired(s => s.Hub)
                .WithMany(h => h.StaffProfiles)
                .HasForeignKey(s => s.HubId)
                .WillCascadeOnDelete(false);

            // Composite key for HubBrands junction
            modelBuilder.Entity<HubBrand>()
                .HasKey(hb => new { hb.HubId, hb.BrandId });

            modelBuilder.Entity<HubBrand>()
                .HasRequired(hb => hb.Hub)
                .WithMany(h => h.HubBrands)
                .HasForeignKey(hb => hb.HubId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<HubBrand>()
                .HasRequired(hb => hb.Brand)
                .WithMany(b => b.HubBrands)
                .HasForeignKey(hb => hb.BrandId)
                .WillCascadeOnDelete(false);

            // Menu and Voucher decimals
            modelBuilder.Entity<MenuItem>().Property(m => m.Price).HasPrecision(19, 4);
            modelBuilder.Entity<MenuItem>()
                .HasRequired(m => m.Brand)
                .WithMany(b => b.MenuItems)
                .HasForeignKey(m => m.BrandId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Voucher>().Property(v => v.Value).HasPrecision(19, 4);
            modelBuilder.Entity<Voucher>().Property(v => v.Cap).HasPrecision(19, 4);
            modelBuilder.Entity<Voucher>().Property(v => v.MinimumSubtotal).HasPrecision(19, 4);

            // Order decimals and foreign keys (cascade delete disabled to protect audit trail)
            var order = modelBuilder.Entity<Order>();
            order.Property(o => o.DistanceKm).HasPrecision(19, 4);
            order.Property(o => o.HubLoadSnapshot).HasPrecision(19, 4);
            order.Property(o => o.Subtotal).HasPrecision(19, 4);
            order.Property(o => o.QuietDiscount).HasPrecision(19, 4);
            order.Property(o => o.VoucherDiscount).HasPrecision(19, 4);
            order.Property(o => o.FoodTotal).HasPrecision(19, 4);
            order.Property(o => o.DeliveryFee).HasPrecision(19, 4);
            order.Property(o => o.RushFee).HasPrecision(19, 4);
            order.Property(o => o.RainFee).HasPrecision(19, 4);
            order.Property(o => o.RawTotal).HasPrecision(19, 4);
            order.Property(o => o.PayableTotal).HasPrecision(19, 4);

            order.HasRequired(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .WillCascadeOnDelete(false);

            order.HasOptional(o => o.Shipper)
                .WithMany()
                .HasForeignKey(o => o.ShipperId)
                .WillCascadeOnDelete(false);

            order.HasRequired(o => o.Hub)
                .WithMany()
                .HasForeignKey(o => o.HubId)
                .WillCascadeOnDelete(false);

            order.HasOptional(o => o.Voucher)
                .WithMany()
                .HasForeignKey(o => o.VoucherId)
                .WillCascadeOnDelete(false);

            // OrderLine, PaymentRecord, and StatusHistory
            modelBuilder.Entity<OrderLine>().Property(l => l.UnitPriceSnapshot).HasPrecision(19, 4);
            modelBuilder.Entity<OrderLine>()
                .HasRequired(l => l.Order)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<OrderLine>()
                .HasRequired(l => l.MenuItem)
                .WithMany()
                .HasForeignKey(l => l.MenuItemId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<PaymentRecord>().Property(p => p.Amount).HasPrecision(19, 4);
            modelBuilder.Entity<PaymentRecord>()
                .HasRequired(p => p.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(p => p.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<OrderStatusHistory>()
                .HasRequired(h => h.Order)
                .WithMany(o => o.StatusHistory)
                .HasForeignKey(h => h.OrderId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<OrderStatusHistory>()
                .HasRequired(h => h.Actor)
                .WithMany()
                .HasForeignKey(h => h.ActorId)
                .WillCascadeOnDelete(false);
        }
    }
}
