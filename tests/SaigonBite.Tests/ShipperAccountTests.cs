using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Domain;
using SaigonBite.Infrastructure;
using SaigonBite.Infrastructure.Migrations;
using SaigonBite.Infrastructure.Security;

namespace SaigonBite.Tests
{
    /// <summary>HT03: tạo tài khoản shipper và đăng nhập với vai trò Shipper.</summary>
    [TestClass]
    public class ShipperAccountTests
    {
        private const string Password = "shipper-pass-1";

        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());
            using (var db = new AppDbContext()) db.Database.Initialize(force: false);
        }

        [TestMethod]
        public void CreateShipper_Success_PersistsShipperWithPbkdf2HashAndNoStaffProfile()
        {
            using (var f = new Fixture())
            {
                string id = f.Id("ship");
                User created = f.Service.CreateShipper("  " + id + "  ", "  Ship Tester  ", Password);

                Assert.AreEqual(ActorRole.Shipper, created.Role);
                using (var db = new AppDbContext())
                {
                    User stored = db.Users.Include(u => u.StaffProfile).Single(u => u.Identifier == id);
                    Assert.AreEqual(created.Id, stored.Id);
                    Assert.AreEqual(ActorRole.Shipper, stored.Role);
                    Assert.AreEqual("Ship Tester", stored.FullName);
                    Assert.IsTrue(stored.IsActive);
                    Assert.IsFalse(stored.IsDeleted);
                    Assert.IsNull(stored.StaffProfile);
                    Assert.AreEqual(0, db.StaffProfiles.Count(s => s.UserId == stored.Id));
                    Assert.AreNotEqual(Password, stored.PasswordHash);
                    Assert.IsTrue(stored.PasswordHash.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal));
                    Assert.IsTrue(PasswordHasher.Verify(Password, stored.PasswordHash));
                    Assert.IsFalse(PasswordHasher.Verify("wrong-password", stored.PasswordHash));
                }
            }
        }

        [TestMethod]
        public void CreateShipper_InvalidIdentifier_ThrowsAndLeavesNoUser()
        {
            using (var f = new Fixture())
            {
                f.AssertRejected("INVALID_IDENTIFIER", "ab");
                f.AssertRejected("INVALID_IDENTIFIER", "has space " + f.Suffix);
                f.AssertRejected("INVALID_IDENTIFIER", new string('x', StaffAccountService.IdentifierMaxLength + 1));
                f.AssertRejected("INVALID_IDENTIFIER", null);
                f.AssertRejected("INVALID_IDENTIFIER", "   ");
            }
        }

        [TestMethod]
        public void CreateShipper_InvalidName_ThrowsAndLeavesNoUser()
        {
            using (var f = new Fixture())
            {
                f.AssertRejected("INVALID_NAME", f.Id("noname"), fullName: "   ");
                f.AssertRejected("INVALID_NAME", f.Id("nullname"), fullName: null);
                f.AssertRejected("INVALID_NAME", f.Id("longname"), fullName: new string('n', StaffAccountService.NameMaxLength + 1));
            }
        }

        [TestMethod]
        public void CreateShipper_InvalidPassword_ThrowsAndLeavesNoUser()
        {
            using (var f = new Fixture())
            {
                f.AssertRejected("INVALID_PASSWORD", f.Id("shortpw"), password: "short");
                f.AssertRejected("INVALID_PASSWORD", f.Id("nullpw"), password: null);
                f.AssertRejected("INVALID_PASSWORD", f.Id("emptypw"), password: "");
            }
        }

        [TestMethod]
        public void CreateShipper_DuplicateIdentifier_ThrowsAndKeepsSingleUser()
        {
            using (var f = new Fixture())
            {
                string id = f.Id("dup");
                User first = f.Service.CreateShipper(id, "First Shipper", Password);

                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    f.Service.CreateShipper(id, "Second Shipper", Password));
                Assert.AreEqual("DUPLICATE_IDENTIFIER", ex.Rule);

                using (var db = new AppDbContext())
                {
                    User only = db.Users.Single(u => u.Identifier == id);
                    Assert.AreEqual(first.Id, only.Id);
                    Assert.AreEqual("First Shipper", only.FullName);
                }
            }
        }

        [TestMethod]
        public void CreateShipper_IdentifierOfSoftDeletedUser_ThrowsDuplicate()
        {
            using (var f = new Fixture())
            {
                string id = f.Id("gone");
                f.Service.CreateShipper(id, "Gone Shipper", Password);
                f.Db.Users.Single(u => u.Identifier == id).IsDeleted = true;
                f.Db.SaveChanges();

                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    f.Service.CreateShipper(id, "Reuse Attempt", Password));
                Assert.AreEqual("DUPLICATE_IDENTIFIER", ex.Rule);
            }
        }

        [TestMethod]
        public void Authenticate_Shipper_SucceedsWithTrimmedIdentifier()
        {
            using (var f = new Fixture())
            {
                string id = f.Id("login");
                f.Service.CreateShipper(id, "Login Shipper", Password);

                User user = f.Service.Authenticate("  " + id + "  ", Password, ActorRole.Shipper);
                Assert.IsNotNull(user);
                Assert.AreEqual(ActorRole.Shipper, user.Role);
                Assert.AreEqual(id, user.Identifier);
            }
        }

        [TestMethod]
        public void Authenticate_ShipperCredentialsWithOtherRole_ReturnsNull()
        {
            using (var f = new Fixture())
            {
                string id = f.Id("cross");
                f.Service.CreateShipper(id, "Cross Shipper", Password);

                Assert.IsNull(f.Service.Authenticate(id, Password, ActorRole.Admin));
                Assert.IsNull(f.Service.Authenticate(id, Password, ActorRole.KitchenStaff));
                Assert.IsNull(f.Service.Authenticate(id, Password, ActorRole.Customer));
            }
        }

        [TestMethod]
        public void Authenticate_AsShipper_RejectsKitchenAndAdminIdentifiers()
        {
            using (var f = new Fixture())
            {
                string kitchenId = f.Id("kit");
                f.Service.CreateKitchenStaff(kitchenId, "Kitchen Tester", Password, f.Hub.Id);

                Assert.IsNull(f.Service.Authenticate(kitchenId, Password, ActorRole.Shipper));

                SampleData.EnsureAdmin(f.Db);
                Assert.IsNull(f.Service.Authenticate("admin123", SampleData.SampleAdminPassword, ActorRole.Shipper));
            }
        }

        [TestMethod]
        public void Authenticate_Shipper_WrongPasswordUnknownIdentifierOrBlankInput_ReturnsNull()
        {
            using (var f = new Fixture())
            {
                string id = f.Id("pw");
                f.Service.CreateShipper(id, "Password Shipper", Password);

                Assert.IsNull(f.Service.Authenticate(id, "wrong-password", ActorRole.Shipper));
                Assert.IsNull(f.Service.Authenticate(id, "", ActorRole.Shipper));
                Assert.IsNull(f.Service.Authenticate(id, null, ActorRole.Shipper));
                Assert.IsNull(f.Service.Authenticate("   ", Password, ActorRole.Shipper));
                Assert.IsNull(f.Service.Authenticate(id + "-nope", Password, ActorRole.Shipper));
            }
        }

        [TestMethod]
        public void Authenticate_InactiveOrSoftDeletedShipper_ReturnsNull()
        {
            using (var f = new Fixture())
            {
                string inactiveId = f.Id("off");
                string deletedId = f.Id("del");
                string activeId = f.Id("on");
                f.Service.CreateShipper(inactiveId, "Inactive Shipper", Password);
                f.Service.CreateShipper(deletedId, "Deleted Shipper", Password);
                f.Service.CreateShipper(activeId, "Active Shipper", Password);

                f.Db.Users.Single(u => u.Identifier == inactiveId).IsActive = false;
                f.Db.Users.Single(u => u.Identifier == deletedId).IsDeleted = true;
                f.Db.SaveChanges();

                Assert.IsNull(f.Service.Authenticate(inactiveId, Password, ActorRole.Shipper));
                Assert.IsNull(f.Service.Authenticate(deletedId, Password, ActorRole.Shipper));
                Assert.IsNotNull(f.Service.Authenticate(activeId, Password, ActorRole.Shipper));
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly List<string> _identifiers = new List<string>();

            public string Suffix { get; private set; }
            public AppDbContext Db { get; private set; }
            public StaffAccountService Service { get; private set; }
            public Hub Hub { get; private set; }

            public Fixture()
            {
                Suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
                Db = new AppDbContext();
                Hub = new Hub
                {
                    Name = "Shipper account test hub " + Suffix,
                    Location = "Test kitchen address",
                    Capacity = 10,
                    IsActive = true
                };
                Db.Hubs.Add(Hub);
                Db.SaveChanges();
                Service = new StaffAccountService(Db);
            }

            public string Id(string prefix)
            {
                string id = prefix + "-" + Suffix;
                Track(id);
                return id;
            }

            public void Track(string identifier)
            {
                _identifiers.Add(identifier.ToLowerInvariant());
            }

            public void AssertRejected(string rule, string identifier, string fullName = "Valid Name",
                string password = Password)
            {
                if (identifier != null && identifier.Trim().Length > 0) Track(identifier.Trim());
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    Service.CreateShipper(identifier, fullName, password));
                Assert.AreEqual(rule, ex.Rule);
                if (identifier != null)
                {
                    string trimmed = identifier.Trim();
                    using (var db = new AppDbContext())
                        Assert.AreEqual(0, db.Users.Count(u => u.Identifier == trimmed));
                }
            }

            public void Dispose()
            {
                try
                {
                    Db.Dispose();
                    using (var db = new AppDbContext())
                    {
                        var users = db.Users.Where(u => _identifiers.Contains(u.Identifier)).ToList();
                        var userIds = users.Select(u => u.Id).ToList();
                        db.Set<StaffProfile>().RemoveRange(db.StaffProfiles.Where(s => userIds.Contains(s.UserId) || s.HubId == Hub.Id));
                        db.SaveChanges();
                        db.Set<User>().RemoveRange(users);
                        db.Hubs.Remove(db.Hubs.Single(h => h.Id == Hub.Id));
                        db.SaveChanges();
                    }
                }
                catch
                {
                    // Cleanup must not mask the assertion result.
                }
            }
        }
    }
}
