using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages
{
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

        // Ten model bêdzie u¿ywany TYLKO do komunikacji z formularzem
        public class InputModel
        {
            public string Id { get; set; } = string.Empty;
            public bool IsEnabled { get; set; }
            public bool RequireDigit { get; set; }
            public bool RequireSpecialCharacter { get; set; }
            public bool RequireUppercase { get; set; }

            [Display(Name = "Minimalna d³ugoœæ has³a")]
            public string? MinimumLength { get; set; }

            [Display(Name = "Wa¿noœæ has³a (w dniach, 0 = wy³¹czone)")]
            public string? PasswordExpirationDays { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            var settings = await _policyService.GetSettingsAsync();
            // Konwertujemy dane z bazy na nasz model formularza
            Input.Id = settings.Id;
            Input.IsEnabled = settings.IsEnabled;
            Input.RequireDigit = settings.RequireDigit;
            Input.RequireSpecialCharacter = settings.RequireSpecialCharacter;
            Input.RequireUppercase = settings.RequireUppercase;
            Input.MinimumLength = settings.MinimumLength?.ToString();
            Input.PasswordExpirationDays = settings.PasswordExpirationDays?.ToString();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int? minLength = null;
            if (int.TryParse(Input.MinimumLength, out int parsedMinLength))
            {
                minLength = parsedMinLength;
            }
            else if (!string.IsNullOrEmpty(Input.MinimumLength))
            {
                ModelState.AddModelError("Input.MinimumLength", "Wartoœæ musi byæ poprawn¹ liczb¹.");
            }

            int? expirationDays = null;
            if (int.TryParse(Input.PasswordExpirationDays, out int parsedExpirationDays))
            {
                expirationDays = parsedExpirationDays;
            }
            else if (!string.IsNullOrEmpty(Input.PasswordExpirationDays))
            {
                ModelState.AddModelError("Input.PasswordExpirationDays", "Wartoœæ musi byæ poprawn¹ liczb¹.");
            }

            if (!ModelState.IsValid)
            {
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

            await _policyService.SaveSettingsAsync(settingsToSave);
            TempData["SuccessMessage"] = "Polityka hase³ zosta³a zaktualizowana.";
            return RedirectToPage("/AdminPanel");
        }
    }
}