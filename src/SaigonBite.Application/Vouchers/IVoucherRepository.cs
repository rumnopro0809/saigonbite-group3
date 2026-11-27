using System.Collections.Generic;
using SaigonBite.Domain.Entities;

namespace SaigonBite.Application.Vouchers
{
    // Persistence contract for HT02 Voucher CRUD (FR15, BR9).
    public interface IVoucherRepository
    {
        IList<Voucher> GetAll(bool includeDeleted);
        Voucher GetById(int id);
        Voucher GetByCode(string code);
        Voucher Add(Voucher voucher);
        void Update(Voucher voucher);
        void SoftDelete(int id);
    }
}
