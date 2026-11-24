using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaigonBite.Domain.Entities
{
    public class HubBrand
    {
        [Key]
        [Column(Order = 0)]
        public int HubId { get; set; }

        [Key]
        [Column(Order = 1)]
        public int BrandId { get; set; }

        public virtual Hub Hub { get; set; }
        public virtual Brand Brand { get; set; }
    }
}
