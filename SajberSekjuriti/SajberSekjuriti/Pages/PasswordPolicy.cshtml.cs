using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages;

[Authorize(Roles = "Admin")]
public class PasswordPolicyModel : PageModel
{
    private readonly PasswordPolicyService _policyService;
    private readonly ILogger<PasswordPolicyModel> _logger;

    public PasswordPolicyModel(PasswordPolicyService policyService, ILogger<PasswordPolicyModel> logger)
    {
        _policyService = policyService;
        _logger = logger;
    }

    public class InputModel
    {
        public string? Id { get; set; }
        public bool IsEnabled { get; set; }
        public bool RequireDigit { get; set; }
        public bool RequireSpecialCharacter { get; set; }
        public bool RequireUppercase { get; set; }

        [Display(Name = "Minimalna d³ugoœæ has³a")]
        public string? MinimumLength { get; set; }

        [Display(Name = "Wa¿noœæ has³a (w dniach, 0 = wy³¹czone)")]
        public string? PasswordExpirationDays { get; set; }

        public bool EnableAuditLog { get; set; }

        [Display(Name = "Limit b³êdnych logowañ (0 = wy³¹czone)")]
        public string? MaxLoginAttempts { get; set; }

        [Display(Name = "Czas blokady konta (w minutach)")]
        public string? LockoutDurationMinutes { get; set; }

        [Display(Name = "Czas sesji u¿ytkownika (w minutach, 0 = bez limitu)")]
        public string? SessionTimeoutMinutes { get; set; }
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var settings = await _policyService.GetSettingsAsync();
        Input.Id = settings.Id;
        Input.IsEnabled = settings.IsEnabled;
        Input.RequireDigit = settings.RequireDigit;
        Input.RequireSpecialCharacter = settings.RequireSpecialCharacter;
        Input.RequireUppercase = settings.RequireUppercase;
        Input.MinimumLength = settings.MinimumLength?.ToString();
        Input.PasswordExpirationDays = settings.PasswordExpirationDays?.ToString();
        Input.EnableAuditLog = settings.EnableAuditLog;
        Input.MaxLoginAttempts = settings.MaxLoginAttempts?.ToString();
        Input.LockoutDurationMinutes = settings.LockoutDurationMinutes?.ToString();
        Input.SessionTimeoutMinutes = settings.SessionTimeoutMinutes?.ToString();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("Metoda OnPostAsync zosta³a wywo³ana.");

        int? minLength = null;
        if (!string.IsNullOrEmpty(Input.MinimumLength))
        {
            if (int.TryParse(Input.MinimumLength, out int parsedMinLength) && parsedMinLength >= 0)
            {
                minLength = parsedMinLength;
            }
            else
            {
                ModelState.AddModelError("Input.MinimumLength", "Wartoœæ musi byæ poprawn¹, nieujemn¹ liczb¹.");
            }
        }

        int? expirationDays = null;
        if (!string.IsNullOrEmpty(Input.PasswordExpirationDays))
        {
            if (int.TryParse(Input.PasswordExpirationDays, out int parsedExpirationDays) && parsedExpirationDays >= 0)
            {
                expirationDays = parsedExpirationDays;
            }
            else
            {
                ModelState.AddModelError("Input.PasswordExpirationDays", "Wartoœæ musi byæ poprawn¹, nieujemn¹ liczb¹.");
            }
        }

        int? maxAttempts = null;
        if (int.TryParse(Input.MaxLoginAttempts, out int parsedAttempts) && parsedAttempts >= 0)
        {
            maxAttempts = parsedAttempts;
        }
        else if (!string.IsNullOrEmpty(Input.MaxLoginAttempts))
        {
            ModelState.AddModelError("Input.MaxLoginAttempts", "Wartoœæ musi byæ poprawn¹, nieujemn¹ liczb¹.");
        }

        int? lockoutMinutes = null;
        if (int.TryParse(Input.LockoutDurationMinutes, out int parsedLockout) && parsedLockout >= 0)
        {
            lockoutMinutes = parsedLockout;
        }
        else if (!string.IsNullOrEmpty(Input.LockoutDurationMinutes))
        {
            ModelState.AddModelError("Input.LockoutDurationMinutes", "Wartoœæ musi byæ poprawn¹, nieujemn¹ liczb¹.");
        }

        int? sessionMinutes = null;
        if (int.TryParse(Input.SessionTimeoutMinutes, out int parsedSession) && parsedSession >= 0)
        {
            sessionMinutes = parsedSession;
        }
        else if (!string.IsNullOrEmpty(Input.SessionTimeoutMinutes))
        {
            ModelState.AddModelError("Input.SessionTimeoutMinutes", "Wartoœæ musi byæ poprawn¹, nieujemn¹ liczb¹.");
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("ModelState jest nieprawid³owy PO RÊCZNEJ WALIDACJI.");
            return Page();
        }

        var settingsToSave = await _policyService.GetSettingsAsync();
        settingsToSave.Id = Input.Id;
        settingsToSave.IsEnabled = Input.IsEnabled;
        settingsToSave.RequireDigit = Input.RequireDigit;
        settingsToSave.RequireSpecialCharacter = Input.RequireSpecialCharacter;
        settingsToSave.RequireUppercase = Input.RequireUppercase;
        settingsToSave.MinimumLength = minLength;
        settingsToSave.PasswordExpirationDays = expirationDays;
        settingsToSave.EnableAuditLog = Input.EnableAuditLog;
        settingsToSave.MaxLoginAttempts = maxAttempts;
        settingsToSave.LockoutDurationMinutes = lockoutMinutes;
        settingsToSave.SessionTimeoutMinutes = sessionMinutes;

        await _policyService.SaveSettingsAsync(settingsToSave);

        TempData["SuccessMessage"] = "Polityka hase³ zosta³a zaktualizowana.";
        return RedirectToPage("/AdminPanel");
    }


}