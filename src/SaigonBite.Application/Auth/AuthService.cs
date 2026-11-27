using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SaigonBite.Application.Menu;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Auth
{
    public sealed class LocalRegisterInput
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Password { get; set; }
        public string OtpCode { get; set; }
    }

    public sealed class TouristRegisterInput
    {
        public string FullName { get; set; }
        public string PassportNumber { get; set; }
        public string Nationality { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string EmailCode { get; set; }
    }

    // HT01 Accounts & Authentication — FR12, FR13, BR18, NFR04.
    // Author: Duong Pham Thai (524H0189).
    public sealed class AuthService
    {
        // Simulated verification codes for Tier 3 demo (BR18).
        public const string SimulatedOtpCode = "123456";
        public const string SimulatedEmailCode = "654321";

        private static readonly Regex VnPhoneRegex = new Regex(@"^(03|05|07|08|09)\d{8}$", RegexOptions.Compiled);
        private static readonly Regex PassportRegex = new Regex(@"^[A-Za-z0-9]{6,9}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;

        public AuthService(IUserRepository users, IPasswordHasher hasher)
        {
            if (users == null) throw new ArgumentNullException("users");
            if (hasher == null) throw new ArgumentNullException("hasher");
            _users = users;
            _hasher = hasher;
        }

        // BR18: Local Customer uses 10-digit VN mobile (03/05/07/08/09) + simulated OTP + hashed password >= 8 chars.
        public MenuResult<User> RegisterLocal(LocalRegisterInput input)
        {
            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input == null)
            {
                return MenuResult<User>.Fail("VALIDATION", string.Empty, "Registration data is required.");
            }

            string fullName = (input.FullName ?? string.Empty).Trim();
            string phone = (input.Phone ?? string.Empty).Trim();
            string password = input.Password ?? string.Empty;
            string otp = (input.OtpCode ?? string.Empty).Trim();

            if (fullName.Length == 0) errors["FullName"] = "Full name is required.";
            else if (fullName.Length > 120) errors["FullName"] = "Full name must be at most 120 characters.";

            if (!VnPhoneRegex.IsMatch(phone))
            {
                errors["Phone"] = "Vietnamese mobile number must be 10 digits starting with 03, 05, 07, 08, or 09 (BR18).";
            }
            else if (_users.GetByIdentifier(phone) != null || _users.GetByLocalPhone(phone) != null)
            {
                errors["Phone"] = "This mobile number is already registered (BR18).";
            }

            if (password.Length < 8)
            {
                errors["Password"] = "Password must be at least 8 characters (BR18).";
            }

            if (!string.Equals(otp, SimulatedOtpCode, StringComparison.Ordinal))
            {
                errors["OtpCode"] = "Invalid OTP code (demo code: 123456).";
            }

            if (errors.Count > 0)
            {
                return MenuResult<User>.Fail("VALIDATION", errors);
            }

            var user = new User
            {
                Identifier = phone,
                PasswordHash = _hasher.Hash(password),
                FullName = fullName,
                Role = ActorRole.Customer,
                CustomerType = CustomerType.Local,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                LocalProfile = new LocalProfile { Phone = phone }
            };

            return MenuResult<User>.Ok(_users.Add(user));
        }

        // BR18: Foreign Tourist uses 6-9 char alphanumeric passport, nationality, valid email + simulated code.
        public MenuResult<User> RegisterTourist(TouristRegisterInput input)
        {
            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input == null)
            {
                return MenuResult<User>.Fail("VALIDATION", string.Empty, "Registration data is required.");
            }

            string fullName = (input.FullName ?? string.Empty).Trim();
            string passport = (input.PassportNumber ?? string.Empty).Trim().ToUpperInvariant();
            string nationality = (input.Nationality ?? string.Empty).Trim();
            string email = (input.Email ?? string.Empty).Trim().ToLowerInvariant();
            string password = input.Password ?? string.Empty;
            string code = (input.EmailCode ?? string.Empty).Trim();

            if (fullName.Length == 0) errors["FullName"] = "Full name is required.";
            else if (fullName.Length > 120) errors["FullName"] = "Full name must be at most 120 characters.";

            if (!PassportRegex.IsMatch(passport))
            {
                errors["PassportNumber"] = "Passport number must be 6 to 9 letters or digits (BR18).";
            }
            else if (_users.GetByTouristPassport(passport) != null)
            {
                errors["PassportNumber"] = "This passport number is already registered (BR18).";
            }

            if (nationality.Length == 0) errors["Nationality"] = "Nationality is required (BR18).";
            else if (nationality.Length > 80) errors["Nationality"] = "Nationality must be at most 80 characters.";

            if (!EmailRegex.IsMatch(email) || email.Length > 120)
            {
                errors["Email"] = "A valid email address is required (BR18).";
            }
            else if (_users.GetByIdentifier(email) != null || _users.GetByTouristEmail(email) != null)
            {
                errors["Email"] = "This email address is already registered (BR18).";
            }

            if (password.Length < 8)
            {
                errors["Password"] = "Password must be at least 8 characters (BR18).";
            }

            if (!string.Equals(code, SimulatedEmailCode, StringComparison.Ordinal))
            {
                errors["EmailCode"] = "Invalid email verification code (demo code: 654321).";
            }

            if (errors.Count > 0)
            {
                return MenuResult<User>.Fail("VALIDATION", errors);
            }

            var user = new User
            {
                Identifier = email,
                PasswordHash = _hasher.Hash(password),
                FullName = fullName,
                Role = ActorRole.Customer,
                CustomerType = CustomerType.Tourist,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                TouristProfile = new TouristProfile
                {
                    PassportNumber = passport,
                    Nationality = nationality,
                    Email = email
                }
            };

            return MenuResult<User>.Ok(_users.Add(user));
        }

        // FR13 / NFR04: Authenticate active account against stored password hash.
        public MenuResult<User> Login(string identifier, string password)
        {
            string key = (identifier ?? string.Empty).Trim();
            if (key.Length == 0 || string.IsNullOrEmpty(password))
            {
                return MenuResult<User>.Fail("AUTH", string.Empty, "Identifier and password are required.");
            }

            User user = _users.GetByIdentifier(key)
                ?? _users.GetByLocalPhone(key)
                ?? _users.GetByTouristEmail(key.ToLowerInvariant());

            if (user == null || user.IsDeleted || !user.IsActive)
            {
                return MenuResult<User>.Fail("AUTH", string.Empty, "Invalid credentials or account is deactivated.");
            }

            if (!_hasher.Verify(password, user.PasswordHash))
            {
                return MenuResult<User>.Fail("AUTH", string.Empty, "Invalid credentials or account is deactivated.");
            }

            return MenuResult<User>.Ok(user);
        }

        // FR12 / FR13: Admin user listing & account deactivation (soft-delete only).
        public IList<User> GetAdminUsers(ActorRole actorRole, bool includeDeleted)
        {
            RequireAdmin(actorRole);
            return _users.GetAll(includeDeleted);
        }

        public MenuResult<bool> SetUserActive(ActorRole actorRole, int userId, bool isActive)
        {
            RequireAdmin(actorRole);
            User user = _users.GetById(userId);
            if (user == null || user.IsDeleted)
            {
                return MenuResult<bool>.Fail("NOT_FOUND", "Id", "User not found.");
            }

            user.IsActive = isActive;
            _users.Update(user);
            return MenuResult<bool>.Ok(true);
        }

        public MenuResult<bool> SoftDeleteUser(ActorRole actorRole, int userId)
        {
            RequireAdmin(actorRole);
            User user = _users.GetById(userId);
            if (user == null)
            {
                return MenuResult<bool>.Fail("NOT_FOUND", "Id", "User not found.");
            }

            _users.SoftDelete(userId);
            return MenuResult<bool>.Ok(true);
        }

        private static void RequireAdmin(ActorRole actorRole)
        {
            if (actorRole != ActorRole.Admin)
            {
                throw new BusinessRuleException("FR13", "Only System Admin can manage user accounts.");
            }
        }
    }
}
