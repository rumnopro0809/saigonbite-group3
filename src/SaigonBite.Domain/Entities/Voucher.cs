using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class Voucher
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(40)]
        public string Code { get; set; }

        public VoucherType Type { get; set; }

        public decimal Value { get; set; }

        public decimal Cap { get; set; }

        public decimal MinimumSubtotal { get; set; }

        public DateTimeOffset ValidFrom { get; set; }

        public DateTimeOffset ValidTo { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;
    }
}
