namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO hiển thị Gói mua thêm (Addon Package) — gói token dự phòng người dùng có thể mua.
    /// </summary>
    public class AddonPackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int QuotaAmount { get; set; }
        public bool IsActive { get; set; }
    }
}
