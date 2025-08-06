using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MobileStore.Web.Pages
{
    [BindProperties]
    public class IndexModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5050");
        }

        public List<SanPham> SanPhamNoiBat { get; set; } = new List<SanPham>();
        public List<KhuyenMai> KhuyenMais { get; set; } = new List<KhuyenMai>();

        public async Task OnGetAsync()
        {
            try
            {
                // Lấy danh sách sản phẩm từ API Ctsp
                var response = await _httpClient.GetAsync("api/Ctsp");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Ctsp JSON: {json}");
                    var products = JsonSerializer.Deserialize<List<Ctsp>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<Ctsp>();

                    // Lấy danh sách hình ảnh từ API HinhAnh
                    var imageResponse = await _httpClient.GetAsync("api/HinhAnh");
                    List<HinhAnh> images = new List<HinhAnh>();
                    if (imageResponse.IsSuccessStatusCode)
                    {
                        var imageJson = await imageResponse.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Image JSON: {imageJson}");
                        images = JsonSerializer.Deserialize<List<HinhAnh>>(imageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<HinhAnh>();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"HinhAnh API error: Status {imageResponse.StatusCode}, Reason: {imageResponse.ReasonPhrase}");
                    }

                    // Log danh sách MaChiTiet từ API Ctsp
                    var ctspMaChiTets = products.Select(p => p.MaChiTiet ?? p.MaSanPham).ToList();
                    System.Diagnostics.Debug.WriteLine($"Ctsp MaChiTiet: {string.Join(", ", ctspMaChiTets)}");

                    // Log danh sách MaChiTiet từ API HinhAnh
                    var hinhAnhMaChiTets = images.Select(i => i.MaChiTiet).ToList();
                    System.Diagnostics.Debug.WriteLine($"HinhAnh MaChiTiet: {string.Join(", ", hinhAnhMaChiTets)}");

                    foreach (var product in products.Take(10))
                    {
                        string imageUrl = "/default-image.jpg";
                        string maChiTiet = product.MaChiTiet ?? product.MaSanPham;

                        // Tìm hình ảnh tương ứng với MaChiTiet
                        var matchingImage = images.FirstOrDefault(img => img.MaChiTiet == maChiTiet && !string.IsNullOrEmpty(img.AnhDaiDien_Url) && IsValidImageUrl(img.AnhDaiDien_Url));
                        if (matchingImage != null)
                        {
                            imageUrl = matchingImage.AnhDaiDien_Url;
                            System.Diagnostics.Debug.WriteLine($"Found image for {maChiTiet}: {imageUrl}");
                        }
                        else
                        {
                            // Thử sử dụng AnhHienThi_Url nếu AnhDaiDien_Url không hợp lệ
                            matchingImage = images.FirstOrDefault(img => img.MaChiTiet == maChiTiet && !string.IsNullOrEmpty(img.AnhHienThi_Url) && IsValidImageUrl(img.AnhHienThi_Url));
                            if (matchingImage != null)
                            {
                                imageUrl = matchingImage.AnhHienThi_Url;
                                System.Diagnostics.Debug.WriteLine($"Found display image for {maChiTiet}: {imageUrl}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No valid image found for {maChiTiet}. Available MaChiTiet: {string.Join(", ", hinhAnhMaChiTets)}");
                            }
                        }

                        SanPhamNoiBat.Add(new SanPham
                        {
                            MaSanPham = product.MaSanPham,
                            MaChiTiet = maChiTiet,
                            TenSanPham = product.TenSanPham,
                            Gia = product.GiaBan,
                            HinhAnh = imageUrl
                        });
                    }

                    // Log danh sách sản phẩm nổi bật
                    System.Diagnostics.Debug.WriteLine($"SanPhamNoiBat: {string.Join(", ", SanPhamNoiBat.Select(sp => $"{sp.TenSanPham} ({sp.MaChiTiet}): {sp.HinhAnh}"))}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Ctsp API error: Status {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }

                // Lấy danh sách khuyến mãi từ API KhuyenMai
                var khuyenMaiResponse = await _httpClient.GetAsync("api/KhuyenMai");
                if (khuyenMaiResponse.IsSuccessStatusCode)
                {
                    var khuyenMaiJson = await khuyenMaiResponse.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"KhuyenMai JSON: {khuyenMaiJson}");
                    KhuyenMais = JsonSerializer.Deserialize<List<KhuyenMai>>(khuyenMaiJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<KhuyenMai>();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"KhuyenMai API error: Status {khuyenMaiResponse.StatusCode}, Reason: {khuyenMaiResponse.ReasonPhrase}");
                    // Fallback to hardcoded data if API fails
                    KhuyenMais = new List<KhuyenMai>
                    {
                        new KhuyenMai
                        {
                            MaKhuyenMai = "KM2025",
                            NgayBatDau = new DateTime(2025, 6, 28, 18, 1, 0),
                            NgayKetThuc = new DateTime(2025, 7, 5, 23, 59, 59),
                            NoiDung = "Giảm 10% cho iPhone từ 28/6 đến 5/7"
                        },
                        new KhuyenMai
                        {
                            MaKhuyenMai = "KMHOT24",
                            NgayBatDau = new DateTime(2025, 6, 28, 18, 1, 0),
                            NgayKetThuc = new DateTime(2025, 6, 30, 23, 59, 59),
                            NoiDung = "Tặng phiếu mua hàng 500K khi mua Samsung"
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching data: {ex.Message}, StackTrace: {ex.StackTrace}");
                SanPhamNoiBat = new List<SanPham>();
                KhuyenMais = new List<KhuyenMai>();
            }
        }

        private bool IsValidImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (!Regex.IsMatch(url, @"^https?:\/\/.+")) return false;
            return true;
        }

        public class SanPham
        {
            public string MaSanPham { get; set; } = string.Empty;
            public string MaChiTiet { get; set; } = string.Empty;
            public string TenSanPham { get; set; } = string.Empty;
            public decimal Gia { get; set; }
            public string HinhAnh { get; set; } = string.Empty;
        }

        public class KhuyenMai
        {
            public string MaKhuyenMai { get; set; } = string.Empty;
            public DateTime NgayBatDau { get; set; }
            public DateTime NgayKetThuc { get; set; }
            public string NoiDung { get; set; } = string.Empty;
        }

        private class Ctsp
        {
            public int SoLuongTon { get; set; }
            public string TrangThai { get; set; } = string.Empty;
            public decimal GiaBan { get; set; }
            public int SoLuongDaBan { get; set; }
            public string TenSanPham { get; set; } = string.Empty;
            public string MaMau { get; set; } = string.Empty;
            public string MaDungLuong { get; set; } = string.Empty;
            public string MaPhieu { get; set; } = string.Empty;
            public string MaSanPham { get; set; } = string.Empty;
            public string MaChiTiet { get; set; } = string.Empty;
        }

        private class HinhAnh
        {
            public string MaAnh { get; set; } = string.Empty;
            public string AnhDaiDien_Url { get; set; } = string.Empty;
            public string AnhHienThi_Url { get; set; } = string.Empty;
            public string MaChiTiet { get; set; } = string.Empty;
        }
    }
}