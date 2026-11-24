using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Migrations;
using SaigonBite.Infrastructure.Security;

namespace SaigonBite.Tests
{
    [TestClass]
    public class DatabaseSeedTests
    {
        [TestMethod]
        public void MigrateAndSeed_PopulatesSqlExpressSchemaWithExpectedStarterData()
        {
            Database.SetInitializer(
                new MigrateDatabaseToLatestVersion<SaigonBiteDbContext, Configuration>());

            using (var db = new SaigonBiteDbContext())
            {
                db.Database.Initialize(force: true);

                Assert.AreEqual(3, db.Hubs.Count());
                Assert.AreEqual(4, db.Brands.Count());
                Assert.AreEqual(12, db.HubBrands.Count());
                Assert.AreEqual(8, db.MenuItems.Count());
                Assert.AreEqual(2, db.Vouchers.Count());
                Assert.AreEqual(5, db.Users.Count());
                Assert.AreEqual(1, db.SystemSettings.Count());

                var admin = db.Users.Single(u => u.Identifier == "admin@saigonbite.vn");
                Assert.IsTrue(PasswordHasher.Verify("Saigon123!", admin.PasswordHash));
            }
        }
    }
}
