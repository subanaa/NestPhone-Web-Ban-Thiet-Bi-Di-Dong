namespace MobileStore.Web.Models
{
    public class SanPham
    {
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string MoTa { get; set; } = string.Empty;
        public string HinhAnh { get; set; } = string.Empty; // Link ảnh
        public string TrangThai {  get; set; } = string.Empty;
        public int SoLuongTon { get; set; } 
        public decimal GiaBan {  get; set; }
        public decimal Gia { get; set; } // Giá tiền
        public int DanhGia { get; set; }
        public string ThuongHieu { get; set; } = string.Empty;
    }
}
