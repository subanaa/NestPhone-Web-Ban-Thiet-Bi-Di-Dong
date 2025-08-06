using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace MobileStore.Web.Pages.Auth
{
    public static class SessionKeys
    {
        public const string VaiTro = "VaiTro";
        public const string TenKhachHang = "TenKhachHang";
        public const string AnhDaiDien = "AnhDaiDien";
        public const string MaKhachHang = "MaKhachHang";
        public const string MaNhanVien = "MaNhanVien";
        public const string HoNhanVien = "HoNhanVien";
        public const string TenNhanVien = "TenNhanVien";
        public const string AvatarNV = "AvatarNV";
        public const string SoDienThoai = "SoDienThoai";
    }

    public class UserSession
    {
        public string VaiTro { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;

        public bool IsLoggedIn => !string.IsNullOrEmpty(VaiTro);
    }

    public abstract class BasePageModel : PageModel
    {
        protected UserSession GetUserSession()
        {
            var session = new UserSession
            {
                VaiTro = HttpContext.Session.GetString(SessionKeys.VaiTro) ?? string.Empty,
                SoDienThoai = HttpContext.Session.GetString(SessionKeys.SoDienThoai) ?? string.Empty,
                Avatar = HttpContext.Session.GetString(SessionKeys.AnhDaiDien) ?? HttpContext.Session.GetString(SessionKeys.AvatarNV) ?? string.Empty,
                UserId = HttpContext.Session.GetString(SessionKeys.MaKhachHang) ?? HttpContext.Session.GetString(SessionKeys.MaNhanVien) ?? string.Empty,
                DisplayName = HttpContext.Session.GetString(SessionKeys.TenKhachHang) ??
                              $"{HttpContext.Session.GetString(SessionKeys.HoNhanVien)} {HttpContext.Session.GetString(SessionKeys.TenNhanVien)}".Trim() ?? string.Empty
            };
            return session;
        }

        protected bool IsUserLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString(SessionKeys.VaiTro));
        }
    }

    public class DangNhapModel : BasePageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<DangNhapModel> _logger;

        [BindProperty]
        public DangNhapInputModel LoginModel { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public DangNhapModel(IHttpClientFactory clientFactory, ILogger<DangNhapModel> logger)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            // Xóa session và cookie khi truy cập trang đăng nhập
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.Session.CommitAsync();
            _logger.LogInformation("Session và cookie xác thực đã được xóa khi truy cập trang đăng nhập.");

            if (!string.IsNullOrEmpty(returnUrl))
            {
                HttpContext.Session.SetString("RedirectAfterLogin", returnUrl);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Xóa session và cookie trước khi xử lý đăng nhập
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.Session.CommitAsync();
            _logger.LogInformation("Session và cookie xác thực đã được xóa trước khi đăng nhập.");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Vui lòng nhập đầy đủ và đúng định dạng thông tin.";
                return Page();
            }

            var httpClient = _clientFactory.CreateClient("api");

            try
            {
                // Check customer credentials via API
                _logger.LogInformation("Sending HTTP request GET api/KhachHang/phone/{SoDienThoai}", LoginModel.Username);
                var khResponse = await httpClient.GetAsync($"api/KhachHang/phone/{LoginModel.Username}");
                if (khResponse.IsSuccessStatusCode)
                {
                    var khData = await khResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("KhachHang API response: {Data}", khData);
                    var khachHang = JsonSerializer.Deserialize<KhachHang>(khData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (khachHang != null && khachHang.MatKhau == LoginModel.Password)
                    {
                        // Lưu thông tin vào session
                        HttpContext.Session.SetString(SessionKeys.VaiTro, "KhachHang");
                        HttpContext.Session.SetString(SessionKeys.TenKhachHang, khachHang.HoVaTen ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.AnhDaiDien, khachHang.Avatar ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.MaKhachHang, khachHang.MaKhachHang ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.SoDienThoai, khachHang.SoDienThoai ?? string.Empty);

                        // Thiết lập xác thực
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, khachHang.HoVaTen ?? string.Empty),
                            new Claim(ClaimTypes.Role, "KhachHang"),
                            new Claim("MaKhachHang", khachHang.MaKhachHang ?? string.Empty),
                            new Claim("SoDienThoai", khachHang.SoDienThoai ?? string.Empty)
                        };
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        await HttpContext.Session.CommitAsync();
                        _logger.LogInformation("Customer logged in: SoDienThoai={SoDienThoai}, MaKhachHang={MaKhachHang}", LoginModel.Username, khachHang.MaKhachHang);

                        // Kiểm tra URL chuyển hướng trong session
                        var redirectUrl = HttpContext.Session.GetString("RedirectAfterLogin");
                        if (!string.IsNullOrEmpty(redirectUrl))
                        {
                            HttpContext.Session.Remove("RedirectAfterLogin");
                            return Redirect(redirectUrl);
                        }

                        return RedirectToPage("/Index");
                    }
                }
                else
                {
                    _logger.LogWarning("KhachHang API failed: StatusCode={StatusCode}, Reason={ReasonPhrase}", khResponse.StatusCode, khResponse.ReasonPhrase);
                }

                // Check employee credentials
                _logger.LogInformation("Sending HTTP request GET api/NhanVien/phone/{SoDienThoai}", LoginModel.Username);
                var nvResponse = await httpClient.GetAsync($"api/NhanVien/phone/{LoginModel.Username}");
                if (nvResponse.IsSuccessStatusCode)
                {
                    var nvData = await nvResponse.Content.ReadAsStringAsync();
                    _logger.LogInformation("NhanVien API response: {Data}", nvData);
                    var nhanVien = JsonSerializer.Deserialize<NhanVien>(nvData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (nhanVien != null && nhanVien.MatKhau == LoginModel.Password)
                    {
                        var vaiTro = nhanVien.VaiTro ?? string.Empty;
                        HttpContext.Session.SetString(SessionKeys.VaiTro, vaiTro);
                        HttpContext.Session.SetString(SessionKeys.HoNhanVien, nhanVien.Ho ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.TenNhanVien, nhanVien.Ten ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.AvatarNV, nhanVien.Avatar ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.MaNhanVien, nhanVien.MaNhanVien ?? string.Empty);
                        HttpContext.Session.SetString(SessionKeys.SoDienThoai, nhanVien.SoDienThoai ?? string.Empty);

                        // Thiết lập xác thực cho nhân viên
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name, $"{nhanVien.Ho} {nhanVien.Ten}".Trim()),
                            new Claim(ClaimTypes.Role, vaiTro),
                            new Claim("MaNhanVien", nhanVien.MaNhanVien ?? string.Empty),
                            new Claim("SoDienThoai", nhanVien.SoDienThoai ?? string.Empty)
                        };
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                        };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        await HttpContext.Session.CommitAsync();
                        _logger.LogInformation("Employee logged in: SoDienThoai={SoDienThoai}, MaNhanVien={MaNhanVien}", LoginModel.Username, nhanVien.MaNhanVien);

                        // Kiểm tra URL chuyển hướng trong session
                        var redirectUrl = HttpContext.Session.GetString("RedirectAfterLogin");
                        if (!string.IsNullOrEmpty(redirectUrl))
                        {
                            HttpContext.Session.Remove("RedirectAfterLogin");
                            return Redirect(redirectUrl);
                        }

                        // Chuyển hướng dựa trên vai trò
                        return vaiTro switch
                        {
                            "Quản lý nhân viên" => RedirectToPage("/QuanLyNhanVienRieng/NhanVienQuanTri"),
                            "Quản lý kho" => RedirectToPage("/Admin/QuanLyKhoRieng/NhanVienQL_Kho"),
                            "Quản lý sản phẩm" => RedirectToPage("/QuanLySanPhamRieng/SanPham"),
                            "Quản lý đơn hàng" => RedirectToPage("/QuanLyDonHangRieng/QuanLyDonHangRieng"),
                            "Quản lý admin" => RedirectToPage("/AdminTongRieng/AdminTongRieng"),
                            _ => RedirectToPage("/Index"),
                        };
                    }
                }
                else
                {
                    _logger.LogWarning("NhanVien API failed: StatusCode={StatusCode}, Reason={ReasonPhrase}", nvResponse.StatusCode, nvResponse.ReasonPhrase);
                }

                ErrorMessage = "Sai số điện thoại hoặc mật khẩu.";
                _logger.LogWarning("Login failed for SoDienThoai: {SoDienThoai}", LoginModel.Username);
                return Page();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi kết nối API khi đăng nhập với SoDienThoai: {SoDienThoai}", LoginModel.Username);
                ErrorMessage = "Lỗi kết nối đến hệ thống. Vui lòng thử lại sau.";
                return Page();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi xử lý dữ liệu JSON khi đăng nhập với SoDienThoai: {SoDienThoai}", LoginModel.Username);
                ErrorMessage = "Lỗi xử lý dữ liệu. Vui lòng thử lại sau.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi đăng nhập với SoDienThoai: {SoDienThoai}", LoginModel.Username);
                ErrorMessage = "Đã xảy ra lỗi. Vui lòng liên hệ quản trị viên.";
                return Page();
            }
        }

        public class DangNhapInputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
            [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10 chữ số")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        private class KhachHang
        {
            public string MaKhachHang { get; set; } = string.Empty;
            public string HoVaTen { get; set; } = string.Empty;
            public string SoDienThoai { get; set; } = string.Empty;
            public string MatKhau { get; set; } = string.Empty;
            public string? DiaChi { get; set; }
            public string? Gmail { get; set; }
            public string? Avatar { get; set; }
            public string? GioiTinh { get; set; }
            public DateTime? NgaySinh { get; set; }
            public List<object>? DonHangs { get; set; }
            public List<object>? DanhGias { get; set; }
        }

        private class NhanVien
        {
            public string MaNhanVien { get; set; } = string.Empty;
            public string Ho { get; set; } = string.Empty;
            public string Ten { get; set; } = string.Empty;
            public string SoDienThoai { get; set; } = string.Empty;
            public string MatKhau { get; set; } = string.Empty;
            public string? Avatar { get; set; }
            public string? VaiTro { get; set; }
        }
    }
}