using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MobileStore.Web.Pages
{
    public class CartModel : PageModel
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CartModel> _logger;

        public CartModel(IHttpClientFactory httpClientFactory, ILogger<CartModel> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5050/api/");
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _logger = logger;
        }

        public List<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal TotalPrice => CartItems.Sum(item => item.Price * item.Quantity);

        private void SaveCartToCookie(List<CartItem> cartItems)
        {
            try
            {
                var options = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(7),
                    HttpOnly = false
                };
                string json = JsonSerializer.Serialize(cartItems);
                Response.Cookies.Append("CartItems", json, options);
                _logger.LogInformation("Lưu giỏ hàng vào cookie: {Json}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu giỏ hàng vào cookie");
            }
        }

        private List<CartItem> GetCartFromCookie()
        {
            try
            {
                var cookie = Request.Cookies["CartItems"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    var cartItems = JsonSerializer.Deserialize<List<CartItem>>(cookie) ?? new List<CartItem>();
                    _logger.LogInformation("Lấy giỏ hàng từ cookie: {Cookie}", cookie);
                    return cartItems;
                }
                return new List<CartItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy giỏ hàng từ cookie");
                return new List<CartItem>();
            }
        }

        public IActionResult OnGet()
        {
            try
            {
                _logger.LogDebug("Bắt đầu tải giỏ hàng");
                CartItems = GetObjectFromSession<List<CartItem>>("CartItems") ?? GetCartFromCookie();

                if (CartItems == null || !CartItems.Any())
                {
                    CartItems = new List<CartItem>
                    {
                        new CartItem { MaChiTiet = "CT001", Quantity = 1, ProductName = "Sản phẩm demo CT001", Price = 199000, ImageUrl = "/images/iphone_14.png" },
                        new CartItem { MaChiTiet = "CT002", Quantity = 2, ProductName = "Sản phẩm demo CT002", Price = 199000, ImageUrl = "/images/iphone_14.png" }
                    };
                    _logger.LogInformation("Sử dụng dữ liệu demo cho giỏ hàng: {CartItems}", JsonSerializer.Serialize(CartItems));
                }

                SetObjectToSession("CartItems", CartItems);
                SaveCartToCookie(CartItems);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi trong OnGet");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống khi tải giỏ hàng." });
            }
        }

        public IActionResult OnPostUpdateQuantityAsync(string maChiTiet, int quantity)
        {
            try
            {
                _logger.LogDebug("Cập nhật số lượng: MaChiTiet={MaChiTiet}, Quantity={Quantity}", maChiTiet, quantity);

                if (string.IsNullOrWhiteSpace(maChiTiet) || quantity < 1)
                {
                    _logger.LogError("Dữ liệu không hợp lệ: MaChiTiet={MaChiTiet}, Quantity={Quantity}", maChiTiet, quantity);
                    return new JsonResult(new { success = false, message = "Thông tin sản phẩm hoặc số lượng không hợp lệ." });
                }

                var cartItems = GetObjectFromSession<List<CartItem>>("CartItems") ?? new List<CartItem>();
                _logger.LogDebug("CartItems từ session: {CartItems}", JsonSerializer.Serialize(cartItems));

                var item = cartItems.FirstOrDefault(i => i.MaChiTiet == maChiTiet);
                if (item != null)
                {
                    item.Quantity = quantity;
                    SetObjectToSession("CartItems", cartItems);
                    SaveCartToCookie(cartItems);
                    _logger.LogInformation("Đã cập nhật số lượng và lưu giỏ hàng: {CartItems}", JsonSerializer.Serialize(cartItems));
                    return new JsonResult(new { success = true, message = "Cập nhật số lượng thành công!" });
                }

                _logger.LogError("Không tìm thấy sản phẩm với MaChiTiet={MaChiTiet}", maChiTiet);
                return new JsonResult(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật số lượng");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        public IActionResult OnPostRemoveFromCartAsync(string maChiTiet)
        {
            try
            {
                _logger.LogDebug("Xóa sản phẩm khỏi giỏ hàng: MaChiTiet={MaChiTiet}", maChiTiet);
                var cartItems = GetObjectFromSession<List<CartItem>>("CartItems") ?? new List<CartItem>();
                _logger.LogDebug("CartItems từ session: {CartItems}", JsonSerializer.Serialize(cartItems));

                var item = cartItems.FirstOrDefault(i => i.MaChiTiet == maChiTiet);
                if (item != null)
                {
                    cartItems.Remove(item);
                    SetObjectToSession("CartItems", cartItems);
                    SaveCartToCookie(cartItems);
                    _logger.LogInformation("Đã xóa sản phẩm và lưu giỏ hàng: {CartItems}", JsonSerializer.Serialize(cartItems));
                    return new JsonResult(new { success = true, message = "Xóa sản phẩm thành công!" });
                }

                _logger.LogError("Sản phẩm không tồn tại trong giỏ hàng: MaChiTiet={MaChiTiet}", maChiTiet);
                return new JsonResult(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xóa sản phẩm");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        public IActionResult OnPostCreateOrderAsync(string phuongThucThanhToan)
        {
            try
            {
                _logger.LogDebug("Bắt đầu tạo đơn hàng");
                var maKhachHang = HttpContext.Session.GetString("MaKhachHang");
                _logger.LogDebug("MaKhachHang: {MaKhachHang}", maKhachHang ?? "null");

                var cartItems = GetObjectFromSession<List<CartItem>>("CartItems") ?? new List<CartItem>();
                _logger.LogDebug("CartItems từ session: {CartItems}", JsonSerializer.Serialize(cartItems));

                if (!cartItems.Any())
                {
                    _logger.LogError("Giỏ hàng rỗng");
                    return new JsonResult(new { success = false, message = "Giỏ hàng rỗng. Vui lòng thêm sản phẩm trước khi tạo đơn hàng." });
                }

                if (string.IsNullOrWhiteSpace(maKhachHang))
                {
                    _logger.LogError("Không tìm thấy mã khách hàng trong session");
                    return new JsonResult(new { success = false, message = "Vui lòng đăng nhập để tạo đơn hàng." });
                }

                // Xóa giỏ hàng sau khi tạo đơn hàng thành công
                cartItems.Clear();
                SetObjectToSession("CartItems", cartItems);
                SaveCartToCookie(cartItems);
                _logger.LogInformation("Tạo đơn hàng thành công");
                return new JsonResult(new { success = true, message = "Tạo đơn hàng thành công!", redirect = "/DonHang/ThanhToan" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo đơn hàng");
                return new JsonResult(new { success = false, message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau." });
            }
        }

        private bool IsValidImageUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("http://") || url.StartsWith("https://");
        }

        private void SetObjectToSession<T>(string key, T value)
        {
            try
            {
                HttpContext.Session.SetString(key, JsonSerializer.Serialize(value));
                _logger.LogInformation("Lưu vào session: Key={Key}, Value={Value}", key, JsonSerializer.Serialize(value));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu session: Key={Key}", key);
            }
        }

        private T GetObjectFromSession<T>(string key)
        {
            try
            {
                var value = HttpContext.Session.GetString(key);
                if (value == null)
                {
                    _logger.LogInformation("Không tìm thấy session với Key={Key}", key);
                    return default;
                }
                var result = JsonSerializer.Deserialize<T>(value);
                _logger.LogInformation("Lấy từ session: Key={Key}, Value={Value}", key, value);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy session: Key={Key}", key);
                return default;
            }
        }

        public class CartItem
        {
            public string MaChiTiet { get; set; }
            public string ProductName { get; set; }
            public int Quantity { get; set; }
            public int Price { get; set; }
            public string ImageUrl { get; set; }
        }

        private class CtspModel
        {
            public string MaChiTiet { get; set; }
            public string TenSanPham { get; set; }
            public decimal GiaBan { get; set; }
            public int SoLuongTon { get; set; }
        }

        private class HinhAnhModel
        {
            public string MaAnh { get; set; }
            public string AnhDaiDien_Url { get; set; }
            public string AnhHienThi_Url { get; set; }
            public string MaChiTiet { get; set; }
        }
    }
}