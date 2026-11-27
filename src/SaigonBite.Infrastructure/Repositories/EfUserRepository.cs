using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaigonBite.Application.Auth;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;
using SaigonBite.Infrastructure.Security;

namespace SaigonBite.Infrastructure.Repositories
{
    public sealed class Sha256PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return PasswordHasher.Hash(password);
        }

        public bool Verify(string password, string storedHash)
        {
            return PasswordHasher.Verify(password, storedHash);
        }
    }

    // EF6 repository for single-schema Users + LocalProfile/TouristProfile (FR12, BR18).
    public sealed class EfUserRepository : IUserRepository
    {
        private readonly SaigonBiteDbContext _db;

        public EfUserRepository(SaigonBiteDbContext db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
        }

        public IList<User> GetAll(bool includeDeleted)
        {
            IQueryable<User> query = _db.Users
                .Include(u => u.LocalProfile)
                .Include(u => u.TouristProfile)
                .Include(u => u.StaffProfile);

            if (!includeDeleted)
            {
                query = query.Where(u => !u.IsDeleted);
            }

            return query
                .OrderBy(u => u.IsDeleted)
                .ThenBy(u => u.Role)
                .ThenBy(u => u.FullName)
                .ToList();
        }

        public User GetById(int id)
        {
            return _db.Users
                .Include(u => u.LocalProfile)
                .Include(u => u.TouristProfile)
                .Include(u => u.StaffProfile)
                .FirstOrDefault(u => u.Id == id);
        }

        public User GetByIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            string key = identifier.Trim();
            return _db.Users
                .Include(u => u.LocalProfile)
                .Include(u => u.TouristProfile)
                .FirstOrDefault(u => u.Identifier == key);
        }

        public User GetByLocalPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            string key = phone.Trim();
            return _db.Users
                .Include(u => u.LocalProfile)
                .FirstOrDefault(u => u.LocalProfile != null && u.LocalProfile.Phone == key);
        }

        public User GetByTouristPassport(string passportNumber)
        {
            if (string.IsNullOrWhiteSpace(passportNumber)) return null;
            string key = passportNumber.Trim().ToUpperInvariant();
            return _db.Users
                .Include(u => u.TouristProfile)
                .FirstOrDefault(u => u.TouristProfile != null && u.TouristProfile.PassportNumber == key);
        }

        public User GetByTouristEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;
            string key = email.Trim().ToLowerInvariant();
            return _db.Users
                .Include(u => u.TouristProfile)
                .FirstOrDefault(u => u.TouristProfile != null && u.TouristProfile.Email == key);
        }

        public User Add(User user)
        {
            if (user == null) throw new ArgumentNullException("user");
            _db.Users.Add(user);
            _db.SaveChanges();
            return GetById(user.Id);
        }

        public void Update(User user)
        {
            if (user == null) throw new ArgumentNullException("user");
            User tracked = _db.Users.FirstOrDefault(u => u.Id == user.Id);
            if (tracked == null) throw new InvalidOperationException("User not found.");

            tracked.FullName = user.FullName;
            tracked.IsActive = user.IsActive;
            _db.SaveChanges();
        }

        public void SoftDelete(int id)
        {
            User tracked = _db.Users.FirstOrDefault(u => u.Id == id);
            if (tracked == null) return;

            tracked.IsActive = false;
            tracked.IsDeleted = true;
            _db.SaveChanges();
        }
    }
}
