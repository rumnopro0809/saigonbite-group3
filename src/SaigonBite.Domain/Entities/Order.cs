using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SaigonBite.Domain.Entities
{
    /// <summary>
    /// Stores an order together with its frozen price breakdown and placement context snapshot (BR17).
    /// </summary>
    public class Order
    {
        public Order()
        {
            Lines = new List<OrderLine>();
            Payments = new List<PaymentRecord>();
            StatusHistory = new List<OrderStatusHistory>();
        }

        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string OrderCode { get; set; }

        public int CustomerId { get; set; }
        public int HubId { get; set; }
        public int? VoucherId { get; set; }
        public int? ShipperId { get; set; }

        public OrderStatus Status { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; }

        [MaxLength(64)]
        public string RequestKey { get; set; }

        [Required]
        [MaxLength(250)]
        public string DeliveryAddress { get; set; }

        public decimal DistanceKm { get; set; }

        public DateTimeOffset PlacedAt { get; set; }

        public decimal HubLoadSnapshot { get; set; }
        public bool HeavyRainSnapshot { get; set; }

        public decimal Subtotal { get; set; }
        public decimal QuietDiscount { get; set; }
        public decimal VoucherDiscount { get; set; }
        public decimal FoodTotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal RushFee { get; set; }
        public decimal RainFee { get; set; }
        public decimal RawTotal { get; set; }
        public decimal PayableTotal { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        [MaxLength(40)]
        public string PaymentStatus { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        public virtual User Customer { get; set; }
        public virtual Hub Hub { get; set; }
        public virtual Voucher Voucher { get; set; }
        public virtual User Shipper { get; set; }

        public virtual ICollection<OrderLine> Lines { get; set; }
        public virtual ICollection<PaymentRecord> Payments { get; set; }
        public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; }
    }
}
