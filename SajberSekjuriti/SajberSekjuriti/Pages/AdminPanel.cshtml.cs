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
        // Metoda obs�uguj�ca ��danie GET do strony panelu administracyjnego, aby pobra� wszystkich uzytkownik�w
        public async Task OnGetAsync()
        {
            Users = await _userService.GetAllAsync();
        }
        // Metoda obs�uguj�ca ��danie POST do zablokowania u�ytkownika
        public async Task<IActionResult> OnPostBlockAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = true;
                await _userService.UpdateAsync(user);
                await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Zablokowano u�ytkownika {user.Username}.");
            }
            return RedirectToPage();
        }
        // Metoda obs�uguj�ca ��danie POST do odblokowania u�ytkownika
        public async Task<IActionResult> OnPostUnblockAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = false;
                await _userService.UpdateAsync(user);
                await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Odblokowano u�ytkownika {user.Username}.");
            }
            return RedirectToPage();
        }
        // Metoda obs�uguj�ca ��danie POST do usuni�cia u�ytkownika
        public async Task<IActionResult> OnPostDeleteAsync([FromForm] string id)
        {
            await _userService.DeleteAsync(id);
            await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Usuni�to u�ytkownika (ID: {id}).");
            return RedirectToPage();
        }
        // Metoda obs�uguj�ca ��danie POST do wymuszenia zmiany has�a przy nast�pnym logowaniu
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
    }
}