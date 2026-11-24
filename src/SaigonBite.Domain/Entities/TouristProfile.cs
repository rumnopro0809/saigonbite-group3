using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonBite.Domain.Entities
{
    public class TouristProfile
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(40)]
        public string PassportNumber { get; set; }

        [Required]
        [MaxLength(80)]
        public string Nationality { get; set; }

        [Required]
        [MaxLength(120)]
        public string Email { get; set; }

        public virtual User User { get; set; }
    }
}
