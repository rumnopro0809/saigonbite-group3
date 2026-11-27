using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using SaigonBite.Application.Hubs;
using SaigonBite.Domain;
using SaigonBite.Domain.Entities;
using SaigonBite.Infrastructure.Data;

namespace SaigonBite.Infrastructure.Services
{
    /// <summary>Nghiệp vụ quản lý bếp: tạo, xem, sửa, ngừng nhận đơn và xóa mềm (FR14.1 đến FR14.4).</summary>
    public sealed class HubService
    {
        private const string StaleMessage = "Bếp này vừa được người khác sửa. Hãy tải lại trang rồi thử lại.";
        private readonly SaigonBiteDbContext _db;
        private readonly HubLoadService _loadService;

        public HubService(SaigonBiteDbContext db)
        {
            _db = db;
            _loadService = new HubLoadService(db);
        }

        /// <summary>Danh sách bếp chưa bị xóa, kèm tải hiện tại (FR14.2).</summary>
        public IList<HubDto> List()
        {
            var counts = _loadService.CountActiveOrders();
            var hubs = _db.Hubs.Include(h => h.HubBrands).Where(h => !h.IsDeleted).OrderBy(h => h.Id).ToList();
            var brandNames = LoadBrandNames();
            return hubs.Select(h => ToDto(h, counts.ContainsKey(h.Id) ? counts[h.Id] : 0, brandNames)).ToList();
        }

        /// <summary>Chi tiết một bếp.</summary>
        public ServiceResult<HubDto> Get(int id)
        {
            var hub = FindHub(id);
            if (hub == null) return ServiceResult<HubDto>.NotFound("Không tìm thấy bếp.");
            return ServiceResult<HubDto>.Ok(ToDto(hub, _loadService.CountActiveOrders(id), LoadBrandNames()));
        }

        /// <summary>Các brand có thể chọn cho bếp (chưa bị xóa).</summary>
        public IList<BrandOptionDto> GetBrandOptions()
        {
            return _db.Brands.Where(b => !b.IsDeleted).OrderBy(b => b.Name)
                .Select(b => new BrandOptionDto { Id = b.Id, Name = b.Name }).ToList();
        }

        /// <summary>Tạo bếp mới cùng các brand. Hub và HubBrand được lưu trong một lần SaveChanges nên là một transaction (FR14.1).</summary>
        public ServiceResult<HubDto> Create(HubInput input)
        {
            var errors = HubValidator.Validate(input, ValidBrandIds());
            if (errors.Count > 0) return ServiceResult<HubDto>.Invalid(errors);

            var hub = new Hub
            {
                Name = input.Name.Trim(),
                Address = input.Location.Trim(),
                District = input.District.Trim(),
                MaxCapacity = input.Capacity,
                IsActive = true
            };
            foreach (int brandId in input.BrandIds)
                hub.HubBrands.Add(new HubBrand { BrandId = brandId });

            _db.Hubs.Add(hub);
            _db.SaveChanges();
            return ServiceResult<HubDto>.Created(ToDto(hub, 0, LoadBrandNames()));
        }

        /// <summary>Sửa tên, vị trí, công suất và brand của bếp. Có kiểm tra rowversion (FR14.3).</summary>
        public ServiceResult<HubDto> Update(int id, HubInput input)
        {
            var hub = FindHub(id);
            if (hub == null) return ServiceResult<HubDto>.NotFound("Không tìm thấy bếp.");

            var errors = HubValidator.Validate(input, ValidBrandIds());
            byte[] rowVersion = ParseRowVersion(input == null ? null : input.RowVersion, errors);
            if (errors.Count > 0) return ServiceResult<HubDto>.Invalid(errors);

            _db.Entry(hub).Property(x => x.RowVersion).OriginalValue = rowVersion;
            hub.Name = input.Name.Trim();
            hub.Address = input.Location.Trim();
            hub.District = input.District.Trim();
            // Giảm công suất có thể làm tải vượt 100%. Vẫn cho phép, và không sửa đơn cũ.
            hub.MaxCapacity = input.Capacity;
            SyncBrands(hub, input.BrandIds);

            return Save(hub);
        }

        /// <summary>Bật hoặc ngừng nhận đơn mới. Đơn đang chạy vẫn được xử lý tiếp (FR14.4).</summary>
        public ServiceResult<HubDto> SetActive(int id, bool isActive, string rowVersionText)
        {
            var hub = FindHub(id);
            if (hub == null) return ServiceResult<HubDto>.NotFound("Không tìm thấy bếp.");

            var errors = new List<ValidationError>();
            byte[] rowVersion = ParseRowVersion(rowVersionText, errors);
            if (errors.Count > 0) return ServiceResult<HubDto>.Invalid(errors);

            _db.Entry(hub).Property(x => x.RowVersion).OriginalValue = rowVersion;
            hub.IsActive = isActive;
            return Save(hub);
        }

        /// <summary>Xóa mềm bếp: ẩn khỏi danh sách nhưng giữ dữ liệu cho lịch sử đơn (FR14.4).</summary>
        public ServiceResult<HubDto> Delete(int id, string rowVersionText)
        {
            var hub = FindHub(id);
            if (hub == null) return ServiceResult<HubDto>.NotFound("Không tìm thấy bếp.");

            var errors = new List<ValidationError>();
            byte[] rowVersion = ParseRowVersion(rowVersionText, errors);
            if (errors.Count > 0) return ServiceResult<HubDto>.Invalid(errors);

            _db.Entry(hub).Property(x => x.RowVersion).OriginalValue = rowVersion;
            hub.IsDeleted = true;
            hub.IsActive = false;
            return Save(hub);
        }

        // ---- Hàm nội bộ ----

        private Hub FindHub(int id)
        {
            return _db.Hubs.Include(h => h.HubBrands).FirstOrDefault(h => h.Id == id && !h.IsDeleted);
        }

        private ICollection<int> ValidBrandIds()
        {
            return _db.Brands.Where(b => !b.IsDeleted).Select(b => b.Id).ToList();
        }

        private IDictionary<int, string> LoadBrandNames()
        {
            return _db.Brands.ToDictionary(b => b.Id, b => b.Name);
        }

        /// <summary>Lưu thay đổi. Nếu rowversion không khớp thì trả về Conflict để người dùng tải lại.</summary>
        private ServiceResult<HubDto> Save(Hub hub)
        {
            try
            {
                _db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                return ServiceResult<HubDto>.Conflict(StaleMessage);
            }
            return ServiceResult<HubDto>.Ok(ToDto(hub, _loadService.CountActiveOrders(hub.Id), LoadBrandNames()));
        }

        /// <summary>Đưa danh sách brand của bếp về đúng danh sách mới: xóa brand bị bỏ, thêm brand mới.</summary>
        private void SyncBrands(Hub hub, IList<int> newBrandIds)
        {
            var wanted = newBrandIds ?? new List<int>();
            foreach (var old in hub.HubBrands.Where(b => !wanted.Contains(b.BrandId)).ToList())
            {
                hub.HubBrands.Remove(old);
                _db.HubBrands.Remove(old);
            }
            foreach (int brandId in wanted)
            {
                if (hub.HubBrands.All(b => b.BrandId != brandId))
                    hub.HubBrands.Add(new HubBrand { HubId = hub.Id, BrandId = brandId });
            }
        }

        /// <summary>Đổi chuỗi base64 thành mảng byte. Nếu thiếu hoặc sai định dạng thì ghi lỗi.</summary>
        private static byte[] ParseRowVersion(string text, IList<ValidationError> errors)
        {
            if (string.IsNullOrEmpty(text))
            {
                errors.Add(new ValidationError("RowVersion", "Thiếu phiên bản dữ liệu (rowVersion). Hãy tải lại trang."));
                return null;
            }
            try
            {
                return Convert.FromBase64String(text);
            }
            catch (FormatException)
            {
                errors.Add(new ValidationError("RowVersion", "Phiên bản dữ liệu (rowVersion) không đúng định dạng."));
                return null;
            }
        }

        private static HubDto ToDto(Hub hub, int activeOrders, IDictionary<int, string> brandNames)
        {
            decimal load = HubLoad.Calculate(activeOrders, hub.MaxCapacity);
            var brandIds = hub.HubBrands.Select(b => b.BrandId).OrderBy(x => x).ToList();
            return new HubDto
            {
                Id = hub.Id,
                Name = hub.Name,
                Location = hub.Address,
                District = hub.District,
                Capacity = hub.MaxCapacity,
                IsActive = hub.IsActive,
                BrandIds = brandIds,
                BrandNames = brandIds.Select(i => brandNames.ContainsKey(i) ? brandNames[i] : "?").ToList(),
                ActiveOrders = activeOrders,
                Load = load,
                LoadPercent = (int)Math.Round(load * 100m, 0, MidpointRounding.AwayFromZero),
                LoadLevel = HubLoad.Classify(load),
                RowVersion = hub.RowVersion == null ? null : Convert.ToBase64String(hub.RowVersion)
            };
        }
    }
}
