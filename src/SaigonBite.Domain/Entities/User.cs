using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    /// <summary>
    /// Application account supporting Local customers, Tourists, Kitchen Staff, Shippers, and Admins.
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Identifier { get; set; }

        [Required]
        [MaxLength(128)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(120)]
        public string FullName { get; set; }

        public ActorRole Role { get; set; }

        public CustomerType? CustomerType { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual LocalProfile LocalProfile { get; set; }
        public virtual TouristProfile TouristProfile { get; set; }
        public virtual StaffProfile StaffProfile { get; set; }
    }
}
