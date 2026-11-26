using System;
using System.Collections.Generic;
using SaigonBite.Application.Orders;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Web.Models
{
    public class MenuSelectionRowViewModel
    {
        public int MenuItemId { get; set; }
        public string ItemName { get; set; }
        public string BrandName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderViewModel
    {
        public CreateOrderViewModel()
        {
            AvailableHubs = new List<Hub>();
            AvailablePaymentMethods = new List<PaymentMethod>();
            Items = new List<MenuSelectionRowViewModel>();
        }

        public int HubId { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal DistanceKm { get; set; }
        public string VoucherCode { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string Notes { get; set; }
        public string RequestKey { get; set; }
        public decimal? ExpectedPayableTotal { get; set; }

        public string Lang { get; set; }
        public CustomerType CustomerType { get; set; }
        public List<Hub> AvailableHubs { get; set; }
        public List<PaymentMethod> AvailablePaymentMethods { get; set; }
        public List<MenuSelectionRowViewModel> Items { get; set; }
        public OrderQuoteResult Quote { get; set; }
        public string ErrorRule { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class EditOrderViewModel
    {
        public EditOrderViewModel()
        {
            Items = new List<MenuSelectionRowViewModel>();
        }

        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string HubName { get; set; }
        public string DeliveryAddress { get; set; }
        public decimal DistanceKm { get; set; }
        public DateTimeOffset OriginalPlacedAt { get; set; }
        public decimal OriginalHubLoad { get; set; }
        public bool OriginalHeavyRain { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public decimal OriginalPayableTotal { get; set; }
        public bool HasVoucher { get; set; }
        public string VoucherCode { get; set; }
        public bool KeepVoucher { get; set; }
        public string Notes { get; set; }
        public string RowVersionBase64 { get; set; }

        public string Lang { get; set; }
        public List<MenuSelectionRowViewModel> Items { get; set; }
        public EditOrderQuoteResult Quote { get; set; }
        public string ErrorRule { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; }
        public User CurrentUser { get; set; }
        public string Lang { get; set; }
    }

    public class OrderDetailViewModel
    {
        public Order Order { get; set; }
        public User CurrentUser { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCancel { get; set; }
        public string RowVersionBase64 { get; set; }
        public string Lang { get; set; }
        public string NoticeMessage { get; set; }
        public string ErrorMessage { get; set; }
    }
}
