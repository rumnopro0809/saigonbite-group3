using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class PaymentRecord
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public PaymentMethod Method { get; set; }

        [Required]
        [MaxLength(40)]
        public string Operation { get; set; }

        public decimal Amount { get; set; }

        [Required]
        [MaxLength(40)]
        public string Result { get; set; }

        [MaxLength(64)]
        public string CorrelationKey { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual Order Order { get; set; }
    }
}
