using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonBite.Domain.Entities
{
    public class StaffProfile
    {
        [Key]
        [ForeignKey("User")]
        public int UserId { get; set; }

        public int HubId { get; set; }

        public virtual User User { get; set; }
        public virtual Hub Hub { get; set; }
    }
}
