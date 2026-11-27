using System.Collections.Generic;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Auth
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string storedHash);
    }

    public interface IUserRepository
    {
        IList<User> GetAll(bool includeDeleted);
        User GetById(int id);
        User GetByIdentifier(string identifier);
        User GetByLocalPhone(string phone);
        User GetByTouristPassport(string passportNumber);
        User GetByTouristEmail(string email);
        User Add(User user);
        void Update(User user);
        void SoftDelete(int id);
    }
}
