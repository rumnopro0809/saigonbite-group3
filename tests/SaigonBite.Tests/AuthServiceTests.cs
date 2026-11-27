using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Auth;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Repositories;

namespace SaigonBite.Tests
{
    // HT01 Registration (BR18), Login & Admin Authorization tests (TC12, TC13). Author: 524H0189.
    [TestClass]
    public class AuthServiceTests
    {
        private InMemoryUserRepository _repo;
        private AuthService _service;

        [TestInitialize]
        public void Setup()
        {
            _repo = new InMemoryUserRepository();
            _service = new AuthService(_repo, new Sha256PasswordHasher());
        }

        [TestMethod]
        public void TC12_RegisterLocal_ValidPhoneAndOtp_HashesPasswordAndCreatesLocalProfile()
        {
            var result = _service.RegisterLocal(new LocalRegisterInput
            {
                FullName = "Nguyen Van A",
                Phone = "0901234567",
                Password = "Password123",
                OtpCode = AuthService.SimulatedOtpCode
            });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(CustomerType.Local, result.Data.CustomerType);
            Assert.AreEqual("0901234567", result.Data.LocalProfile.Phone);
            Assert.AreNotEqual("Password123", result.Data.PasswordHash);

            var login = _service.Login("0901234567", "Password123");
            Assert.IsTrue(login.Succeeded);
        }

        [TestMethod]
        public void TC12_RegisterLocal_PhoneAndPasswordBoundaries_EnforcesBR18()
        {
            // Invalid prefix 02, 9 digits, 7-char password, wrong OTP.
            var bad = _service.RegisterLocal(new LocalRegisterInput
            {
                FullName = "Test",
                Phone = "0201234567",
                Password = "1234567",
                OtpCode = "000000"
            });

            Assert.IsFalse(bad.Succeeded);
            Assert.IsTrue(bad.FieldErrors.ContainsKey("Phone"));
            Assert.IsTrue(bad.FieldErrors.ContainsKey("Password"));
            Assert.IsTrue(bad.FieldErrors.ContainsKey("OtpCode"));

            // Valid 8-char password boundary + valid 03 prefix.
            var ok = _service.RegisterLocal(new LocalRegisterInput
            {
                FullName = "Test OK",
                Phone = "0387654321",
                Password = "12345678",
                OtpCode = AuthService.SimulatedOtpCode
            });
            Assert.IsTrue(ok.Succeeded);
        }

        [TestMethod]
        public void TC12_RegisterTourist_ValidPassportAndEmail_CreatesTouristProfileInSameSchema()
        {
            var result = _service.RegisterTourist(new TouristRegisterInput
            {
                FullName = "Emma Watson",
                PassportNumber = "ab123456",
                Nationality = "United Kingdom",
                Email = "Emma@Example.com",
                Password = "TravelPass1",
                EmailCode = AuthService.SimulatedEmailCode
            });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(CustomerType.Tourist, result.Data.CustomerType);
            Assert.AreEqual("AB123456", result.Data.TouristProfile.PassportNumber);
            Assert.AreEqual("emma@example.com", result.Data.Identifier);
        }

        [TestMethod]
        public void TC12_RegisterTourist_PassportLengthBoundaries_Rejects5And10_Accepts6And9()
        {
            var tooShort = _service.RegisterTourist(new TouristRegisterInput
            {
                FullName = "Short",
                PassportNumber = "A1234",
                Nationality = "US",
                Email = "short@test.com",
                Password = "Password1",
                EmailCode = AuthService.SimulatedEmailCode
            });
            Assert.IsFalse(tooShort.Succeeded);
            Assert.IsTrue(tooShort.FieldErrors.ContainsKey("PassportNumber"));

            var tooLong = _service.RegisterTourist(new TouristRegisterInput
            {
                FullName = "Long",
                PassportNumber = "A123456789",
                Nationality = "US",
                Email = "long@test.com",
                Password = "Password1",
                EmailCode = AuthService.SimulatedEmailCode
            });
            Assert.IsFalse(tooLong.Succeeded);
            Assert.IsTrue(tooLong.FieldErrors.ContainsKey("PassportNumber"));

            var len6 = _service.RegisterTourist(new TouristRegisterInput
            {
                FullName = "Len6",
                PassportNumber = "A12345",
                Nationality = "US",
                Email = "len6@test.com",
                Password = "Password1",
                EmailCode = AuthService.SimulatedEmailCode
            });
            Assert.IsTrue(len6.Succeeded);

            var len9 = _service.RegisterTourist(new TouristRegisterInput
            {
                FullName = "Len9",
                PassportNumber = "A12345678",
                Nationality = "US",
                Email = "len9@test.com",
                Password = "Password1",
                EmailCode = AuthService.SimulatedEmailCode
            });
            Assert.IsTrue(len9.Succeeded);
        }

        [TestMethod]
        public void TC12_RegisterDuplicatePhoneOrPassport_IsRejected()
        {
            _service.RegisterLocal(new LocalRegisterInput
            {
                FullName = "User 1",
                Phone = "0701112222",
                Password = "Password1",
                OtpCode = AuthService.SimulatedOtpCode
            });

            var dup = _service.RegisterLocal(new LocalRegisterInput
            {
                FullName = "User 2",
                Phone = "0701112222",
                Password = "Password2",
                OtpCode = AuthService.SimulatedOtpCode
            });
            Assert.IsFalse(dup.Succeeded);
            Assert.IsTrue(dup.FieldErrors.ContainsKey("Phone"));
        }

        [TestMethod]
        public void TC13_DeactivatedUser_CannotLogin_AndNonAdminCannotManageUsers()
        {
            var created = _service.RegisterLocal(new LocalRegisterInput
            {
                FullName = "Lock Test",
                Phone = "0809998888",
                Password = "Password1",
                OtpCode = AuthService.SimulatedOtpCode
            });

            var lockRes = _service.SetUserActive(ActorRole.Admin, created.Data.Id, false);
            Assert.IsTrue(lockRes.Succeeded);
            Assert.IsFalse(_service.Login("0809998888", "Password1").Succeeded);

            var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                _service.GetAdminUsers(ActorRole.Customer, false));
            Assert.AreEqual("FR13", ex.Rule);
        }

        private sealed class InMemoryUserRepository : IUserRepository
        {
            private readonly List<User> _users = new List<User>();
            private int _nextId = 1;

            public IList<User> GetAll(bool includeDeleted)
            {
                return _users.Where(u => includeDeleted || !u.IsDeleted).ToList();
            }

            public User GetById(int id)
            {
                return _users.FirstOrDefault(u => u.Id == id);
            }

            public User GetByIdentifier(string identifier)
            {
                return _users.FirstOrDefault(u => string.Equals(u.Identifier, identifier, StringComparison.OrdinalIgnoreCase));
            }

            public User GetByLocalPhone(string phone)
            {
                return _users.FirstOrDefault(u => u.LocalProfile != null && u.LocalProfile.Phone == phone);
            }

            public User GetByTouristPassport(string passportNumber)
            {
                return _users.FirstOrDefault(u => u.TouristProfile != null && string.Equals(u.TouristProfile.PassportNumber, passportNumber, StringComparison.OrdinalIgnoreCase));
            }

            public User GetByTouristEmail(string email)
            {
                return _users.FirstOrDefault(u => u.TouristProfile != null && string.Equals(u.TouristProfile.Email, email, StringComparison.OrdinalIgnoreCase));
            }

            public User Add(User user)
            {
                user.Id = _nextId++;
                _users.Add(user);
                return user;
            }

            public void Update(User user)
            {
            }

            public void SoftDelete(int id)
            {
                User u = GetById(id);
                if (u != null)
                {
                    u.IsActive = false;
                    u.IsDeleted = true;
                }
            }
        }
    }
}
