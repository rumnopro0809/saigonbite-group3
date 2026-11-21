namespace SaigonBite.Domain
{
    /// <summary>
    /// TẠM (UC02): bản rút gọn của Brand để UC03 chọn brand cho bếp.
    /// Khi UC02 (Thái) thêm Brand đầy đủ thì thay file này, giữ nguyên Id và Name.
    /// </summary>
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
    }
}
