using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class MenuItem
    {
        public int Id { get; set; }

        public int BrandId { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public decimal Price { get; set; }

        [MaxLength(500)]
        public string ImageUrl { get; set; }

        public bool IsAvailable { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public virtual Brand Brand { get; set; }
    }
}
