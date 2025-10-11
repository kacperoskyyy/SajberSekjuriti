using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;

namespace SajberSekjuriti.Pages
{
    [Authorize(Roles = "Admin")]
    public class PasswordPolicyModel : PageModel
    {
        private readonly PasswordPolicyService _policyService;
        private readonly ILogger<PasswordPolicyModel> _logger; // Dodajemy logger

        public PasswordPolicyModel(PasswordPolicyService policyService, ILogger<PasswordPolicyModel> logger)
        {
            _policyService = policyService;
            _logger = logger;
        }

        [BindProperty]
        public PasswordPolicySettings Settings { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Metoda OnGetAsync zosta�a wywo�ana.");
            Settings = await _policyService.GetSettingsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Metoda OnPostAsync zosta�a wywo�ana.");
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState jest nieprawid�owy.");
                return Page();
            }

            try
            {
                await _policyService.SaveSettingsAsync(Settings);
                _logger.LogInformation("Zapis zako�czony sukcesem. Przekierowuj� na /AdminPanel.");
                TempData["SuccessMessage"] = "Polityka hase� zosta�a zaktualizowana.";
                return RedirectToPage("/AdminPanel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wyst�pi� b��d w OnPostAsync podczas zapisu.");
                ModelState.AddModelError(string.Empty, "Nie uda�o si� zapisa� ustawie�. Sprawd� logi aplikacji.");
                return Page();
            }
        }
    }
}