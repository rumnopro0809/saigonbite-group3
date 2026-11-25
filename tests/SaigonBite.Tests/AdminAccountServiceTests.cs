using System;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Migrations;
using SaigonBite.Infrastructure.Security;

namespace SaigonBite.Tests
{
    [TestClass]
    public class AdminAccountServiceTests
    {
        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());
            using (var db = new AppDbContext()) db.Database.Initialize(force: false);
        }

        [TestMethod]
        public void SampleAdmin_AuthenticatesOnlyWithItsOwnPassword()
        {
            using (var db = new AppDbContext())
            {
                SampleData.EnsureAdmin(db);
                var service = new AdminAccountService(db);

                User admin = service.Authenticate(" admin123 ", "sgbite123");
                Assert.IsNotNull(admin);
                Assert.AreEqual(ActorRole.Admin, admin.Role);
                StringAssert.StartsWith(admin.PasswordHash, "PBKDF2-SHA256$");
                Assert.IsNull(service.Authenticate("admin123", "wrong-password"));
                Assert.IsNull(service.Authenticate("admin123", ""));
                Assert.IsNull(service.Authenticate("no-such-admin", "sgbite123"));
            }
        }

        [TestMethod]
        public void SeedingTwice_DoesNotDuplicateOrResetTheAdmin()
        {
            using (var db = new AppDbContext())
            {
                SampleData.EnsureAdmin(db);
                string hash = db.Users.Single(u => u.Identifier == "admin123").PasswordHash;
                SampleData.EnsureAdmin(db);
                Assert.AreEqual(1, db.Users.Count(u => u.Identifier == "admin123"));
                Assert.AreEqual(hash, db.Users.Single(u => u.Identifier == "admin123").PasswordHash);
            }
        }

        [TestMethod]
        public void NonAdminInactiveAndDeletedAccounts_CannotUseAdminLogin()
        {
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            using (var db = new AppDbContext())
            {
                var customer = NewUser("cust-" + suffix, ActorRole.Customer, true, false);
                customer.CustomerType = CustomerType.Local;
                var inactive = NewUser("off-" + suffix, ActorRole.Admin, false, false);
                var deleted = NewUser("del-" + suffix, ActorRole.Admin, true, true);
                var active = NewUser("on-" + suffix, ActorRole.Admin, true, false);
                db.Set<User>().AddRange(new[] { customer, inactive, deleted, active });
                db.SaveChanges();
                try
                {
                    var service = new AdminAccountService(db);
                    Assert.IsNull(service.Authenticate(customer.Identifier, "sgbite123"));
                    Assert.IsNull(service.Authenticate(inactive.Identifier, "sgbite123"));
                    Assert.IsNull(service.Authenticate(deleted.Identifier, "sgbite123"));
                    Assert.IsNotNull(service.Authenticate(active.Identifier, "sgbite123"));
                }
                finally
                {
                    db.Set<User>().RemoveRange(new[] { customer, inactive, deleted, active });
                    db.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void AdminAccount_IsRejectedByCustomerLogin()
        {
            using (var db = new AppDbContext())
            {
                SampleData.EnsureAdmin(db);
                Assert.IsNull(new CustomerAccountService(db).Authenticate("admin123", "sgbite123"));
            }
        }

        private static User NewUser(string identifier, ActorRole role, bool active, bool deleted)
        {
            return new User
            {
                Identifier = identifier,
                FullName = "Admin auth test",
                PasswordHash = PasswordHasher.Hash("sgbite123"),
                Role = role,
                IsActive = active,
                IsDeleted = deleted
            };
        }
    }
}
