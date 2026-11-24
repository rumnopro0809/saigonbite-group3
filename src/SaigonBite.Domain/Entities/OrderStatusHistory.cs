using System;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public int ActorId { get; set; }

        public OrderStatus? FromStatus { get; set; }
        public OrderStatus ToStatus { get; set; }

        [MaxLength(300)]
        public string Reason { get; set; }

        public bool CashCollected { get; set; }

        public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual Order Order { get; set; }
        public virtual User Actor { get; set; }
    }
}
