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
            // Konwertujemy dane z bazy (int?) na nasz model formularza (string)
            Input.Id = settings.Id;
            Input.IsEnabled = settings.IsEnabled;
            Input.RequireDigit = settings.RequireDigit;
            Input.RequireSpecialCharacter = settings.RequireSpecialCharacter;
            Input.RequireUppercase = settings.RequireUppercase;
            Input.MinimumLength = settings.MinimumLength?.ToString();
            Input.PasswordExpirationDays = settings.PasswordExpirationDays?.ToString();
        }
        //PROBLEM JEST TUTAJ
        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Metoda OnPostAsync zosta³a wywo³ana.");

            // Krok 1: Rêcznie zamieniamy tekst z formularza na liczby
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

            // Krok 2: Sprawdzamy, czy nasza rêczna walidacja znalaz³a b³êdy
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState jest nieprawid³owy PO RÊCZNEJ WALIDACJI.");
                return Page();
            }

            // Krok 3: Zapisujemy dane do bazy
            var settingsToSave = await _policyService.GetSettingsAsync();
            settingsToSave.Id = Input.Id;
            settingsToSave.IsEnabled = Input.IsEnabled;
            settingsToSave.RequireDigit = Input.RequireDigit;
            settingsToSave.RequireSpecialCharacter = Input.RequireSpecialCharacter;
            settingsToSave.RequireUppercase = Input.RequireUppercase;
            settingsToSave.MinimumLength = minLength; // Zapisujemy skonwertowan¹ liczbê (lub null)
            settingsToSave.PasswordExpirationDays = expirationDays; // Zapisujemy skonwertowan¹ liczbê (lub null)

            await _policyService.SaveSettingsAsync(settingsToSave);

            TempData["SuccessMessage"] = "Polityka hase³ zosta³a zaktualizowana.";
            return RedirectToPage("/AdminPanel");
        }

        //DO TAD JEST PROBLEM
    }
}