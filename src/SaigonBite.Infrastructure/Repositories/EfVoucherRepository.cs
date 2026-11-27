using System;
using System.Collections.Generic;
using System.Linq;
using SaigonBite.Application.Vouchers;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Repositories
{
    // EF6 implementation of IVoucherRepository on SaigonBiteDbContext (FR15, BR9).
    public sealed class EfVoucherRepository : IVoucherRepository
    {
        private readonly SaigonBiteDbContext _db;

        public EfVoucherRepository(SaigonBiteDbContext db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
        }

        public IList<Voucher> GetAll(bool includeDeleted)
        {
            IQueryable<Voucher> query = _db.Vouchers;
            if (!includeDeleted)
            {
                query = query.Where(v => !v.IsDeleted);
            }

            return query
                .OrderBy(v => v.IsDeleted)
                .ThenBy(v => v.Code)
                .ToList();
        }

        public Voucher GetById(int id)
        {
            return _db.Vouchers.FirstOrDefault(v => v.Id == id);
        }

        public Voucher GetByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            string normalized = code.Trim().ToUpperInvariant();
            return _db.Vouchers.FirstOrDefault(v => v.Code == normalized);
        }

        public Voucher Add(Voucher voucher)
        {
            if (voucher == null) throw new ArgumentNullException("voucher");
            _db.Vouchers.Add(voucher);
            _db.SaveChanges();
            return voucher;
        }

        public void Update(Voucher voucher)
        {
            if (voucher == null) throw new ArgumentNullException("voucher");
            Voucher tracked = _db.Vouchers.FirstOrDefault(v => v.Id == voucher.Id);
            if (tracked == null) throw new InvalidOperationException("Voucher not found.");

            tracked.Code = voucher.Code;
            tracked.Type = voucher.Type;
            tracked.Value = voucher.Value;
            tracked.Cap = voucher.Cap;
            tracked.MinimumSubtotal = voucher.MinimumSubtotal;
            tracked.ValidFrom = voucher.ValidFrom;
            tracked.ValidTo = voucher.ValidTo;
            tracked.IsActive = voucher.IsActive;
            _db.SaveChanges();
        }

        public void SoftDelete(int id)
        {
            Voucher tracked = _db.Vouchers.FirstOrDefault(v => v.Id == id);
            if (tracked == null) return;

            tracked.IsActive = false;
            tracked.IsDeleted = true;
            _db.SaveChanges();
        }
    }
}
