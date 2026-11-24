using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class Brand
    {
        public Brand()
        {
            HubBrands = new List<HubBrand>();
            MenuItems = new List<MenuItem>();
        }

        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; }

        [Required]
        [MaxLength(80)]
        public string Category { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public virtual ICollection<HubBrand> HubBrands { get; set; }
        public virtual ICollection<MenuItem> MenuItems { get; set; }
    }
}
