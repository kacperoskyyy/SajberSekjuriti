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

        public AdminPanelModel(UserService userService)
        {
            _userService = userService;
        }

        public List<User> Users { get; set; } = new();

        public async Task OnGetAsync()
        {
            Users = await _userService.GetAllAsync();
        }
        public async Task<IActionResult> OnPostBlockAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = true;
                await _userService.UpdateAsync(user);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnblockAsync([FromForm] string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user != null)
            {
                user.IsBlocked = false;
                await _userService.UpdateAsync(user);
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromForm] string id)
        {
            await _userService.DeleteAsync(id);
            return RedirectToPage();
        }

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