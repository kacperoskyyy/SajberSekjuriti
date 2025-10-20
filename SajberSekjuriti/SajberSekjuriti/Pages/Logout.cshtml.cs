using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Services;

namespace SajberSekjuriti.Pages
{
    public class LogoutModel : PageModel
    {
        private readonly AuditLogService _auditLogService;

        public LogoutModel(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                await _auditLogService.LogAsync(User.Identity.Name ?? "Nieznany", "Wylogowanie", "U¿ytkownik wylogowa³ siê.");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Index");
        }
    }
}