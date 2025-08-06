namespace MobileStore.Web.Models
{
    public class KhoModel
    {
        public class Kho
        {
            public string MaKho { get; set; }
            public string DiaChi { get; set; }
            public string SDT { get; set; }
            public string MaPhieu { get; set; }
            public DateTime? NgayNhap { get; set; }
            public DateTime? NgayXuat { get; set; }
            public decimal GiaNhap { get; set; }
            public int SoLuongNhap { get; set; }
            public int SoLuongXuat { get; set; }
        }
    }
}