using System;
using System.Collections.Generic;
using SaigonBite.Application.Menu;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Vouchers
{
    // Form input model for Admin voucher creation and update (FR15, BR9).
    public sealed class VoucherFormInput
    {
        public string Code { get; set; }
        public VoucherType Type { get; set; }
        public decimal Value { get; set; }
        public decimal Cap { get; set; }
        public decimal MinimumSubtotal { get; set; }
        public DateTimeOffset ValidFrom { get; set; }
        public DateTimeOffset ValidTo { get; set; }
        public bool IsActive { get; set; }
    }

    // HT02 Manage Vouchers (CRC08) — FR15, FR13, BR9.
    // Author: Duong Pham Thai (524H0189).
    public sealed class VoucherService
    {
        private readonly IVoucherRepository _repository;

        public VoucherService(IVoucherRepository repository)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            _repository = repository;
        }

        private static void RequireAdmin(ActorRole role)
        {
            if (role != ActorRole.Admin)
            {
                throw new BusinessRuleException("FR13", "Only System Admin can manage vouchers.");
            }
        }

        public IList<Voucher> GetAdminVouchers(ActorRole role, bool includeDeleted)
        {
            RequireAdmin(role);
            return _repository.GetAll(includeDeleted);
        }

        public Voucher GetById(ActorRole role, int id)
        {
            RequireAdmin(role);
            return _repository.GetById(id);
        }

        // BR9: Look up an active, in-date voucher that meets minimum subtotal S.
        public Voucher FindEligibleVoucher(string code, decimal subtotal, DateTimeOffset atTime)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            Voucher v = _repository.GetByCode(code.Trim().ToUpperInvariant());
            if (v == null || v.IsDeleted || !v.IsActive) return null;
            if (atTime < v.ValidFrom || atTime > v.ValidTo) return null;
            if (subtotal < v.MinimumSubtotal) return null;
            return v;
        }

        public MenuResult<Voucher> Create(ActorRole role, VoucherFormInput input)
        {
            RequireAdmin(role);
            var errors = Validate(input, excludeId: null);
            if (errors.Count > 0)
            {
                return MenuResult<Voucher>.Fail("VALIDATION", errors);
            }

            var voucher = new Voucher
            {
                Code = input.Code.Trim().ToUpperInvariant(),
                Type = input.Type,
                Value = input.Value,
                Cap = input.Type == VoucherType.Fixed ? input.Value : input.Cap,
                MinimumSubtotal = input.MinimumSubtotal,
                ValidFrom = input.ValidFrom,
                ValidTo = input.ValidTo,
                IsActive = input.IsActive,
                IsDeleted = false
            };

            return MenuResult<Voucher>.Ok(_repository.Add(voucher));
        }

        public MenuResult<Voucher> Update(ActorRole role, int id, VoucherFormInput input)
        {
            RequireAdmin(role);
            Voucher existing = _repository.GetById(id);
            if (existing == null || existing.IsDeleted)
            {
                return MenuResult<Voucher>.Fail("NOT_FOUND", "Id", "Voucher not found or already soft-deleted.");
            }

            var errors = Validate(input, excludeId: id);
            if (errors.Count > 0)
            {
                return MenuResult<Voucher>.Fail("VALIDATION", errors);
            }

            existing.Code = input.Code.Trim().ToUpperInvariant();
            existing.Type = input.Type;
            existing.Value = input.Value;
            existing.Cap = input.Type == VoucherType.Fixed ? input.Value : input.Cap;
            existing.MinimumSubtotal = input.MinimumSubtotal;
            existing.ValidFrom = input.ValidFrom;
            existing.ValidTo = input.ValidTo;
            existing.IsActive = input.IsActive;

            _repository.Update(existing);
            return MenuResult<Voucher>.Ok(existing);
        }

        // FR15 / Section 2E: Soft-delete keeps VoucherId valid for past Orders.
        public MenuResult<bool> SoftDelete(ActorRole role, int id)
        {
            RequireAdmin(role);
            Voucher existing = _repository.GetById(id);
            if (existing == null)
            {
                return MenuResult<bool>.Fail("NOT_FOUND", "Id", "Voucher not found.");
            }

            _repository.SoftDelete(id);
            return MenuResult<bool>.Ok(true);
        }

        private IDictionary<string, string> Validate(VoucherFormInput input, int? excludeId)
        {
            var errors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (input == null)
            {
                errors[string.Empty] = "Voucher data is required.";
                return errors;
            }

            string code = (input.Code ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length == 0) errors["Code"] = "Voucher code is required.";
            else if (code.Length > 40) errors["Code"] = "Voucher code must be at most 40 characters.";
            else
            {
                Voucher dup = _repository.GetByCode(code);
                if (dup != null && (!excludeId.HasValue || dup.Id != excludeId.Value))
                {
                    errors["Code"] = "Voucher code already exists.";
                }
            }

            if (input.Type == VoucherType.Percent)
            {
                if (input.Value <= 0m || input.Value > 100m)
                {
                    errors["Value"] = "Percent voucher value must be greater than 0 and at most 100 (BR9).";
                }
                if (input.Cap < 0m)
                {
                    errors["Cap"] = "Maximum discount cap cannot be negative (BR9).";
                }
            }
            else if (input.Type == VoucherType.Fixed)
            {
                if (input.Value <= 0m)
                {
                    errors["Value"] = "Fixed voucher amount must be greater than 0 (BR9).";
                }
            }
            else
            {
                errors["Type"] = "Unsupported voucher type (BR9).";
            }

            if (input.MinimumSubtotal < 0m)
            {
                errors["MinimumSubtotal"] = "Minimum subtotal cannot be negative.";
            }

            if (input.ValidTo < input.ValidFrom)
            {
                errors["ValidTo"] = "End date must be on or after start date.";
            }

            return errors;
        }
    }
}
