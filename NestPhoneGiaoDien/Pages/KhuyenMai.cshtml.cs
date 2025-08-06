using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MobileStore.Web.Pages
{
    public class KhuyenMaiModel : PageModel
    {
        public List<KhuyenMai> KhuyenMais { get; set; } = new();

        public void OnGet()
        {
            // Dữ liệu giả
            KhuyenMais = new List<KhuyenMai>
            {
                new KhuyenMai
                {
                    MaKhuyenMai = "KM001",
                    NgayBatDau = DateTime.Now.AddDays(-5),
                    NgayKetThuc = DateTime.Now.AddDays(5),
                    NoiDung = "Giảm 10% cho tất cả các sản phẩm iPhone khi mua online."
                },
                new KhuyenMai
                {
                    MaKhuyenMai = "KM002",
                    NgayBatDau = DateTime.Now,
                    NgayKetThuc = DateTime.Now.AddDays(10),
                    NoiDung = "Tặng tai nghe Bluetooth trị giá 500.000đ cho đơn hàng trên 10 triệu."
                },
                new KhuyenMai
                {
                    MaKhuyenMai = "KM003",
                    NgayBatDau = DateTime.Now.AddDays(-2),
                    NgayKetThuc = DateTime.Now.AddDays(7),
                    NoiDung = "Miễn phí vận chuyển toàn quốc trong tuần lễ sinh nhật hệ thống."
                }
            };
        }
    }

    public class KhuyenMai
    {
        public string MaKhuyenMai { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string NoiDung { get; set; } = string.Empty;
    }
}
