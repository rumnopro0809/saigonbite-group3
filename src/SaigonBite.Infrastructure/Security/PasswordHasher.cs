using System;
using System.Security.Cryptography;
using System.Text;

namespace SaigonBite.Infrastructure.Security
{
    /// <summary>
    /// Simple deterministic SHA-256 password helper for local demo accounts.
    /// </summary>
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be empty.", "password");

            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("SaigonBite:" + password));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }

        public static bool Verify(string password, string storedHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
                return false;

            return string.Equals(Hash(password), storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
