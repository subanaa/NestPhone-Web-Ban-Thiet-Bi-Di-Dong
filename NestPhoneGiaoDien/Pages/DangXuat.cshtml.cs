using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MobileStore.Web.Pages.Auth
{
    public class DangXuatModel : PageModel
    {
        private readonly ILogger<DangXuatModel> _logger;

        public DangXuatModel(ILogger<DangXuatModel> logger)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Người dùng đăng xuất: UserName={UserName}, MaKhachHang={MaKhachHang}",
                User.Identity.Name ?? "null",
                User.FindFirst("MaKhachHang")?.Value ?? "null");

            HttpContext.Session.Clear();
            await HttpContext.Session.CommitAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Đăng xuất thành công. Session và cookie đã được xóa.");
            return RedirectToPage("/Index");
        }
    }
}