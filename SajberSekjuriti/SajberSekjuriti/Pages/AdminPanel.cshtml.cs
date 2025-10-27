using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;

namespace SajberSekjuriti.Pages
{
    [Authorize(Roles = "Admin")]
    public class AdminPanelModel : PageModel
    {
        private readonly UserService _userService;
        private readonly AuditLogService _auditLogService;

        public AdminPanelModel(UserService userService, AuditLogService auditLogService)
        {
            _userService = userService;
            _auditLogService = auditLogService;
        }

        public List<User> Users { get; set; } = new();
        // Metoda obs³uguj¹ca ¿¹danie GET do strony panelu administracyjnego, aby pobraæ wszystkich uzytkowników
        public async Task OnGetAsync()
        {
            Users = await _userService.GetAllAsync();
        }
        // Metoda obs³uguj¹ca ¿¹danie POST do zablokowania u¿ytkownika
        public async Task<IActionResult> OnPostBlockAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = true;
                await _userService.UpdateAsync(user);
                await _auditLogService.LogAsync(User.Identity.Name, "Zarz¹dzanie", $"Zablokowano u¿ytkownika {user.Username}.");
            }
            return RedirectToPage();
        }
        // Metoda obs³uguj¹ca ¿¹danie POST do odblokowania u¿ytkownika
        public async Task<IActionResult> OnPostUnblockAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = false;
                await _userService.UpdateAsync(user);
                await _auditLogService.LogAsync(User.Identity.Name, "Zarz¹dzanie", $"Odblokowano u¿ytkownika {user.Username}.");
            }
            return RedirectToPage();
        }
        // Metoda obs³uguj¹ca ¿¹danie POST do usuniêcia u¿ytkownika
        public async Task<IActionResult> OnPostDeleteAsync([FromForm] string id)
        {
            await _userService.DeleteAsync(id);
            await _auditLogService.LogAsync(User.Identity.Name, "Zarz¹dzanie", $"Usuniêto u¿ytkownika (ID: {id}).");
            return RedirectToPage();
        }
        // Metoda obs³uguj¹ca ¿¹danie POST do wymuszenia zmiany has³a przy nastêpnym logowaniu
        public async Task<IActionResult> OnPostForcePasswordChangeAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.MustChangePassword = true;
                await _userService.UpdateAsync(user);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEnableOTPAsync(string id)
        {
            // 1. ZnajdŸ u¿ytkownika
            var user = await _userService.GetByIdAsync(id);

            if (user != null)
            {
                // 2. Zmieñ flagê
                user.IsOneTimePasswordEnabled = true;

                // 3. Zapisz zmiany w bazie
                await _userService.UpdateAsync(user);

                // 4. Zapisz log
                await _auditLogService.LogAsync(User.Identity.Name, "W³¹czono OTP", $"Admin w³¹czy³ OTP dla u¿ytkownika {user.Username}.");
            }

            // 5. Wróæ na tê sam¹ stronê
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDisableOTPAsync(string id)
        {
            var user = await _userService.GetByIdAsync(id);

            if (user != null)
            {
                user.IsOneTimePasswordEnabled = false;
                await _userService.UpdateAsync(user);
                await _auditLogService.LogAsync(User.Identity.Name, "Wy³¹czono OTP", $"Admin wy³¹czy³ OTP dla u¿ytkownika {user.Username}.");
            }

            return RedirectToPage();
        }
    }
}