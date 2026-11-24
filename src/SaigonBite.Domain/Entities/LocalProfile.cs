using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonBite.Domain.Entities
{
    public class LocalProfile
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(30)]
        public string Phone { get; set; }

        public virtual User User { get; set; }
    }
}
