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
    [TestClass]
    public class StaffAccountServiceTests
    {
        private const string Password = "kitchen-pass-1";

        [ClassInitialize]
        public static void EnsureDatabaseMigrated(TestContext context)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AppDbContext, Configuration>());
            using (var db = new AppDbContext()) db.Database.Initialize(force: false);
        }

        [TestMethod]
        public void SampleAdmin_AuthenticatesAsAdminButNotAsKitchenStaff()
        {
            using (var db = new AppDbContext())
            {
                SampleData.EnsureAdmin(db);
                var service = new StaffAccountService(db);

                User admin = service.Authenticate("admin123", SampleData.SampleAdminPassword, ActorRole.Admin);
                Assert.IsNotNull(admin);
                Assert.AreEqual(ActorRole.Admin, admin.Role);
                Assert.IsNull(service.Authenticate("admin123", SampleData.SampleAdminPassword, ActorRole.KitchenStaff));
                Assert.IsNull(service.Authenticate("admin123", "wrong-password", ActorRole.Admin));
            }
        }

        [TestMethod]
        public void Authenticate_RoleOtherThanAdminOrKitchenStaff_ReturnsNull()
        {
            using (var fixture = new Fixture())
            {
                User staff = fixture.Service.CreateKitchenStaff(fixture.Id("role"), "Role Tester", Password, fixture.Hub.Id);
                foreach (ActorRole role in new[] { ActorRole.Customer, ActorRole.Shipper })
                    Assert.IsNull(fixture.Service.Authenticate(staff.Identifier, Password, role));
            }
        }

        [TestMethod]
        public void CreatedKitchenStaff_AuthenticatesWithTrimmedIdentifierOnly()
        {
            using (var fixture = new Fixture())
            {
                string id = fixture.Id("cook");
                fixture.Service.CreateKitchenStaff(id, "Cook Tester", Password, fixture.Hub.Id);

                User staff = fixture.Service.Authenticate("  " + id + "  ", Password, ActorRole.KitchenStaff);
                Assert.IsNotNull(staff);
                Assert.AreEqual(ActorRole.KitchenStaff, staff.Role);
                Assert.IsNull(fixture.Service.Authenticate(id, Password, ActorRole.Admin));
                Assert.IsNull(fixture.Service.Authenticate(id, "wrong-password", ActorRole.KitchenStaff));
                Assert.IsNull(fixture.Service.Authenticate(id, "", ActorRole.KitchenStaff));
                Assert.IsNull(fixture.Service.Authenticate(id + "-nope", Password, ActorRole.KitchenStaff));
            }
        }

        [TestMethod]
        public void Authenticate_InactiveSoftDeletedAndProfilelessStaff_ReturnsNull()
        {
            using (var fixture = new Fixture())
            {
                string inactiveId = fixture.Id("off");
                string deletedId = fixture.Id("del");
                string noProfileId = fixture.Id("nop");
                string activeId = fixture.Id("on");
                fixture.Service.CreateKitchenStaff(inactiveId, "Inactive", Password, fixture.Hub.Id);
                fixture.Service.CreateKitchenStaff(deletedId, "Deleted", Password, fixture.Hub.Id);
                fixture.Service.CreateKitchenStaff(activeId, "Active", Password, fixture.Hub.Id);

                User inactive = fixture.Db.Users.Single(u => u.Identifier == inactiveId);
                inactive.IsActive = false;
                User deleted = fixture.Db.Users.Single(u => u.Identifier == deletedId);
                deleted.IsDeleted = true;
                fixture.Db.Users.Add(new User
                {
                    Identifier = noProfileId,
                    FullName = "No profile",
                    PasswordHash = PasswordHasher.Hash(Password),
                    Role = ActorRole.KitchenStaff,
                    IsActive = true
                });
                fixture.Db.SaveChanges();

                Assert.IsNull(fixture.Service.Authenticate(inactiveId, Password, ActorRole.KitchenStaff));
                Assert.IsNull(fixture.Service.Authenticate(deletedId, Password, ActorRole.KitchenStaff));
                Assert.IsNull(fixture.Service.Authenticate(noProfileId, Password, ActorRole.KitchenStaff));
                Assert.IsNotNull(fixture.Service.Authenticate(activeId, Password, ActorRole.KitchenStaff));
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_Success_PersistsActiveUserWithPbkdf2HashAndProfile()
        {
            using (var fixture = new Fixture())
            {
                string id = fixture.Id("ok");
                User created = fixture.Service.CreateKitchenStaff("  " + id + " ", "  Nguyen Van A  ", Password, fixture.Hub.Id);
                Assert.IsNotNull(created);

                using (var db = new AppDbContext())
                {
                    User stored = db.Users.Include(u => u.StaffProfile).Single(u => u.Identifier == id);
                    Assert.AreEqual(ActorRole.KitchenStaff, stored.Role);
                    Assert.IsTrue(stored.IsActive);
                    Assert.IsFalse(stored.IsDeleted);
                    Assert.AreEqual("Nguyen Van A", stored.FullName);
                    Assert.AreNotEqual(Password, stored.PasswordHash);
                    StringAssert.StartsWith(stored.PasswordHash, "PBKDF2-SHA256$");
                    Assert.IsFalse(stored.PasswordHash.Contains(Password));
                    Assert.IsNotNull(stored.StaffProfile);
                    Assert.AreEqual(fixture.Hub.Id, stored.StaffProfile.HubId);
                }
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_IdentifierBoundaries_RejectsTwoAndFortyOneAcceptsThreeAndForty()
        {
            using (var fixture = new Fixture())
            {
                string seed = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                string two = seed.Substring(0, 2);
                string three = seed.Substring(0, 3);
                string forty = seed.Substring(0, 40);
                string fortyOne = seed.Substring(0, 41);

                fixture.AssertRejected("INVALID_IDENTIFIER", two);
                fixture.AssertRejected("INVALID_IDENTIFIER", fortyOne);

                fixture.Track(three);
                fixture.Track(forty);
                Assert.IsNotNull(fixture.Service.CreateKitchenStaff(three, "Boundary Three", Password, fixture.Hub.Id));
                Assert.IsNotNull(fixture.Service.CreateKitchenStaff(forty, "Boundary Forty", Password, fixture.Hub.Id));
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_InvalidIdentifierCharacters_ThrowsInvalidIdentifier()
        {
            using (var fixture = new Fixture())
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 8);
                fixture.AssertRejected("INVALID_IDENTIFIER", "has space " + suffix);
                fixture.AssertRejected("INVALID_IDENTIFIER", "bad@" + suffix);
                fixture.AssertRejected("INVALID_IDENTIFIER", "   ");
                fixture.AssertRejected("INVALID_IDENTIFIER", null);
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_ValidIdentifierPunctuation_IsAccepted()
        {
            using (var fixture = new Fixture())
            {
                string id = "a.b_c-" + Guid.NewGuid().ToString("N").Substring(0, 10);
                fixture.Track(id);
                Assert.IsNotNull(fixture.Service.CreateKitchenStaff(id, "Punctuation", Password, fixture.Hub.Id));
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_BlankOrTooLongName_ThrowsInvalidName()
        {
            using (var fixture = new Fixture())
            {
                fixture.AssertRejected("INVALID_NAME", fixture.Id("n1"), fullName: "");
                fixture.AssertRejected("INVALID_NAME", fixture.Id("n2"), fullName: "    ");
                fixture.AssertRejected("INVALID_NAME", fixture.Id("n3"), fullName: null);
                fixture.AssertRejected("INVALID_NAME", fixture.Id("n4"), fullName: new string('x', 121));

                string okId = fixture.Id("n5");
                Assert.IsNotNull(fixture.Service.CreateKitchenStaff(okId, new string('x', 120), Password, fixture.Hub.Id));
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_PasswordBoundary_RejectsSevenAcceptsEight()
        {
            using (var fixture = new Fixture())
            {
                fixture.AssertRejected("INVALID_PASSWORD", fixture.Id("p7"), password: "1234567");
                fixture.AssertRejected("INVALID_PASSWORD", fixture.Id("p0"), password: "");
                fixture.AssertRejected("INVALID_PASSWORD", fixture.Id("pn"), password: null);

                string okId = fixture.Id("p8");
                Assert.IsNotNull(fixture.Service.CreateKitchenStaff(okId, "Password Eight", "12345678", fixture.Hub.Id));
                Assert.IsNotNull(fixture.Service.Authenticate(okId, "12345678", ActorRole.KitchenStaff));
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_DuplicateIdentifier_IsCaseInsensitiveAndKeepsOriginal()
        {
            using (var fixture = new Fixture())
            {
                string id = fixture.Id("dup").ToLowerInvariant();
                User first = fixture.Service.CreateKitchenStaff(id, "First", Password, fixture.Hub.Id);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    fixture.Service.CreateKitchenStaff(id.ToUpperInvariant(), "Second", Password, fixture.Hub.Id));
                Assert.AreEqual("DUPLICATE_IDENTIFIER", ex.Rule);

                using (var db = new AppDbContext())
                {
                    Assert.AreEqual(1, db.Users.Count(u => u.Identifier == id));
                    Assert.AreEqual("First", db.Users.Single(u => u.Identifier == id).FullName);
                }
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_DuplicateOfAdminIdentifierDifferentCase_ThrowsDuplicate()
        {
            using (var fixture = new Fixture())
            {
                SampleData.EnsureAdmin(fixture.Db);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    fixture.Service.CreateKitchenStaff("ADMIN123", "Imposter", Password, fixture.Hub.Id));
                Assert.AreEqual("DUPLICATE_IDENTIFIER", ex.Rule);
                using (var db = new AppDbContext())
                {
                    Assert.AreEqual(1, db.Users.Count(u => u.Identifier == "admin123"));
                    Assert.AreEqual(ActorRole.Admin, db.Users.Single(u => u.Identifier == "admin123").Role);
                    Assert.AreEqual(0, db.StaffProfiles.Count(s => s.HubId == fixture.Hub.Id));
                }
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_DuplicateOfSoftDeletedUser_ThrowsDuplicate()
        {
            using (var fixture = new Fixture())
            {
                string id = fixture.Id("gone");
                fixture.Track(id);
                fixture.Db.Users.Add(new User
                {
                    Identifier = id,
                    FullName = "Deleted earlier",
                    PasswordHash = PasswordHasher.Hash(Password),
                    Role = ActorRole.KitchenStaff,
                    IsActive = false,
                    IsDeleted = true
                });
                fixture.Db.SaveChanges();

                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    fixture.Service.CreateKitchenStaff(id.ToUpperInvariant(), "Again", Password, fixture.Hub.Id));
                Assert.AreEqual("DUPLICATE_IDENTIFIER", ex.Rule);
                using (var db = new AppDbContext())
                {
                    Assert.AreEqual(1, db.Users.Count(u => u.Identifier == id));
                    Assert.AreEqual(0, db.StaffProfiles.Count(s => s.HubId == fixture.Hub.Id));
                }
            }
        }

        [TestMethod]
        public void CreateKitchenStaff_MissingOrSoftDeletedHub_ThrowsHubNotFound()
        {
            using (var fixture = new Fixture())
            {
                string missingHubId = fixture.Id("nohub");
                int maxHubId = fixture.Db.Hubs.Max(h => h.Id);
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    fixture.Service.CreateKitchenStaff(missingHubId, "No Hub", Password, maxHubId + 1000));
                Assert.AreEqual("HUB_NOT_FOUND", ex.Rule);
                fixture.AssertNoUser(missingHubId);

                fixture.Hub.IsDeleted = true;
                fixture.Db.SaveChanges();
                string deletedHubId = fixture.Id("delhub");
                ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    fixture.Service.CreateKitchenStaff(deletedHubId, "Deleted Hub", Password, fixture.Hub.Id));
                Assert.AreEqual("HUB_NOT_FOUND", ex.Rule);
                fixture.AssertNoUser(deletedHubId);
                using (var db = new AppDbContext())
                    Assert.AreEqual(0, db.StaffProfiles.Count(s => s.HubId == fixture.Hub.Id));
            }
        }

        [TestMethod]
        public void ListKitchenStaff_ReturnsOwnStaffWithHubNameOrderedAndExcludesSoftDeleted()
        {
            using (var fixture = new Fixture())
            {
                string idB = fixture.Id("lb");
                string idA = fixture.Id("la");
                string idDeleted = fixture.Id("ld");
                fixture.Service.CreateKitchenStaff(idB, "Staff B", Password, fixture.Hub.Id);
                fixture.Service.CreateKitchenStaff(idA, "Staff A", Password, fixture.Hub.Id);
                fixture.Service.CreateKitchenStaff(idDeleted, "Staff Deleted", Password, fixture.Hub.Id);
                fixture.Db.Users.Single(u => u.Identifier == idDeleted).IsDeleted = true;
                fixture.Db.SaveChanges();

                IList<KitchenStaffInfo> list = fixture.Service.ListKitchenStaff();
                var mine = list.Where(s => s.HubId == fixture.Hub.Id).ToList();

                Assert.AreEqual(2, mine.Count);
                Assert.IsFalse(list.Any(s => s.Identifier == idDeleted));
                KitchenStaffInfo a = mine.Single(s => s.Identifier == idA);
                Assert.AreEqual("Staff A", a.FullName);
                Assert.AreEqual(fixture.Hub.Name, a.HubName);
                Assert.IsTrue(a.IsActive);
                Assert.IsTrue(a.UserId > 0);
                CollectionAssert.AreEqual(
                    list.Select(s => s.Identifier).OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList(),
                    list.Select(s => s.Identifier).ToList(),
                    StringComparer.OrdinalIgnoreCase);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly string _suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            private readonly List<string> _identifiers = new List<string>();

            public AppDbContext Db { get; private set; }
            public StaffAccountService Service { get; private set; }
            public Hub Hub { get; private set; }

            public Fixture()
            {
                Db = new AppDbContext();
                Hub = new Hub
                {
                    Name = "Staff test hub " + _suffix,
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
                string id = prefix + "-" + _suffix;
                Track(id);
                return id;
            }

            public void Track(string identifier)
            {
                _identifiers.Add(identifier.ToLowerInvariant());
            }

            public void AssertNoUser(string identifier)
            {
                using (var db = new AppDbContext())
                    Assert.AreEqual(0, db.Users.Count(u => u.Identifier == identifier));
            }

            public void AssertRejected(string rule, string identifier, string fullName = "Valid Name",
                string password = Password, int? hubId = null)
            {
                if (identifier != null && identifier.Trim().Length > 0) Track(identifier.Trim());
                var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                    Service.CreateKitchenStaff(identifier, fullName, password, hubId ?? Hub.Id));
                Assert.AreEqual(rule, ex.Rule);
                if (identifier != null)
                {
                    string trimmed = identifier.Trim();
                    using (var db = new AppDbContext())
                        Assert.AreEqual(0, db.Users.Count(u => u.Identifier == trimmed));
                }
                using (var db = new AppDbContext())
                    Assert.AreEqual(0, db.StaffProfiles.Count(s => s.HubId == Hub.Id));
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
