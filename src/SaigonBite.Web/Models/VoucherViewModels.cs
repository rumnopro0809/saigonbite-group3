using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Web.Models
{
    public sealed class VoucherIndexViewModel
    {
        public VoucherIndexViewModel()
        {
            Vouchers = new List<Voucher>();
        }

        public string Lang { get; set; }
        public bool IncludeDeleted { get; set; }
        public IList<Voucher> Vouchers { get; set; }
    }

    public sealed class VoucherEditViewModel
    {
        public VoucherEditViewModel()
        {
            Type = VoucherType.Percent;
            IsActive = true;
            ValidFromDate = DateTime.Today;
            ValidToDate = DateTime.Today.AddMonths(1);
        }

        public int Id { get; set; }
        public string Lang { get; set; }

        [Required(ErrorMessage = "Voucher code is required.")]
        [MaxLength(40)]
        public string Code { get; set; }

        public VoucherType Type { get; set; }

        [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "Value must be greater than 0.")]
        public decimal Value { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Cap cannot be negative.")]
        public decimal Cap { get; set; }

        [Range(typeof(decimal), "0", "999999999", ErrorMessage = "Minimum subtotal cannot be negative.")]
        public decimal MinimumSubtotal { get; set; }

        [DataType(DataType.Date)]
        public DateTime ValidFromDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime ValidToDate { get; set; }

        public bool IsActive { get; set; }
    }
}
