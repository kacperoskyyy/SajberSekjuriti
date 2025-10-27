using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration; // <-- Dodane
using System.Threading.Tasks; // <-- Dodane

namespace SajberSekjuriti.Pages
{
    [Authorize]
    public class UserChangePasswordModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        private readonly PasswordPolicyService _policyService;
        private readonly PasswordValidationService _validationService;
        private readonly AuditLogService _auditLogService;

        // --- DODANE SERWISY ---
        private readonly ReCaptchaService _reCaptchaService;
        private readonly IConfiguration _configuration;

        //Konstruktor klasy UserChangePasswordModel, który inicjalizuje serwisy potrzebne do zmiany has³a u¿ytkownika.
        public UserChangePasswordModel(
            AuditLogService auditLogService,
            UserService userService,
            PasswordService passwordService,
            PasswordPolicyService policyService,
            PasswordValidationService validationService,
            ReCaptchaService reCaptchaService, // <-- Dodane
            IConfiguration configuration) // <-- Dodane
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
            _auditLogService = auditLogService;
            _reCaptchaService = reCaptchaService; // <-- Dodane
            _configuration = configuration; // <-- Dodane
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // --- DODANA W£AŒCIWOŒÆ ---
        // Do przekazania klucza publicznego do widoku .cshtml
        public string ReCaptchaSiteKey => _configuration["ReCaptchaSettings:SiteKey"];

        // Dodajemy, aby wyœwietlaæ b³êdy reCaptcha
        public string? ErrorMessage { get; set; }

        //Definicja klasy InputModel, która zawiera w³aœciwoœci potrzebne do zmiany has³a u¿ytkownika.
        public class InputModel
        {
            [Required(ErrorMessage = "Stare has³o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Stare has³o")]
            public string OldPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nowe has³o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has³o")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "PotwierdŸ nowe has³o")]
            [Compare("NewPassword", ErrorMessage = "Has³a nie s¹ takie same.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        //Metoda obs³uguj¹ca ¿¹dania POST do strony zmiany has³a.
        public async Task<IActionResult> OnPostAsync()
        {
            // --- KROK 1: Weryfikacja Google reCaptcha ---
            var reCaptchaToken = Request.Form["g-recaptcha-response"];
            var isReCaptchaValid = await _reCaptchaService.ValidateAsync(reCaptchaToken);

            if (!isReCaptchaValid)
            {
                ErrorMessage = "Weryfikacja CAPTCHA nie powiod³a siê. Spróbuj ponownie.";
                return Page();
            }

            // --- KROK 2: Twoja dotychczasowa logika ---

            //Sprawdzenie, czy dane wejœciowe s¹ poprawne.
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //Pobranie nazwy u¿ytkownika z kontekstu i sprawdzenie, czy u¿ytkownik jest zalogowany.
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }
            //Pobranie polityki hase³ i walidacja nowego has³a u¿ytkownika.
            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.NewPassword, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                return Page();
            }
            //Pobranie u¿ytkownika z bazy danych i sprawdzenie, czy stare has³o jest poprawne.
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.OldPassword", "Stare has³o jest nieprawid³owe.");
                return Page();
            }

            //SPRAWDZANIE HISTORII HAS£A
            foreach (var oldHash in user.PasswordHistory)
            {
                if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
                {
                    ModelState.AddModelError("Input.NewPassword", "Nowe has³o nie mo¿e byæ takie samo jak jedno z 5 poprzednich.");
                    return Page();
                }
            }
            user.PasswordHistory.Add(user.PasswordHash);

            while (user.PasswordHistory.Count > 5)
            {
                user.PasswordHistory.RemoveAt(0);
            }
            //Aktualizacja has³a u¿ytkownika i zapisanie zmian w bazie danych.
            user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            user.PasswordLastSet = DateTime.UtcNow;
            user.MustChangePassword = false;

            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(username, "Zmiana has³a", "U¿ytkownik samodzielnie zmieni³ has³o.");

            TempData["SuccessMessage"] = "Has³o zosta³o pomyœlnie zmienione.";
            return RedirectToPage("/Index");
        }
    }
}