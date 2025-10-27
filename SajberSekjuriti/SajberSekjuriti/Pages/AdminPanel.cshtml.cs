using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;

namespace SajberSekjuriti.Pages;

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
            await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Zablokowano u�ytkownika {user.Username}.");
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
            await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Odblokowano u�ytkownika {user.Username}.");
        }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteAsync([FromForm] string id)
    {
        await _userService.DeleteAsync(id);
        await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Usuni�to u�ytkownika (ID: {id}).");
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

    public async Task<IActionResult> OnPostEnableOTPAsync(string id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user != null)
        {
            user.IsOneTimePasswordEnabled = true;
            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(User.Identity.Name, "W��czono OTP", $"Admin w��czy� OTP dla u�ytkownika {user.Username}.");
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDisableOTPAsync(string id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user != null)
        {
            user.IsOneTimePasswordEnabled = false;
            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(User.Identity.Name, "Wy��czono OTP", $"Admin wy��czy� OTP dla u�ytkownika {user.Username}.");
        }

        return RedirectToPage();
    }
}