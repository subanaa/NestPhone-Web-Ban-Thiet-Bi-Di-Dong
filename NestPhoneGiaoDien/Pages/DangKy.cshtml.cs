using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace NestPhoneGiaoDien.Pages
{
    public class DangKyModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DangKyModel> _logger;

        [BindProperty]
        public InputModel RegisterModel { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public DangKyModel(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DangKyModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Dữ liệu nhập không hợp lệ. Vui lòng kiểm tra lại.";
                return Page();
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                var apiUrl = _configuration.GetValue<string>("ApiSettings:KhachHangApiUrl")
                             ?? "http://localhost:5050/api/KhachHang";

                // Dữ liệu gửi về API
                var khachHangRequest = new
                {
                    soDienThoai = RegisterModel.Username,
                    matKhau = RegisterModel.Password,
                    gmail = RegisterModel.Email,      // Use 'gmail' to match API
                    hoVaTen = RegisterModel.FullName  // Use 'hoVaTen' to match API
                };

                var json = JsonSerializer.Serialize(khachHangRequest);
                _logger.LogInformation("API request payload: {Payload}", json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Registration successful for soDienThoai: {SoDienThoai}", RegisterModel.Username);
                    return RedirectToPage("/DangNhap");
                }

                var errorResponse = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("API error response: {Response}, Status: {Status}", errorResponse, response.StatusCode);
                ErrorMessage = TryParseErrorMessage(errorResponse, response.StatusCode);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for soDienThoai: {SoDienThoai}", RegisterModel.Username);
                ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại.";
                return Page();
            }
        }

        private string TryParseErrorMessage(string errorResponse, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                if (string.IsNullOrEmpty(errorResponse))
                {
                    return $"Đăng ký thất bại (Mã lỗi: {(int)statusCode}). Vui lòng thử lại.";
                }

                using var jsonDoc = JsonDocument.Parse(errorResponse);
                var root = jsonDoc.RootElement;

                // Check for 'message' property
                if (root.TryGetProperty("message", out var messageElement))
                {
                    return messageElement.GetString() ?? $"Đăng ký thất bại (Mã lỗi: {(int)statusCode}).";
                }

                // Check for 'errors' object (e.g., validation errors)
                if (root.TryGetProperty("errors", out var errorsElement))
                {
                    var errors = new List<string>();
                    if (errorsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var error in errorsElement.EnumerateArray())
                        {
                            errors.Add(error.GetString() ?? "Lỗi không xác định");
                        }
                    }
                    else if (errorsElement.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in errorsElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var error in prop.Value.EnumerateArray())
                                {
                                    errors.Add(error.GetString() ?? "Lỗi không xác định");
                                }
                            }
                        }
                    }
                    return errors.Any() ? string.Join("; ", errors) : $"Đăng ký thất bại (Mã lỗi: {(int)statusCode}).";
                }

                // Fallback to raw response
                return $"Lỗi từ máy chủ: {errorResponse} (Mã lỗi: {(int)statusCode})";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse error response: {Response}", errorResponse);
                return $"Lỗi từ máy chủ: {errorResponse} (Mã lỗi: {(int)statusCode})";
            }
        }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
            [StringLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
            [RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10 chữ số")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [DataType(DataType.Password)]
            [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
            public string Password { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}