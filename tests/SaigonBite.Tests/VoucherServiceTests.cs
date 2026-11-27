using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SaigonBite.Application.Vouchers;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Tests
{
    // HT02 Voucher CRUD and BR9 boundary tests (TC15, FR15, FR13). Author: 524H0189.
    [TestClass]
    public class VoucherServiceTests
    {
        private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);
        private InMemoryVoucherRepository _repo;
        private VoucherService _service;

        [TestInitialize]
        public void Setup()
        {
            _repo = new InMemoryVoucherRepository();
            _service = new VoucherService(_repo);
        }

        [TestMethod]
        public void TC15_CreatePercentVoucher_NormalizesCodeAndPersists()
        {
            var result = _service.Create(ActorRole.Admin, new VoucherFormInput
            {
                Code = "  saigon10 ",
                Type = VoucherType.Percent,
                Value = 10m,
                Cap = 30000m,
                MinimumSubtotal = 100000m,
                ValidFrom = new DateTimeOffset(2026, 11, 1, 0, 0, 0, VnOffset),
                ValidTo = new DateTimeOffset(2026, 12, 31, 23, 59, 59, VnOffset),
                IsActive = true
            });

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("SAIGON10", result.Data.Code);
            Assert.AreEqual(30000m, result.Data.Cap);
            Assert.IsFalse(result.Data.IsDeleted);
        }

        [TestMethod]
        public void TC15_CreateVoucher_PercentBoundaries_RejectsZeroAnd101_Accepts100()
        {
            DateTimeOffset from = new DateTimeOffset(2026, 11, 1, 0, 0, 0, VnOffset);
            DateTimeOffset to = new DateTimeOffset(2026, 12, 31, 23, 59, 59, VnOffset);

            var zero = _service.Create(ActorRole.Admin, new VoucherFormInput
            {
                Code = "ZERO",
                Type = VoucherType.Percent,
                Value = 0m,
                Cap = 10000m,
                MinimumSubtotal = 50000m,
                ValidFrom = from,
                ValidTo = to,
                IsActive = true
            });
            Assert.IsFalse(zero.Succeeded);
            Assert.IsTrue(zero.FieldErrors.ContainsKey("Value"));

            var over100 = _service.Create(ActorRole.Admin, new VoucherFormInput
            {
                Code = "OVER100",
                Type = VoucherType.Percent,
                Value = 101m,
                Cap = 10000m,
                MinimumSubtotal = 50000m,
                ValidFrom = from,
                ValidTo = to,
                IsActive = true
            });
            Assert.IsFalse(over100.Succeeded);
            Assert.IsTrue(over100.FieldErrors.ContainsKey("Value"));

            var exact100 = _service.Create(ActorRole.Admin, new VoucherFormInput
            {
                Code = "FREE100",
                Type = VoucherType.Percent,
                Value = 100m,
                Cap = 50000m,
                MinimumSubtotal = 50000m,
                ValidFrom = from,
                ValidTo = to,
                IsActive = true
            });
            Assert.IsTrue(exact100.Succeeded);
        }

        [TestMethod]
        public void TC15_CreateVoucher_DuplicateCodeOrInvalidDates_ReturnsErrors()
        {
            DateTimeOffset from = new DateTimeOffset(2026, 12, 10, 0, 0, 0, VnOffset);
            DateTimeOffset toBefore = new DateTimeOffset(2026, 12, 1, 0, 0, 0, VnOffset);

            var duplicate = _service.Create(ActorRole.Admin, new VoucherFormInput
            {
                Code = "saigon15",
                Type = VoucherType.Fixed,
                Value = 15000m,
                MinimumSubtotal = 50000m,
                ValidFrom = from,
                ValidTo = toBefore,
                IsActive = true
            });

            Assert.IsFalse(duplicate.Succeeded);
            Assert.IsTrue(duplicate.FieldErrors.ContainsKey("Code"));
            Assert.IsTrue(duplicate.FieldErrors.ContainsKey("ValidTo"));
        }

        [TestMethod]
        public void TC15_FindEligibleVoucher_ChecksMinSubtotalAndValidityWindow()
        {
            DateTimeOffset inRange = new DateTimeOffset(2026, 11, 27, 12, 0, 0, VnOffset);
            DateTimeOffset expired = new DateTimeOffset(2028, 1, 1, 0, 0, 0, VnOffset);

            // SAIGON15 has MinimumSubtotal = 100,000.
            Assert.IsNull(_service.FindEligibleVoucher("SAIGON15", 99999m, inRange));
            Assert.IsNotNull(_service.FindEligibleVoucher("SAIGON15", 100000m, inRange));
            Assert.IsNull(_service.FindEligibleVoucher("SAIGON15", 150000m, expired));
        }

        [TestMethod]
        public void TC15_UpdateFixedVoucher_SetsCapToValueAndUpdatesTerms()
        {
            var update = _service.Update(ActorRole.Admin, 1, new VoucherFormInput
            {
                Code = "SAIGON25K",
                Type = VoucherType.Fixed,
                Value = 25000m,
                Cap = 0m,
                MinimumSubtotal = 120000m,
                ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, VnOffset),
                ValidTo = new DateTimeOffset(2027, 12, 31, 23, 59, 59, VnOffset),
                IsActive = true
            });

            Assert.IsTrue(update.Succeeded);
            Assert.AreEqual("SAIGON25K", update.Data.Code);
            Assert.AreEqual(25000m, update.Data.Cap);
        }

        [TestMethod]
        public void TC15_SoftDeleteVoucher_DeactivatesWithoutRemovingRecord()
        {
            var delete = _service.SoftDelete(ActorRole.Admin, 1);
            Assert.IsTrue(delete.Succeeded);

            DateTimeOffset now = new DateTimeOffset(2026, 11, 27, 12, 0, 0, VnOffset);
            Assert.IsNull(_service.FindEligibleVoucher("SAIGON15", 200000m, now));

            Voucher retained = _service.GetById(ActorRole.Admin, 1);
            Assert.IsNotNull(retained);
            Assert.IsTrue(retained.IsDeleted);
            Assert.IsFalse(retained.IsActive);
        }

        [TestMethod]
        public void TC15_NonAdmin_CannotManageVouchers_ThrowsFR13()
        {
            var ex = Assert.ThrowsException<BusinessRuleException>(() =>
                _service.SoftDelete(ActorRole.Customer, 1));
            Assert.AreEqual("FR13", ex.Rule);
        }

        private sealed class InMemoryVoucherRepository : IVoucherRepository
        {
            private readonly List<Voucher> _vouchers = new List<Voucher>();
            private int _nextId = 2;

            public InMemoryVoucherRepository()
            {
                _vouchers.Add(new Voucher
                {
                    Id = 1,
                    Code = "SAIGON15",
                    Type = VoucherType.Percent,
                    Value = 15m,
                    Cap = 40000m,
                    MinimumSubtotal = 100000m,
                    ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, VnOffset),
                    ValidTo = new DateTimeOffset(2027, 12, 31, 23, 59, 59, VnOffset),
                    IsActive = true,
                    IsDeleted = false
                });
            }

            public IList<Voucher> GetAll(bool includeDeleted)
            {
                return _vouchers.Where(v => includeDeleted || !v.IsDeleted).ToList();
            }

            public Voucher GetById(int id)
            {
                return _vouchers.FirstOrDefault(v => v.Id == id);
            }

            public Voucher GetByCode(string code)
            {
                return _vouchers.FirstOrDefault(v => string.Equals(v.Code, code, StringComparison.OrdinalIgnoreCase));
            }

            public Voucher Add(Voucher voucher)
            {
                voucher.Id = _nextId++;
                _vouchers.Add(voucher);
                return voucher;
            }

            public void Update(Voucher voucher)
            {
            }

            public void SoftDelete(int id)
            {
                Voucher v = GetById(id);
                if (v != null)
                {
                    v.IsActive = false;
                    v.IsDeleted = true;
                }
            }
        }
    }
}
