using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class OrderLine
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int MenuItemId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPriceSnapshot { get; set; }

        [Required]
        [MaxLength(120)]
        public string ItemNameSnapshot { get; set; }

        [Required]
        [MaxLength(80)]
        public string CategorySnapshot { get; set; }

        public virtual Order Order { get; set; }
        public virtual MenuItem MenuItem { get; set; }
    }
}
