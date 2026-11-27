using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using SaigonBite.Application.Reports;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Repositories
{
    // EF6 read repository for R1 Revenue Report (FR18).
    public sealed class EfRevenueReportRepository : IRevenueReportRepository
    {
        private readonly SaigonBiteDbContext _db;

        public EfRevenueReportRepository(SaigonBiteDbContext db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
        }

        public IList<Order> GetOrdersWithLinesAndHistory()
        {
            return _db.Orders
                .Include(o => o.Lines)
                .Include(o => o.StatusHistory)
                .ToList();
        }
    }
}
