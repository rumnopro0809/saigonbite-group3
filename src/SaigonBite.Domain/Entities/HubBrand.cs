namespace SaigonBite.Domain
{
    /// <summary>Bảng nối: bếp nào phục vụ brand nào. Khóa chính là (HubId, BrandId).</summary>
    public class HubBrand
    {
        public int HubId { get; set; }
        public int BrandId { get; set; }
        public virtual Hub Hub { get; set; }
        public virtual Brand Brand { get; set; }
    }
}
